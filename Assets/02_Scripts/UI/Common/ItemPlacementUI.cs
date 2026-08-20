using UnityEngine;
using UnityEngine.UI;

public class ItemPlacementUI : MonoBehaviour
{
    [SerializeField] private RectTransform _root;
    [SerializeField] private Button cancelEditModeButton;

    private Canvas _canvas;
    private Transform _target;
    private Camera _worldCamera;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        cancelEditModeButton.onClick.AddListener(OnCancelEditModeButtonClicked);
    }

    private void OnDestroy()
    {
        if (cancelEditModeButton != null)
        {
            cancelEditModeButton.onClick.RemoveListener(OnCancelEditModeButtonClicked);
        }
    }

    private void OnCancelEditModeButtonClicked()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.CancelEditMode();
        }
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void SetTarget(Transform target, Camera worldCamera)
    {
        _target = target;
        _worldCamera = worldCamera;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_root == null || _canvas == null || _target == null || _worldCamera == null)
        {
            return;
        }

        var screenPosition = _worldCamera.WorldToScreenPoint(_target.position);
        if (screenPosition.z < 0f)
        {
            return;
        }

        var parentRect = _root.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        var rootCanvas = _canvas.rootCanvas;
        var uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                uiCamera,
                out var localPosition))
        {
            return;
        }

        var currentPosition = _root.localPosition;
        _root.localPosition = new Vector3(localPosition.x, localPosition.y, currentPosition.z);
    }
}
