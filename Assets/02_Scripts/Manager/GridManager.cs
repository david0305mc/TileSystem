using Unity.Mathematics;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int _width = 50;
    [SerializeField] private int _height = 50;

    [Header("Tile Size")]
    [SerializeField] private float _tileWidth = 1f;
    [SerializeField] private float _tileHeight = 0.5f;

    [Header("References")]
    [SerializeField] private FloorTileObj _floorPrefab;
    [SerializeField] private Transform _floorRoot;
    [SerializeField] private Sprite _defaultFloorSprite;

    private CellData[,] _cellDatas;
    private FloorTileObj[,] _floorTileObjs;

    public int Width => _width;
    public int Height => _height;

    void Start()
    {
        Initialize();
    }
    private void Initialize()
    {
        _cellDatas = new CellData[_width, _height];
        _floorTileObjs = new FloorTileObj[_width, _height];

        CreateCellData();
        CreateFloorTileObjs();
    }

    private void CreateCellData()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var pos = new Vector2Int(x, y);
                _cellDatas[x, y] = new CellData(pos);
            }
        }
    }

    private void CreateFloorTileObjs()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                CreateFloorTileObj(new Vector2Int(x, y));
            }
        }
    }

    private void CreateFloorTileObj(Vector2Int gridPos)
    {
        var worldPos = GridUtil.GridToWorld(gridPos, _tileWidth, _tileHeight);
        var tileObj = Instantiate(_floorPrefab, worldPos, quaternion.identity, _floorRoot);
        tileObj.Initialize(gridPos, FloorType.Default);
        _floorTileObjs[gridPos.x, gridPos.y] = tileObj;
    }
    public bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < Width &&
               position.y >= 0 &&
               position.y < Height;
    }
    public CellData GetCell(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return null;
        }

        return _cellDatas[position.x, position.y];
    }

    public FloorTileObj GetFloorView(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return null;
        }

        return _floorTileObjs[position.x, position.y];
    }
}
