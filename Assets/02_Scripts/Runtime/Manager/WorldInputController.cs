using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldInputController : MonoBehaviour
{

    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
    }
    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        // if (mouse.leftButton.wasPressedThisFrame)
        // {
        //     Debug.Log("Point Down");
        // }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            var screenPos = mouse.position.ReadValue();
            var worldPos = _camera.ScreenToWorldPoint(screenPos);

            Collider2D collider = Physics2D.OverlapPoint(worldPos);
            if (collider != null)
            {
                Debug.Log($"collider {collider.name}");
            }

        }

    }
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
