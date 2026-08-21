using UnityEngine;

public interface IPointerInteractable
{
    void OnPointerDown(Vector2 worldPosition);
    void OnPointerDrag(Vector2 worldPosition);
    void OnPointerUp();
    void OnClick();
}
