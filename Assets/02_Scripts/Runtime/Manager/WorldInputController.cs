using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private ItemPlacementUI _itemPlacementUI;

    [Header("Interaction")]
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _dragThreshold = 3f;

    private bool _isPointerDown;
    private bool _isDraggingObject;

    private Vector2 _pointerDownPosition;
    private IPointerInteractable _pressedObject;
    private float _dragVisualOffset = 100f;

    private Camera WorldCamera => _cameraController.Camera;

    void Start()
    {
        _dragVisualOffset = Mathf.Clamp(Screen.height * 0.05f, 10f, 30f);

        if (_itemPlacementUI == null)
        {
            _itemPlacementUI = FindFirstObjectByType<ItemPlacementUI>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#endif
    }

    private void HandleMouseInput()
    {
        var mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 position = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandlePointerDown(position);
            return;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            HandlePointerUp();
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            HandlePointerMove(position);
        }
    }

    private void HandlePointerDown(Vector2 screenPosition)
    {
        if (_isPointerDown)
            return;

        if (IsPointerOverUI())
            return;

        _isPointerDown = true;
        _isDraggingObject = false;
        _pointerDownPosition = screenPosition;

        _pressedObject = FindInteractableObject(screenPosition);

        if (_pressedObject != null)
        {
            Vector2 worldPosition = ScreenToWorldPosition(screenPosition);
            _pressedObject.OnPointerDown(worldPosition);

            if (_pressedObject is Component pressedComponent)
            {
                _itemPlacementUI?.SetTarget(pressedComponent.transform, WorldCamera);
            }
        }
        else
        {
            _cameraController.BeginDrag(screenPosition);
        }
    }

    private void HandlePointerMove(Vector2 screenPosition)
    {
        if (!_isPointerDown)
            return;

        if (_pressedObject != null)
        {
            HandleObjectDrag(screenPosition + Vector2.up * 100);
        }
        else
        {
            _cameraController.UpdateDrag(screenPosition);
        }
    }

    private void HandleObjectDrag(Vector2 screenPosition)
    {
        if (!_isDraggingObject)
        {
            if (!HasExceededDragThreshold(screenPosition))
                return;

            _isDraggingObject = true;
        }

        Vector2 dragScreenPosition = screenPosition + Vector2.up * _dragVisualOffset;
        Vector2 worldPosition = ScreenToWorldPosition(dragScreenPosition);
        _pressedObject.OnPointerDrag(worldPosition);
    }

    private void HandlePointerUp()
    {
        if (!_isPointerDown)
            return;

        if (_pressedObject != null)
        {
            // 클릭이든 드래그든 PointerDown 된 객체가 PointerUp도 받도록 함.
            _pressedObject.OnPointerUp();
        }
        else
        {
            _cameraController.EndDrag();
        }

        ResetPointerState();
    }

    private void ResetPointerState()
    {
        _isPointerDown = false;
        _isDraggingObject = false;
        _pressedObject = null;
    }

    private bool HasExceededDragThreshold(Vector2 screenPosition)
    {
        Vector2 delta = screenPosition - _pointerDownPosition;

        // Distance()보다 sqrt 계산을 피할 수 있음.
        return delta.sqrMagnitude >= _dragThreshold * _dragThreshold;
    }

    private IPointerInteractable FindInteractableObject(Vector2 screenPosition)
    {
        Vector2 worldPosition = ScreenToWorldPosition(screenPosition);

        Collider2D collider = Physics2D.OverlapPoint(
            worldPosition,
            _interactableLayer
        );

        if (collider == null)
            return null;

        collider.TryGetComponent(out IPointerInteractable interactable);
        return interactable;
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
    {
        return WorldCamera.ScreenToWorldPoint(screenPosition);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}
