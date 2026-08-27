
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : SingletonMono<GridManager>
{

    [Header("Tile Size")]
    [SerializeField] private float _tileWidth = 1f;
    [SerializeField] private float _tileHeight = 0.5f;

    [SerializeField, Min(0f)] private float _snapDistance = 0.5f;
    [Header("References")]
    [SerializeField] private FloorTileObj _floorPrefab;
    [SerializeField] private PlaceableObj _placeableObjPrefab;
    [SerializeField] private NpcObj _npcObjPrefab;
    [SerializeField] private Transform _floorRoot;
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private Sprite _defaultFloorSprite;
    [SerializeField] private Sprite _stoneFloorSprite;

    private FloorTileObj[,] _floorTileObjs;
    private AStarPathfinder _pathfinder;
    private Camera _mainCamera;
    private PlaceableObj _previewObj;
    private int _previewFurnitureId;
    private Vector2Int _previewGridPosition;

    public AStarPathfinder Pathfinder => _pathfinder;
    public bool HasPreviewObj => _previewObj != null;
    public event System.Action<Transform> PreviewTargetChanged;

    void Start()
    {
        _mainCamera = Camera.main;
        Initialize();
    }
    private void Initialize()
    {
        UpdateGridRoot();
        ClearFloorObjs();
        _floorTileObjs = new FloorTileObj[GameDefine.GridWidth, GameDefine.GridHeight];
        _pathfinder = new AStarPathfinder(GameDefine.GridWidth, GameDefine.GridHeight, IsWalkable);

        CreateFloorTileObjs();
        GeneratePlaceableObjsFromUserData();
        GenerateNpcRandom();
    }
    private void GeneratePlaceableObjsFromUserData()
    {
        foreach (var obj in UserDataManager.Instance.User.PlaceableObjs)
        {
            CreateBuildingObj(obj.Key);
        }
    }

    private void GenerateNpcRandom()
    {
        var attemptCount = 0;
        var createdCount = 0;
        while (createdCount < 10 && attemptCount++ < 100)
        {
            int x = Random.Range(0, GameDefine.GridWidth);
            int y = Random.Range(0, GameDefine.GridHeight);
            var gridPosition = new Vector2Int(x, y);

            if (!IsWalkable(gridPosition))
            {
                continue;
            }

            var npcObj = CreateNpc(gridPosition);
            // npcObj.MoveTo(GetRandomGridPos());

            createdCount++;
        }
    }

    public Vector2Int GetRandomGridPos()
    {
        var attemptCount = 0;
        var createdCount = 0;
        while (createdCount < 1 && attemptCount++ < 100)
        {
            int x = Random.Range(0, GameDefine.GridWidth);
            int y = Random.Range(0, GameDefine.GridHeight);
            var gridPosition = new Vector2Int(x, y);

            if (!IsWalkable(gridPosition))
            {
                continue;
            }
            createdCount++;
            return new Vector2Int(x, y);
        }
        return new Vector2Int(0, 0);
    }

    private Vector3 CalculateCenterOffset()
    {
        var centerX = (GameDefine.GridWidth - 1) * 0.5f;
        var centerY = (GameDefine.GridHeight - 1) * 0.5f;
        return new Vector3(
            -(centerX - centerY) * _tileWidth * 0.5f,
            -(centerX + centerY) * _tileHeight * 0.5f,
            0f);
    }

    private void UpdateGridRoot()
    {
        _gridRoot.localPosition = CalculateCenterOffset();
    }
    private void ClearFloorObjs()
    {
        for (int i = _floorRoot.childCount - 1; i >= 0; i--)
        {
            var child = _floorRoot.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child);
                continue;
            }
#endif
            Destroy(child);
        }
    }
    private void CreateFloorTileObjs()
    {
        for (int x = 0; x < GameDefine.GridWidth; x++)
        {
            for (int y = 0; y < GameDefine.GridHeight; y++)
            {
                CreateFloorTileObj(new Vector2Int(x, y));
            }
        }
    }
    void Update()
    {
        // HandleFloorInput();
    }

    private void HandleFloorInput()
    {
        if (Pointer.current == null)
        {
            return;
        }

        if (!Pointer.current.press.wasPressedThisFrame)
        {
            return;
        }

        var screenPosition = Pointer.current.position.ReadValue();
        var worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);

        var hit = Physics2D.Raycast(
            worldPosition,
            Vector2.zero);

        if (hit.collider == null)
        {
            Debug.Log($"타일 감지 실패: {worldPosition}");
            return;
        }

        if (!hit.collider.TryGetComponent<FloorTileObj>(out var floorTileObj))
        {
            Debug.Log($"FloorTileObj 없음: {hit.collider.name}");
            return;
        }

        ChangeFloorTile(floorTileObj.GridPos);
    }
    private void CreateFloorTileObj(Vector2Int gridPos)
    {
        var localPos = GridToWorld(gridPos, Vector2Int.one);
        var tileObj = Instantiate(_floorPrefab, _floorRoot);
        tileObj.transform.localPosition = localPos;
        tileObj.transform.localRotation = Quaternion.identity;
        tileObj.Initialize(gridPos, _defaultFloorSprite, () =>
        {
            // ChangeFloorTile(gridPos);
        });
        _floorTileObjs[gridPos.x, gridPos.y] = tileObj;
    }
    private void CreateBuildingObj(long uid)
    {
        UserDataManager.Instance.User.TryGetPlaceableObjData(uid, out var placeableObjData);
        var gridPos = new Vector2Int(placeableObjData.GridX, placeableObjData.GridY);
        var localPos = GridToWorld(gridPos, new Vector2Int(placeableObjData.TableData.sizex, placeableObjData.TableData.sizey));

        PlaceableObj placeableObj = Lean.Pool.LeanPool.Spawn(_placeableObjPrefab, _gridRoot);
        placeableObj.Initialize(placeableObjData, TryMovePlaceableObjToDropPosition);
        placeableObj.transform.localPosition = localPos;
        placeableObj.transform.localRotation = Quaternion.identity;
    }

    public bool CreatePreviewObj(int furnitureId)
    {
        var furnitureData = DataManager.Instance.GetFurnitureData(furnitureId);
        if (furnitureData == null || !TryFindPreviewStartGridPosition(furnitureId, out var gridPosition))
        {
            return false;
        }

        CancelPreviewPlacement();

        _previewFurnitureId = furnitureId;
        _previewGridPosition = gridPosition;
        _previewObj = Lean.Pool.LeanPool.Spawn(_placeableObjPrefab, _gridRoot);
        _previewObj.InitializePreview(furnitureId, TryMovePreviewObjToDropPosition);
        _previewObj.transform.localPosition = GridToWorld(gridPosition, _previewObj.FootprintSize);
        _previewObj.transform.localRotation = Quaternion.identity;
        PreviewTargetChanged?.Invoke(_previewObj.transform);
        if (GameManager.HasInstance)
        {
            GameManager.Instance.EnterEditMode();
        }

        return true;
    }

    public bool ConfirmPreviewPlacement()
    {
        if (_previewObj == null)
        {
            return false;
        }

        var placeableObjData = UserDataManager.Instance.CreatePlaceableObj(
            _previewFurnitureId,
            _previewGridPosition.x,
            _previewGridPosition.y);
        if (placeableObjData == null)
        {
            return false;
        }

        var placeableObj = _previewObj;
        ClearPreviewState();
        placeableObj.Initialize(placeableObjData, TryMovePlaceableObjToDropPosition);
        return true;
    }

    public void CancelPreviewPlacement()
    {
        if (_previewObj != null)
        {
            Lean.Pool.LeanPool.Despawn(_previewObj);
        }

        ClearPreviewState();
    }

    private void ClearPreviewState()
    {
        _previewObj = null;
        _previewFurnitureId = 0;
        _previewGridPosition = default;
        if (_previewObj != null)
        {
            PreviewTargetChanged?.Invoke(null);
        }
    }

    private bool TryFindPreviewStartGridPosition(int furnitureId, out Vector2Int gridPosition)
    {
        gridPosition = default;

        var furnitureData = DataManager.Instance.GetFurnitureData(furnitureId);
        if (furnitureData == null)
        {
            return false;
        }

        var footprintSize = new Vector2Int(
            Mathf.Max(1, furnitureData.sizex),
            Mathf.Max(1, furnitureData.sizey));
        var maxGridX = GameDefine.GridWidth - footprintSize.x;
        var maxGridY = GameDefine.GridHeight - footprintSize.y;
        if (maxGridX < 0 || maxGridY < 0)
        {
            return false;
        }

        var preferredPosition = new Vector2Int(maxGridX / 2, maxGridY / 2);
        var nearestDistanceSqr = int.MaxValue;

        for (int x = 0; x <= maxGridX; x++)
        {
            for (int y = 0; y <= maxGridY; y++)
            {
                var candidate = new Vector2Int(x, y);
                if (!UserDataManager.Instance.User.CanPlaceFurniture(furnitureId, candidate))
                {
                    continue;
                }

                var distanceSqr = (candidate - preferredPosition).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                gridPosition = candidate;
            }
        }

        return nearestDistanceSqr != int.MaxValue;
    }

    private bool TryGetNearestGridPosition(Vector3 currentWorldPosition, Vector2Int footprintSize, out Vector2Int nearestGridPosition, out Vector3 nearestWorldPosition)
    {
        nearestGridPosition = default;
        nearestWorldPosition = default;

        var currentPosition = (Vector2)currentWorldPosition;
        var nearestDistanceSqr = float.MaxValue;
        var maxGridX = GameDefine.GridWidth - footprintSize.x;
        var maxGridY = GameDefine.GridHeight - footprintSize.y;

        for (int x = 0; x <= maxGridX; x++)
        {
            for (int y = 0; y <= maxGridY; y++)
            {
                var gridPosition = new Vector2Int(x, y);
                Vector3 tilePos = GridToWorldPosition(gridPosition, footprintSize);

                var distanceSqr = ((Vector2)tilePos - currentPosition).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestGridPosition = gridPosition;
                nearestWorldPosition = new Vector3(tilePos.x, tilePos.y, currentWorldPosition.z);
            }
        }

        return nearestDistanceSqr <= _snapDistance * _snapDistance;
    }

    private bool TryMovePreviewObjToDropPosition(Vector3 dropWorldPosition, out Vector3 snappedWorldPosition)
    {
        snappedWorldPosition = default;
        if (_previewObj == null ||
            !TryGetNearestGridPosition(dropWorldPosition, _previewObj.FootprintSize, out var nearestGridPosition, out snappedWorldPosition) ||
            !UserDataManager.Instance.User.CanPlaceFurniture(_previewFurnitureId, nearestGridPosition))
        {
            return false;
        }

        _previewGridPosition = nearestGridPosition;
        return true;
    }
    private bool TryMovePlaceableObjToDropPosition(long uid, Vector3 dropWorldPosition, out Vector3 snappedWorldPosition)
    {
        snappedWorldPosition = default;

        if (!UserDataManager.Instance.User.TryGetPlaceableObjData(uid, out var placeableObjData))
        {
            return false;
        }

        var footprintSize = new Vector2Int(placeableObjData.TableData.sizex, placeableObjData.TableData.sizey);
        if (!TryGetNearestGridPosition(dropWorldPosition, footprintSize, out var nearestGridPosition, out snappedWorldPosition))
        {
            return false;
        }

        return UserDataManager.Instance.TryMovePlaceableObj(uid, nearestGridPosition);
    }

    public void ChangeFloorTile(Vector2Int gridPos)
    {
        if (!IsValidPosition(gridPos))
        {
            return;
        }

        var tile = _floorTileObjs[gridPos.x, gridPos.y];
        tile.SetSprite(_stoneFloorSprite);
    }
    public bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < GameDefine.GridWidth && position.y >= 0 && position.y < GameDefine.GridHeight;
    }

    public bool IsWalkable(Vector2Int position)
    {
        if (UserDataManager.Instance.User.TryGetTileData(position.x, position.y, out var tileData))
        {
            return tileData.IsWalkable;
        }
        return false;
    }

    public bool TryFindPath(
        Vector2Int start,
        Vector2Int target,
        out List<Vector2Int> path)
    {
        if (_pathfinder == null)
        {
            path = null;
            return false;
        }

        return _pathfinder.TryFindPath(start, target, out path);
    }

    public bool TryFindWorldPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition,
        out List<Vector3> worldPath)
    {
        worldPath = null;

        if (!TryWorldToGridPosition(startWorldPosition, Vector2Int.one, out var start) ||
            !TryWorldToGridPosition(targetWorldPosition, Vector2Int.one, out var target) ||
            !TryFindPath(start, target, out var gridPath))
        {
            return false;
        }

        worldPath = new List<Vector3>(gridPath.Count);
        foreach (var gridPosition in gridPath)
        {
            worldPath.Add(GridToWorldPosition(gridPosition));
        }

        return true;
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return _gridRoot.TransformPoint(GridToWorld(gridPosition, Vector2Int.one));
    }
    public Vector3 GridToWorldPosition(Vector2Int gridPosition, Vector2Int footprintSize)
    {
        return _gridRoot.TransformPoint(GridToWorld(gridPosition, footprintSize));
    }
    public bool TryWorldToGridPosition(Vector3 worldPosition, Vector2Int footprintSize, out Vector2Int gridPosition)
    {
        gridPosition = default;

        if (_gridRoot == null ||
            Mathf.Approximately(_tileWidth, 0f) ||
            Mathf.Approximately(_tileHeight, 0f))
        {
            return false;
        }

        var localPosition = _gridRoot.InverseTransformPoint(worldPosition);
        localPosition -= GetFootprintCenterOffset(footprintSize);
        var gridX = localPosition.x / _tileWidth + localPosition.y / _tileHeight;
        var gridY = localPosition.y / _tileHeight - localPosition.x / _tileWidth;
        var nearestPosition = new Vector2Int(
            Mathf.RoundToInt(gridX),
            Mathf.RoundToInt(gridY));

        if (!IsValidPosition(nearestPosition))
        {
            return false;
        }

        gridPosition = nearestPosition;
        return true;
    }

    public NpcObj CreateNpc(Vector2Int gridPosition)
    {
        if (_npcObjPrefab == null || !IsWalkable(gridPosition))
        {
            return null;
        }

        var npc = Lean.Pool.LeanPool.Spawn(_npcObjPrefab, _gridRoot);
        npc.transform.localPosition = GridToWorld(gridPosition, Vector2Int.one);
        npc.transform.localRotation = Quaternion.identity;
        npc.Initialize(gridPosition);
        return npc;
    }

    public FloorTileObj GetFloorView(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return null;
        }

        return _floorTileObjs[position.x, position.y];
    }

    private Vector3 GridToWorld(Vector2Int gridPosition, Vector2Int footprintSize)
    {
        return GridUtil.GridToWorld(gridPosition, _tileWidth, _tileHeight) + GetFootprintCenterOffset(footprintSize);
    }
    private Vector3 GetFootprintCenterOffset(Vector2Int footprintSize)
    {
        var farthestTileOffset = footprintSize - Vector2Int.one;
        return GridUtil.GridToWorld(farthestTileOffset, _tileWidth, _tileHeight) * 0.5f;
    }

}
