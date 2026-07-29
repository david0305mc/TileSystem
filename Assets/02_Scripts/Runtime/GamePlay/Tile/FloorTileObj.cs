using UnityEngine;

public class FloorTileObj : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    private Vector2Int _gridPos;
    public Vector2Int GridPos => _gridPos;

    private System.Action _touchAction;
    public void Initialize(Vector2Int gridPos, Sprite sprite, System.Action touchAction)
    {
        touchAction = _touchAction;
        name = $"Floor_{gridPos.x}_{gridPos.y}";
        _gridPos = gridPos;
        _spriteRenderer.sprite = sprite;
    }
    public void SetSprite(Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
    }
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        _touchAction?.Invoke();
    }
}
