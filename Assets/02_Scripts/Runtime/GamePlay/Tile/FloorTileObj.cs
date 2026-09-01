using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class FloorTileObj : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    private Vector2Int _gridPos;
    public Vector2Int GridPos => _gridPos;

    private System.Action _touchAction;

    private static readonly Color NormalColor = Color.white;
    private static readonly Color OccupiedColor = Color.red;
    private static readonly Color AvailableColor = Color.green;
    public void Initialize(Vector2Int gridPos, Sprite sprite, System.Action touchAction)
    {
        _touchAction = touchAction;
        name = $"Floor_{gridPos.x}_{gridPos.y}";
        _gridPos = gridPos;
        _spriteRenderer.sprite = sprite;
        UserDataManager.Instance.User.TryGetTileData(gridPos, out var tileData);
        GameManager.Instance.EditMode.Subscribe(mode =>
        {
            switch (mode)
            {
                case EditMode.Normal:
                    _spriteRenderer.color = NormalColor;
                    break;
                case EditMode.Floor:
                    {
                        if (tileData.IsOccupied)
                        {
                            _spriteRenderer.color = OccupiedColor;
                        }
                        else
                        {
                            _spriteRenderer.color = AvailableColor;
                        }
                    }
                    break;
            }

        }).AddTo(gameObject);

    }
    public void SetSprite(Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
    }
    private void OnMouseDown()
    {
        _touchAction?.Invoke();
    }
}
