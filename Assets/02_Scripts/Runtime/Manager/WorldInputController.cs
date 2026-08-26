using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using R3;
using System.Linq;

public class WorldInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private ItemPlacementUI _itemPlacementUI;

    [Header("Interaction")]
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _dragThreshold = 3f;
    [SerializeField, Min(0f)] private float _longPressDuration = 2f;

    private bool _isPointerDown;
    private bool _isDraggingObject;
    private bool _isLongPressTriggered;

    private Vector2 _pointerDownPosition;
    private IPointerInteractable _pressedObject;
    private float _pointerDownTime;
    private float _dragVisualOffset = 100f;

    private Camera WorldCamera => _cameraController.Camera;

    void Start()
    {
        _dragVisualOffset = Mathf.Clamp(Screen.height * 0.05f, 10f, 30f);

        if (_itemPlacementUI == null)
        {
            _itemPlacementUI = FindFirstObjectByType<ItemPlacementUI>(FindObjectsInactive.Include);
        }

        GameManager.Instance.GameMode.Subscribe(mode =>
        {
            switch (mode)
            {
                case GameMode.Normal:
                    _itemPlacementUI.gameObject.SetActive(false);
                    break;
                case GameMode.Edit:
                    _itemPlacementUI.gameObject.SetActive(true);
                    break;
            }

        }).AddTo(gameObject);
    }

    private void Update()
    {
        if (HandleTouchInput())
        {
            return;
        }

        HandleMouseInput();
    }

    private bool HandleTouchInput()
    {
        var touchscreen = Touchscreen.current;

        if (touchscreen == null)
        {
            return false;
        }

        var touch = touchscreen.primaryTouch;
        var press = touch.press;

        if (!press.isPressed &&
            !press.wasPressedThisFrame &&
            !press.wasReleasedThisFrame)
        {
            return false;
        }

        Vector2 position = touch.position.ReadValue();

        if (press.wasPressedThisFrame)
        {
            HandlePointerDown(position, touch.touchId.ReadValue());
            return true;
        }

        if (press.wasReleasedThisFrame)
        {
            HandlePointerUp();
            return true;
        }

        HandlePointerMove(position);
        return true;
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

    private void HandlePointerDown(Vector2 screenPosition, int pointerId = -1)
    {
        if (_isPointerDown)
            return;

        if (IsPointerOverUI(pointerId))
            return;

        _isPointerDown = true;
        _isDraggingObject = false;
        _isLongPressTriggered = false;
        _pointerDownPosition = screenPosition;
        _pointerDownTime = Time.unscaledTime;

        _pressedObject = FindInteractableObject(screenPosition);
        _cameraController.BeginDrag(screenPosition);

        if (_pressedObject != null)
        {
            Vector2 worldPosition = ScreenToWorldPosition(screenPosition);
            _pressedObject.OnPointerDown(worldPosition);

            if (_pressedObject is Component pressedComponent)
            {
                _itemPlacementUI?.SetTarget(pressedComponent.transform, WorldCamera);
            }
        }
    }

    private void HandlePointerMove(Vector2 screenPosition)
    {
        if (!_isPointerDown)
            return;

        if (_pressedObject != null)
        {
            TryEnterEditMode(screenPosition);
            HandleObjectDrag(screenPosition);
            if (GameManager.Instance.GameMode.Value == GameMode.Normal)
            {
                _cameraController.UpdateDrag(screenPosition);
            }
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

        if (GameManager.Instance.GameMode.Value == GameMode.Edit)
        {
            Vector2 dragScreenPosition = screenPosition + Vector2.up * _dragVisualOffset;
            Vector2 worldPosition = ScreenToWorldPosition(dragScreenPosition);
            _pressedObject.OnPointerDrag(worldPosition);
        }
    }

    private void TryEnterEditMode(Vector2 screenPosition)
    {
        if (_isLongPressTriggered || _isDraggingObject || HasExceededDragThreshold(screenPosition) ||
            Time.unscaledTime - _pointerDownTime < _longPressDuration)
        {
            return;
        }

        _isLongPressTriggered = true;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.EnterEditMode();
        }
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
        _cameraController.EndDrag();

        ResetPointerState();
    }

    private void ResetPointerState()
    {
        _isPointerDown = false;
        _isDraggingObject = false;
        _isLongPressTriggered = false;
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

        var hit = Physics2D.OverlapPointAll(worldPosition, _interactableLayer)
            .Select(collider =>
            {
                collider.TryGetComponent(out IPointerInteractable interactable);
                collider.TryGetComponent(out Renderer renderer);
                return (interactable, renderer, collider.transform);
            })
            .Where(candidate => candidate.interactable != null)
            .OrderByDescending(candidate => GetSortingLayerValue(candidate.renderer))
            .ThenByDescending(candidate => candidate.renderer != null
                ? candidate.renderer.sortingOrder
                : int.MinValue)
            .ThenBy(candidate => GetSortPositionY(candidate.renderer, candidate.transform))
            .FirstOrDefault();

        return hit.interactable;
    }

    private static int GetSortingLayerValue(Renderer renderer)
    {
        return renderer != null
            ? SortingLayer.GetLayerValueFromID(renderer.sortingLayerID)
            : int.MinValue;
    }

    private static float GetSortPositionY(Renderer renderer, Transform fallbackTransform)
    {
        if (renderer == null)
        {
            return fallbackTransform.position.y;
        }

        if (renderer is SpriteRenderer spriteRenderer &&
            spriteRenderer.spriteSortPoint == SpriteSortPoint.Pivot)
        {
            return spriteRenderer.transform.position.y;
        }

        return renderer.bounds.center.y;
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
    {
        return WorldCamera.ScreenToWorldPoint(screenPosition);
    }

    private static bool IsPointerOverUI(int pointerId = -1)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }
}
