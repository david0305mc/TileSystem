using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerTest : MonoBehaviour
{

    [SerializeField] private Camera _camera;

    [SerializeField] private Vector2 _worldMin;
    [SerializeField] private Vector2 _worldMax;

    private bool _isDragging;
    private Vector2 _previousPosition;

    void Awake()
    {
        _camera = Camera.main;
    }

    void Update()
    {
        HandleMouseDrag();
    }

    private void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;

        Vector2 currenPointerPosition = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginDrag(currenPointerPosition);
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            UpdateDrag(currenPointerPosition);
            return;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
            return;
        }
    }



    private void BeginDrag(Vector2 pointerPosition)
    {
        _isDragging = true;
        _previousPosition = pointerPosition;
    }

    private void UpdateDrag(Vector2 pointerPosition)
    {
        if (!_isDragging)
            return;

        Vector3 prevWorldPos = ScreenToWorldPosition(_camera, _previousPosition);
        Vector3 currWorldPos = ScreenToWorldPosition(_camera, pointerPosition);

        var delta = prevWorldPos - currWorldPos;
        _camera.transform.position += new Vector3(delta.x, delta.y, 0);
        ClampCameraPosition();

        _previousPosition = pointerPosition;
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void ClampCameraPosition()
    {
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;

        var x = Mathf.Clamp(_camera.transform.position.x, _worldMin.x + halfWidth, _worldMax.x - halfWidth);
        var y = Mathf.Clamp(_camera.transform.position.y, _worldMin.y + halfHeight, _worldMax.y - halfHeight);
        _camera.transform.position = new Vector3(x, y, _camera.transform.position.z);
    }

    public static Vector3 ScreenToWorldPosition(Camera camera, Vector3 worldPos)
    {
        return camera.ScreenToWorldPoint(new Vector3(worldPos.x, worldPos.y, Mathf.Abs(camera.transform.position.z)));
    }
}
