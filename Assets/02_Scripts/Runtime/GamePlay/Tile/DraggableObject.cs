using UnityEngine;

public interface IPointerInteractable
{
    void OnPointerDown(Vector2 worldPosition);
    void OnPointerDrag(Vector2 worldPosition);
    void OnPointerUp();
    void OnClick();
}

public class DraggableObject : MonoBehaviour, IPointerInteractable
{
    public void OnPointerDown(Vector2 worldPosition)
    {
        Debug.Log("오브젝트 누름");
    }

    public void OnPointerDrag(Vector2 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void OnPointerUp()
    {
        Debug.Log("오브젝트 드래그 종료");
    }

    public void OnClick()
    {
        Debug.Log("오브젝트 클릭");
    }
}