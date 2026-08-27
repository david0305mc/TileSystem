using UnityEngine;

public class PlaceableObj : MonoBehaviour, IPointerInteractable
{
    public delegate bool TryDropAction(long uid, Vector3 dropWorldPosition, out Vector3 snappedWorldPosition);

    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 _positionBeforeDrag;
    private TryDropAction _tryDropAction;
    private PlaceableObjData _placeableObjData;
    public long Uid => _placeableObjData.Uid;
    public Vector2Int FootprintSize { get; private set; } = Vector2Int.one;

    public void OnClick()
    {

    }

    public void Initialize(PlaceableObjData placeableObjData, TryDropAction tryDropAction)
    {
        _placeableObjData = placeableObjData;
        _tryDropAction = tryDropAction;
        var tableData = DataManager.Instance.GetFurnitureData(placeableObjData.TableID);
        spriteRenderer.sprite = ResourceManager.Instance.GetSpriteFromAtlas(tableData.spritepath);
        FootprintSize = new Vector2Int(tableData.sizex, tableData.sizey);
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
        if (_tryDropAction != null && _tryDropAction.Invoke(Uid, transform.position, out var snappedWorldPosition))
        {
            transform.position = snappedWorldPosition;
            return;
        }

        transform.position = _positionBeforeDrag;
    }
}
