using UnityEngine;

public class PlaceableObj : MonoBehaviour, IPointerInteractable
{
    public void OnClick()
    {
        
    }

    public void OnPointerDown(Vector2 worldPosition)
    {
        
    }

    public void OnPointerDrag(Vector2 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void OnPointerUp()
    {
        
    }
}
