using UnityEngine;

public class PlaceableObj : MonoBehaviour, IPointerInteractable
{
    [SerializeField, Min(0f)] private float _snapDistance = 0.5f;
    
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 _positionBeforeDrag;
    private System.Action<long> _pointUpAction;
    private PlaceableObjData _placeableObjData;
    public long Uid=>_placeableObjData.Uid;

    public void OnClick()
    {

    }
    public void Initialize(PlaceableObjData placeableObjData, System.Action<long> pointUpAction)
    {
        _placeableObjData = placeableObjData;
        _pointUpAction = pointUpAction;
        var tableData = DataManager.Instance.GetFurnitureData(placeableObjData.TableID);
        spriteRenderer.sprite = ResourceManager.Instance.GetSpriteFromAtlas(tableData.spritepath);
    }

    public void OnPointerDown(Vector2 worldPosition)
    {
        _positionBeforeDrag = transform.position;
    }

    public void OnPointerDrag(Vector2 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void OnPointerUp()
    {
        if (TryGetNearestGridPosition(out var gridPosition))
        {
            transform.position = gridPosition;
            _pointUpAction?.Invoke(_placeableObjData.Uid);
            return;
        }

        transform.position = _positionBeforeDrag;
    }

    private bool TryGetNearestGridPosition(out Vector3 gridPosition)
    {
        gridPosition = default;

        if (!GridManager.HasInstance)
        {
            return false;
        }

        var gridManager = GridManager.Instance;
        var currentPosition = (Vector2)transform.position;
        var nearestDistanceSqr = float.MaxValue;

        for (int x = 0; x < GameDefine.GridWidth; x++)
        {
            for (int y = 0; y < GameDefine.GridHeight; y++)
            {
                var floorView = gridManager.GetFloorView(new Vector2Int(x, y));
                if (floorView == null)
                {
                    continue;
                }

                var floorPosition = floorView.transform.position;
                var distanceSqr = ((Vector2)floorPosition - currentPosition).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                gridPosition = new Vector3(floorPosition.x, floorPosition.y, transform.position.z);
            }
        }

        return nearestDistanceSqr <= _snapDistance * _snapDistance;
    }
}
