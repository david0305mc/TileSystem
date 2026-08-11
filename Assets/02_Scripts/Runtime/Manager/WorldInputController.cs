using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldInputController : MonoBehaviour
{
    [SerializeField] private LayerMask _InteractableLayer;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] float _dragThresHold = 3f;

    private bool _isDragging;
    private bool _isDraggingObject;
    private Vector2 _pointDownPosition;
    private IPointerInteractable _pressedObj;

    void Update()
    {

#if UNITY_EDITOR
        Mouse mouse = Mouse.current;

        HandleMouseDrag();
#endif
        // _cameraController.HandleMouseDrag();
        // _cameraController.HandleMouseZoom();

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
        _pointDownPosition = point;
        _isDragging = true;
        var interactableObj = FindInteractableObj(point);
        if (interactableObj != null)
        {
            var worldPos = _cameraController.Camera.ScreenToWorldPoint(point);
            _pressedObj = interactableObj;
            _pressedObj.OnPointerDown(worldPos);
        }
        else
        {
            _cameraController.BeginDrag(point);
        }
    }

    private void HandleDragUpdate(Vector2 point)
    {
        if (!_isDragging)
            return;

        if (_pressedObj != null)
        {
            if (!_isDraggingObject && HasExceededDragThresHold(point))
            {
                _isDraggingObject = true;
            }
            else
            {
                var worldPos = _cameraController.Camera.ScreenToWorldPoint(point);
                _pressedObj.OnPointerDrag(worldPos);
            }
        }
        else
        {
            _cameraController.UpdateDrag(point);
        }
    }

    private void HandleDragEnd()
    {
        if (!_isDragging)
            return;

        if (_isDraggingObject)
        {
            _pressedObj.OnPointerUp();
        }
        else
        {
            _cameraController.EndDrag();
        }
        _isDragging = false;
        _isDraggingObject = false;
        _pressedObj = null;
    }

    bool HasExceededDragThresHold(Vector2 point)
    {
        return Vector2.Distance(_pointDownPosition, point) >= _dragThresHold;
    }
    IPointerInteractable FindInteractableObj(Vector2 point)
    {
        var worldPos = _cameraController.Camera.ScreenToWorldPoint(point);
        var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0, _InteractableLayer);
        if (hit.collider == null)
        {
            return null;
        }
        hit.collider.gameObject.TryGetComponent(out IPointerInteractable interactableObj);
        return interactableObj;
    }

}
