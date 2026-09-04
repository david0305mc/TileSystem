using UnityEngine;

public class PlaceableObj : MonoBehaviour, IPointerInteractable, IWorldHudTarget
{
    [SerializeField] private Transform _hudAnchor;
    public delegate bool TryDropAction(long uid, Vector3 dropWorldPosition, out Vector3 snappedWorldPosition);
    public delegate bool TryPreviewDropAction(Vector3 dropWorldPosition, out Vector3 snappedWorldPosition);

    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 _positionBeforeDrag;
    private TryPreviewDropAction _tryDropAction;
    public long Uid { get; private set; }
    public Vector2Int FootprintSize { get; private set; } = Vector2Int.one;

    public Transform HudAnchor => _hudAnchor == null ? transform : _hudAnchor;

    public void OnClick()
    {

    }

    public void Initialize(PlaceableObjData placeableObjData, TryDropAction tryDropAction)
    {
        Uid = placeableObjData.Uid;
        _tryDropAction = (Vector3 dropWorldPosition, out Vector3 snappedWorldPosition) =>
            tryDropAction(Uid, dropWorldPosition, out snappedWorldPosition);
        InitializeVisual(placeableObjData.TableID);
    }

    public void InitializePreview(int furnitureId, TryPreviewDropAction tryDropAction)
    {
        Uid = 0;
        _tryDropAction = tryDropAction;
        InitializeVisual(furnitureId);
    }

    private void InitializeVisual(int furnitureId)
    {
        var tableData = DataManager.Instance.GetFurnitureData(furnitureId);
        spriteRenderer.sprite = ResourceManager.Instance.GetSpriteFromAtlas(tableData.spritepath);
        FootprintSize = new Vector2Int(
            Mathf.Max(1, tableData.sizex),
            Mathf.Max(1, tableData.sizey));
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
        if (_tryDropAction != null && _tryDropAction.Invoke(transform.position, out var snappedWorldPosition))
        {
            transform.position = snappedWorldPosition;
            return;
        }

        transform.position = _positionBeforeDrag;
    }
}
