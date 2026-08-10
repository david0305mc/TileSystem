using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldInputController : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;
    private Camera _camera;

    private bool _isDragging;

    void Start()
    {
        _camera = Camera.main;
    }
    void Update()
    {

#if UNITY_EDITOR
        Mouse mouse = Mouse.current;

        HandleMouseDrag();
#endif
        cameraController.HandleMouseDrag();
        cameraController.HandleMouseZoom();

        // if (mouse.leftButton.wasPressedThisFrame)
        // {
        //     Debug.Log("Point Down");
        // }

        // if (mouse.leftButton.wasReleasedThisFrame)
        // {
        //     var screenPos = mouse.position.ReadValue();
        //     var worldPos = _camera.ScreenToWorldPoint(screenPos);

        //     Collider2D collider = Physics2D.OverlapPoint(worldPos);
        //     if (collider != null)
        //     {
        //         Debug.Log($"collider {collider.name}");
        //     }
        // }

    }
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;
        Vector2 mousePosition = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            HandleDragBegin(mousePosition);
        }
        else if (mouse.leftButton.isPressed)
        {
            HandleDragUpdate(mousePosition);
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            HandleDragEnd();
        }
    }

    private void HandleDragBegin(Vector2 point)
    {
        if (_isDragging)
            return;
        _isDragging = true;
    }

    private void HandleDragUpdate(Vector2 point)
    {
        if (!_isDragging)
            return;
    }

    private void HandleDragEnd()
    {
        if (!_isDragging)
            return;
        _isDragging = false;
    }

}
