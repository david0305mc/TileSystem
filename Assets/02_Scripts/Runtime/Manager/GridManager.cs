
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : SingletonMono<GridManager>
{

    [Header("Tile Size")]
    [SerializeField] private float _tileWidth = 1f;
    [SerializeField] private float _tileHeight = 0.5f;

    [Header("References")]
    [SerializeField] private FloorTileObj _floorPrefab;
    [SerializeField] private PlaceableObj _placeableObjPrefab;
    [SerializeField] private NpcObj _npcObjPrefab;
    [SerializeField] private Transform _floorRoot;
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private Sprite _defaultFloorSprite;
    [SerializeField] private Sprite _stoneFloorSprite;

    private FloorTileObj[,] _floorTileObjs;
    private bool[,] _blockedCells;
    private AStarPathfinder _pathfinder;
    private Camera _mainCamera;

    public AStarPathfinder Pathfinder => _pathfinder;

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
        _blockedCells = new bool[GameDefine.GridWidth, GameDefine.GridHeight];
        _pathfinder = new AStarPathfinder(
            GameDefine.GridWidth,
            GameDefine.GridHeight,
            IsWalkable);

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
        var localPos = GridToWorld(gridPos);
        var tileObj = Instantiate(_floorPrefab, _floorRoot);
        tileObj.transform.localPosition = localPos;
        tileObj.transform.localRotation = Quaternion.identity;
        tileObj.Initialize(gridPos, _defaultFloorSprite, () =>
        {
            ChangeFloorTile(gridPos);
        });
        _floorTileObjs[gridPos.x, gridPos.y] = tileObj;
    }
    private void CreateBuildingObj(long uid)
    {
        UserDataManager.Instance.User.TryGetPlaceableObjData(uid, out var placeableObjData);
        var gridPos = new Vector2Int(placeableObjData.GridX, placeableObjData.GridY);
        var localPos = GridToWorld(gridPos);
        
        PlaceableObj placeableObj = Lean.Pool.LeanPool.Spawn(_placeableObjPrefab, _gridRoot);
        placeableObj.Initialize(placeableObjData, gridPos =>
        {
            
        });
        placeableObj.transform.localPosition = localPos;
        placeableObj.transform.localRotation = Quaternion.identity;
        SetBlocked(gridPos, true);
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
        return IsValidPosition(position) && (_blockedCells == null || !_blockedCells[position.x, position.y]);
    }

    public bool SetBlocked(Vector2Int position, bool blocked)
    {
        if (!IsValidPosition(position) || _blockedCells == null)
        {
            return false;
        }

        _blockedCells[position.x, position.y] = blocked;
        return true;
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

        if (!TryWorldToGridPosition(startWorldPosition, out var start) ||
            !TryWorldToGridPosition(targetWorldPosition, out var target) ||
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
        return _gridRoot.TransformPoint(GridToWorld(gridPosition));
    }

    public bool TryWorldToGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
    {
        gridPosition = default;

        if (_gridRoot == null ||
            Mathf.Approximately(_tileWidth, 0f) ||
            Mathf.Approximately(_tileHeight, 0f))
        {
            return false;
        }

        var localPosition = _gridRoot.InverseTransformPoint(worldPosition);
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
        npc.transform.localPosition = GridToWorld(gridPosition);
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

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return GridUtil.GridToWorld(gridPosition, _tileWidth, _tileHeight);
    }
}
