using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScroll : MonoBehaviour
{
    private enum Orientation { Vertical, Horizontal }

    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;   // Usually scrollRect.viewport
    [SerializeField] private RectTransform content;    // The content under ScrollRect

    [Header("Item Settings")]
    [SerializeField] private RectTransform itemPrefab; // Prefab with fixed axis size

    // 🔹 리스트/그리드 공통 간격을 분리
    [SerializeField] private float verticalSpacing = 0f;   // 줄(row) 간 간격
    [SerializeField] private float horizontalSpacing = 0f; // 열(column) 간 간격

    [Header("Grid (Vertical scroll only)")]
    [Tooltip("2 이상이면 세로 스크롤 그리드로 동작")]
    [SerializeField] private int columnCount = 1;          // 열 수
    [Tooltip("열 폭을 뷰포트에 맞게 균등 분배(권장)")]
    [SerializeField] private bool stretchColumnsToViewport = true;

    [Header("Padding (Content inner margins)")]
    [SerializeField] private float paddingTop = 0f;
    [SerializeField] private float paddingBottom = 0f;
    [SerializeField] private float paddingLeft = 0f;
    [SerializeField] private float paddingRight = 0f;


    [Header("Pooling/Perf")]
    [SerializeField] private int bufferItems = 2;      // Extra rows before/after viewport (grid에선 '행' 버퍼로 해석)

    [Header("Effect")]
    [SerializeField] private AnimationCurve targetScrollCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    public Action<RectTransform, int> OnBind;

    public int TotalCount { get; private set; } = 0;
    public bool Initialized { get; private set; } = false;

    private readonly Dictionary<int, RectTransform> activeItems = new();
    private readonly Queue<RectTransform> itemPool = new();

    private float itemHeight = 100f; // inferred from prefab (or LayoutElement)
    private float itemWidth = 100f;  // inferred from prefab (or LayoutElement)

    private Orientation orientation;
    private int visibleSlotCount = 0;
    private int prevFirstVisibleIndex = -1;
    private int prevLastVisibleIndex = -1;
    private bool _layoutInitialized = false;

    // 🔹 그리드용 계산치
    private float ColumnWidth => ComputeColumnWidth();                // 실제 아이템 폭(스트레치 반영)
    private float RowSpan => itemHeight + verticalSpacing;         // 한 행 높이(간격 포함)
    private float ColSpan => ColumnWidth + horizontalSpacing;      // 한 열 폭(간격 포함)
    private int Columns => Mathf.Max(1, columnCount);            // 안전 가드

    // cached listener
    private UnityAction<Vector2> _onScrollChanged;

    private bool _pendingRebuild;


    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewport = scrollRect.viewport;
            content = scrollRect.content;
        }
    }

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (viewport == null) viewport = scrollRect != null ? scrollRect.viewport : null;
        if (content == null) content = scrollRect != null ? scrollRect.content : null;

        DetectOrientation();
        EnsureContentLayoutForOrientation();
        RecalculateItemSizeFromPrefab();
    }

    void OnEnable()
    {
        DetectOrientation();
        EnsureContentLayoutForOrientation();
        RecalculateItemSizeFromPrefab();

        _onScrollChanged ??= _ => UpdateVisibleRange();
        scrollRect.onValueChanged.AddListener(_onScrollChanged);

        if (Initialized)
            UpdateVisibleRange(true);
    }

    void OnDisable()
    {
        if (_onScrollChanged != null)
            scrollRect.onValueChanged.RemoveListener(_onScrollChanged);
    }

    // ---------- Public API ----------

    public void Init(int totalCount, Action<RectTransform, int> onBind)
    {
        Initialized = true;
        OnBind = onBind;
        SetTotalCount(totalCount);
    }

    public void SetTotalCount(int totalCount)
    {
        TotalCount = Mathf.Max(0, totalCount);
        WithScrollHandlerMuted(() =>
        {
            UpdateContentSize();
            EnsurePoolCapacity();
        });
        RequestUpdateVisibleRange();
    }

    public void Refresh(bool resetPos = false)
    {
        foreach (var kv in activeItems)
            TryBindItem(kv.Value, kv.Key);
        if (resetPos)
        {
            content.anchoredPosition = Vector2.zero;
        }
    }
    public async UniTask ScrollToIndex(int index, float duration = 0.5f, CancellationToken token = default)
    {
        if (TotalCount == 0) return;
        index = Mathf.Clamp(index, 0, Mathf.Max(0, TotalCount - 1));

        float contentLen = GetContentLength();

        int targetRow = (orientation == Orientation.Vertical && Columns > 1)
            ? (index / Columns)
            : index;

        float target = (orientation == Orientation.Vertical)
            ? (paddingTop + targetRow * RowSpan)
            : (paddingLeft + targetRow * (itemWidth + horizontalSpacing));

        float maxOffset = Mathf.Max(0, contentLen - GetViewportLength());
        float norm = maxOffset <= 0 ? 0 : Mathf.Clamp01(target / maxOffset);

        Vector2 orgPos = scrollRect.normalizedPosition;
        Vector2 targetPos = (orientation == Orientation.Vertical)
            ? new Vector2(scrollRect.normalizedPosition.x, 1f - norm)
            : new Vector2(norm, scrollRect.normalizedPosition.y);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            scrollRect.normalizedPosition = Vector2.Lerp(orgPos, targetPos, targetScrollCurve.Evaluate(t / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        UpdateVisibleRange(true);
    }


    public void SetSpacing(float spacing) // 호환용(세로/가로 동일 적용)
    {
        verticalSpacing = horizontalSpacing = Mathf.Max(0f, spacing);
        WithScrollHandlerMuted(() =>
        {
            UpdateContentSize();
            EnsurePoolCapacity();
        });
        UpdateVisibleRange(true);
    }

    public void SetBuffer(int buffer)
    {
        bufferItems = Mathf.Max(0, buffer);
        EnsurePoolCapacity();
        UpdateVisibleRange(true);
    }

    // ---------- Internal ----------

    private void DetectOrientation()
    {
        if (scrollRect != null && scrollRect.vertical)
            orientation = Orientation.Vertical;
        else
            orientation = Orientation.Horizontal;

        if (scrollRect != null && scrollRect.vertical && scrollRect.horizontal)
        {
            Debug.LogWarning("[InfiniteScroll] Both axes enabled on ScrollRect. Using Vertical mode.");
        }
    }

    private void EnsureContentLayoutForOrientation()
    {
        if (content == null) return;

        if (orientation == Orientation.Vertical)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            var size = content.sizeDelta;
            size.x = 0f; // stretch
            content.sizeDelta = size;

            if (!Initialized)
                content.anchoredPosition = Vector2.zero;
        }
        else
        {
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);

            var size = content.sizeDelta;
            size.y = 0f; // stretch
            content.sizeDelta = size;

            if (!Initialized)
                content.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateContentSize()
    {
        if (content == null) return;
        var size = content.sizeDelta;

        if (orientation == Orientation.Vertical)
        {
            size.y = GetContentLength(); // 🔹 그리드면 총 행 수 기준
            size.x = 0f;
            content.sizeDelta = size;
        }
        else
        {
            size.x = GetContentLength();
            size.y = 0f;
            content.sizeDelta = size;
        }
    }

    private float GetContentLength()
    {
        if (TotalCount == 0) return paddingTop + paddingBottom; // 최소 패딩은 유지

        if (orientation == Orientation.Vertical && Columns > 1)
        {
            int totalRows = Mathf.CeilToInt(TotalCount / (float)Columns);
            float itemsLen = totalRows * RowSpan - verticalSpacing; // 마지막 줄 간격 제외
            return paddingTop + itemsLen + paddingBottom;
        }
        else
        {
            float itemSpan = (orientation == Orientation.Vertical) ? (itemHeight + verticalSpacing)
                                                                   : (itemWidth + horizontalSpacing);
            float lastGap = (orientation == Orientation.Vertical) ? verticalSpacing : horizontalSpacing;
            float itemsLen = TotalCount * itemSpan - lastGap;
            return (orientation == Orientation.Vertical)
                ? paddingTop + itemsLen + paddingBottom
                : paddingLeft + itemsLen + paddingRight;
        }
    }


    private float GetViewportLength()
    {
        if (viewport == null) return 0f;
        return orientation == Orientation.Vertical ? viewport.rect.height : viewport.rect.width;
    }

    private int CalcVisibleSlotCount()
    {
        float viewLen = GetViewportLength();

        if (orientation == Orientation.Vertical && Columns > 1)
        {
            // 🔹 그리드: 보이는 '행' 수 × 열 수
            int rows = Mathf.CeilToInt(viewLen / Mathf.Max(1e-5f, RowSpan)) + bufferItems * 2;
            rows = Mathf.Max(1, rows);
            visibleSlotCount = rows * Columns;
        }
        else
        {
            // 리스트
            float span = (orientation == Orientation.Vertical) ? RowSpan : (itemWidth + horizontalSpacing);
            int needed = Mathf.CeilToInt(viewLen / Mathf.Max(1e-5f, span)) + bufferItems * 2;
            visibleSlotCount = Mathf.Max(1, needed);
        }

        return visibleSlotCount;
    }

    private void EnsurePoolCapacity()
    {
        int need = CalcVisibleSlotCount();
        while (itemPool.Count + activeItems.Count < need)
        {
            var rt = CreateItemInstance();
            rt.gameObject.SetActive(false);
            itemPool.Enqueue(rt);
        }
    }

    private RectTransform CreateItemInstance()
    {
        var inst = Instantiate(itemPrefab, content);
        var rt = inst.GetComponent<RectTransform>();
        EnsureItemLayoutForOrientation(rt);
        return rt;
    }
    private void EnsureItemLayoutForOrientation(RectTransform rt)
    {
        if (orientation == Orientation.Vertical)
        {
            if (Columns > 1)
            {
                // ── 그리드(세로 스크롤)
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);

                float width = (stretchColumnsToViewport && viewport != null) ? ColumnWidth : itemWidth;
                rt.sizeDelta = new Vector2(width, itemHeight);
            }
            else
            {
                // ── 리스트(세로 스크롤): 가로 스트레치 + 좌우 패딩
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);

                var sd = rt.sizeDelta;
                sd.y = itemHeight;
                rt.sizeDelta = sd;

                // 좌우 패딩
                rt.offsetMin = new Vector2(paddingLeft, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-paddingRight, rt.offsetMax.y);
            }
        }
        else
        {
            // ── 가로 스크롤 리스트: 세로 스트레치 + 상하 패딩
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(itemWidth, 0f);

            // 상하 패딩
            rt.offsetMin = new Vector2(rt.offsetMin.x, paddingBottom);
            rt.offsetMax = new Vector2(rt.offsetMax.x, -paddingTop);
        }
    }


    private void UpdateVisibleRange(bool force = false)
    {
        if (!Initialized || TotalCount == 0)
        {
            RecycleAll();
            return;
        }

        float rawOffset = orientation == Orientation.Vertical
            ? content.anchoredPosition.y
            : -content.anchoredPosition.x;
        if (float.IsNaN(rawOffset)) rawOffset = 0f;

        // 패딩 보정
        float axisOffset = orientation == Orientation.Vertical
            ? Mathf.Max(0f, rawOffset - paddingTop)
            : Mathf.Max(0f, rawOffset - paddingLeft);

        int firstIndex, lastIndex;

        if (orientation == Orientation.Vertical && Columns > 1)
        {
            int firstRow = Mathf.FloorToInt(axisOffset / Mathf.Max(1e-5f, RowSpan));
            firstRow = Mathf.Max(0, firstRow);

            CalcVisibleSlotCount();
            int rowsVisible = Mathf.CeilToInt(visibleSlotCount / (float)Columns);

            firstIndex = firstRow * Columns;
            lastIndex = Mathf.Min(TotalCount - 1, (firstRow + rowsVisible) * Columns - 1);
        }
        else
        {
            float span = (orientation == Orientation.Vertical) ? RowSpan : (itemWidth + horizontalSpacing);
            firstIndex = Mathf.FloorToInt(axisOffset / Mathf.Max(1e-5f, span));
            firstIndex = Mathf.Clamp(firstIndex, 0, Mathf.Max(0, TotalCount - 1));

            CalcVisibleSlotCount();
            lastIndex = Mathf.Clamp(firstIndex + visibleSlotCount - 1, 0, TotalCount - 1);
        }

        if (!force && firstIndex == prevFirstVisibleIndex && lastIndex == prevLastVisibleIndex)
            return;

        prevFirstVisibleIndex = firstIndex;
        prevLastVisibleIndex = lastIndex;

        RecycleOutsideRange(firstIndex, lastIndex);

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            if (!activeItems.ContainsKey(i))
            {
                var item = GetItemFromPool();
                EnsureItemLayoutForOrientation(item);
                PlaceItem(item, i);
                TryBindItem(item, i);
                activeItems[i] = item;
            }
        }
    }


    private void RecycleOutsideRange(int firstIndex, int lastIndex)
    {
        scratchRecycleIndices.Clear();
        foreach (var kv in activeItems)
        {
            int idx = kv.Key;
            if (idx < firstIndex || idx > lastIndex)
                scratchRecycleIndices.Add(idx);
        }

        foreach (int idx in scratchRecycleIndices)
        {
            var rt = activeItems[idx];
            activeItems.Remove(idx);
            ReturnItemToPool(rt);
        }
    }

    private static readonly List<int> scratchRecycleIndices = new();

    private RectTransform GetItemFromPool()
    {
        if (itemPool.Count > 0)
        {
            var rt = itemPool.Dequeue();
            rt.gameObject.SetActive(true);
            return rt;
        }
        var r = CreateItemInstance();
        r.gameObject.SetActive(true);
        return r;
    }

    private void ReturnItemToPool(RectTransform rt)
    {
        rt.gameObject.SetActive(false);
        itemPool.Enqueue(rt);
    }

    private void RecycleAll()
    {
        foreach (var kv in activeItems)
            ReturnItemToPool(kv.Value);
        activeItems.Clear();
    }
    private void PlaceItem(RectTransform item, int index)
    {
        if (orientation == Orientation.Vertical && Columns > 1)
        {
            int row = index / Columns;
            int col = index % Columns;

            float x = paddingLeft + col * ColSpan;
            float y = -(paddingTop + row * RowSpan);

            item.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        }
        else
        {
            if (orientation == Orientation.Vertical)
            {
                float y = -(paddingTop + index * RowSpan);
                // 리스트 모드(세로): X는 좌우 패딩을 offset으로 처리(아래 EnsureItemLayoutForOrientation 참고)
                item.anchoredPosition = new Vector2(0f, Mathf.Round(y));
            }
            else
            {
                float x = paddingLeft + index * (itemWidth + horizontalSpacing);
                // 가로 스크롤: Y는 상하 패딩을 offset으로 처리
                item.anchoredPosition = new Vector2(Mathf.Round(x), 0f);
            }
        }

        item.SetAsLastSibling();
    }


    private void TryBindItem(RectTransform item, int index)
    {
        try { OnBind?.Invoke(item, index); }
        catch (Exception e) { Debug.LogException(e); }
    }

    protected void OnRectTransformDimensionsChange()
    {
        if (!Initialized || viewport == null || content == null) return;

        var prev = orientation;
        DetectOrientation();
        EnsureContentLayoutForOrientation();
        RecalculateItemSizeFromPrefab();

        if (prev != orientation)
        {
            RecycleAll(); // orientation 바뀌면 전체 재배치
        }

        WithScrollHandlerMuted(() =>
        {
            UpdateContentSize();
            EnsurePoolCapacity();
        });
        RequestUpdateVisibleRange();
    }
    private void RequestUpdateVisibleRange()
    {
        if (_pendingRebuild) return;
        _pendingRebuild = true;
        DelayedUpdateVisibleRange().Forget();
    }

    private async UniTaskVoid DelayedUpdateVisibleRange()
    {
        // Canvas 리빌딩이 끝날 때까지 대기
        while (CanvasUpdateRegistry.IsRebuildingLayout() || CanvasUpdateRegistry.IsRebuildingGraphics())
        {
            await UniTask.WaitForEndOfFrame();
        }

        _pendingRebuild = false;
        UpdateVisibleRange(true);
    }

    // --- Utils ---
    private void RecalculateItemSizeFromPrefab()
    {
        if (itemPrefab == null) return;

        var rt = itemPrefab.GetComponent<RectTransform>();
        float w = rt.rect.width;
        float h = rt.rect.height;

        if (itemPrefab.TryGetComponent<LayoutElement>(out var le))
        {
            if (le.preferredWidth > 0) w = le.preferredWidth;
            if (le.preferredHeight > 0) h = le.preferredHeight;
        }

        itemWidth = Mathf.Max(1, w);
        itemHeight = Mathf.Max(1, h);
    }

    /// <summary>뷰포트 너비 기준 균등 열 폭 계산</summary>
    private float ComputeColumnWidth()
    {
        if (!(orientation == Orientation.Vertical && Columns > 1 && stretchColumnsToViewport && viewport != null))
            return itemWidth;

        float totalSpacing = horizontalSpacing * (Columns - 1);
        float available = Mathf.Max(0f, viewport.rect.width - totalSpacing - paddingLeft - paddingRight);
        return Mathf.Max(1f, available / Columns);
    }


    private void WithScrollHandlerMuted(Action act)
    {
        if (_onScrollChanged != null)
            scrollRect.onValueChanged.RemoveListener(_onScrollChanged);
        try { act?.Invoke(); }
        finally
        {
            if (_onScrollChanged != null)
                scrollRect.onValueChanged.AddListener(_onScrollChanged);
        }
    }
}
