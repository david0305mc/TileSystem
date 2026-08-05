// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.InputSystem;

// public sealed class WorldInputTest : MonoBehaviour
// {
//     [SerializeField] private Camera _camera;
//     [SerializeField] private CameraController _cameraController;
//     [SerializeField] private LayerMask _interactableLayer;
//     [SerializeField] private float _dragThreshold = 10f;

//     private Vector2 _pointerDownPosition;
//     private Vector2 _previousPointerPosition;

//     private IPointerInteractable _pressedObject;

//     private bool _isPointerDown;
//     private bool _isDraggingObject;
//     private bool _isDraggingCamera;

//     private void Update()
//     {
//         Mouse mouse = Mouse.current;

//         if (mouse == null)
//         {
//             return;
//         }

//         Vector2 screenPosition = mouse.position.ReadValue();

//         if (mouse.leftButton.wasPressedThisFrame)
//         {
//             HandlePointerDown(screenPosition);
//         }

//         if (mouse.leftButton.isPressed)
//         {
//             HandlePointerMove(screenPosition);
//         }

//         if (mouse.leftButton.wasReleasedThisFrame)
//         {
//             HandlePointerUp(screenPosition);
//         }
//     }

//     private void HandlePointerDown(Vector2 screenPosition)
//     {
//         if (IsPointerOverUI())
//         {
//             return;
//         }

//         _isPointerDown = true;
//         _pointerDownPosition = screenPosition;
//         _previousPointerPosition = screenPosition;

//         Vector2 worldPosition =
//             _camera.ScreenToWorldPoint(screenPosition);

//         _pressedObject = FindInteractable(worldPosition);
//         _pressedObject?.OnPointerDown(worldPosition);
//     }

//     private void HandlePointerMove(Vector2 screenPosition)
//     {
//         if (!_isPointerDown)
//         {
//             return;
//         }

//         if (!_isDraggingObject &&
//             !_isDraggingCamera &&
//             HasExceededDragThreshold(screenPosition))
//         {
//             if (_pressedObject != null)
//             {
//                 _isDraggingObject = true;
//             }
//             else
//             {
//                 _isDraggingCamera = true;
//                 _cameraController.BeginExternalDrag(
//                     _previousPointerPosition);
//             }
//         }

//         Vector2 worldPosition =
//             _camera.ScreenToWorldPoint(screenPosition);

//         if (_isDraggingObject)
//         {
//             _pressedObject.OnPointerDrag(worldPosition);
//         }
//         else if (_isDraggingCamera)
//         {
//             _cameraController.UpdateExternalDrag(
//                 screenPosition);
//         }

//         _previousPointerPosition = screenPosition;
//     }

//     private void HandlePointerUp(Vector2 screenPosition)
//     {
//         if (!_isPointerDown)
//         {
//             return;
//         }

//         Vector2 worldPosition =
//             _camera.ScreenToWorldPoint(screenPosition);

//         if (_isDraggingObject)
//         {
//             _pressedObject?.OnPointerUp(worldPosition);
//         }
//         else if (_isDraggingCamera)
//         {
//             _cameraController.EndExternalDrag();
//         }
//         else
//         {
//             _pressedObject?.OnClick();
//         }

//         ResetPointerState();
//     }

//     private IPointerInteractable FindInteractable(
//         Vector2 worldPosition)
//     {
//         RaycastHit2D hit = Physics2D.Raycast(
//             worldPosition,
//             Vector2.zero,
//             0f,
//             _interactableLayer);

//         if (hit.collider == null)
//         {
//             return null;
//         }

//         hit.collider.TryGetComponent(
//             out IPointerInteractable interactable);

//         return interactable;
//     }

//     private bool HasExceededDragThreshold(
//         Vector2 currentPosition)
//     {
//         return Vector2.Distance(
//             _pointerDownPosition,
//             currentPosition) >= _dragThreshold;
//     }

//     private void ResetPointerState()
//     {
//         _pressedObject = null;
//         _isPointerDown = false;
//         _isDraggingObject = false;
//         _isDraggingCamera = false;
//     }

//     private bool IsPointerOverUI()
//     {
//         return EventSystem.current != null &&
//                EventSystem.current.IsPointerOverGameObject();
//     }
// }