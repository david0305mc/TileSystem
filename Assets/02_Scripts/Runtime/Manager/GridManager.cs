
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
    [SerializeField] private Transform _floorRoot;
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private Sprite _defaultFloorSprite;
    [SerializeField] private Sprite _stoneFloorSprite;

    private FloorTileObj[,] _floorTileObjs;
    private Camera _mainCamera;

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

        CreateFloorTileObjs();
        GenerateBuildingRandom();
    }
    private void GenerateBuildingRandom()
    {
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(0, GameDefine.GridWidth);
            int y = Random.Range(0, GameDefine.GridHeight);

            CreateBuildingObj(new Vector2Int(x, y));
        }
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
    private void CreateBuildingObj(Vector2Int gridPos)
    {
        var localPos = GridToWorld(gridPos);
        var placeableObj = Lean.Pool.LeanPool.Spawn(_placeableObjPrefab);
        placeableObj.transform.localPosition = localPos;
        placeableObj.transform.localRotation = Quaternion.identity;
        
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
