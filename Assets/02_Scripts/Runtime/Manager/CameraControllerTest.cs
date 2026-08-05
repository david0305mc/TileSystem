using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControllerTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;

    [Header("World Bounds")]
    [SerializeField] private Vector2 _worldMin;
    [SerializeField] private Vector2 _worldMax;

    [Header("Inertia")]
    [SerializeField, Min(0f)]
    private float _cameraDamping = 8f;

    [SerializeField, Min(0f)]
    private float _velocitySmoothing = 20f;

    [SerializeField, Min(0f)]
    private float _stopSpeed = 0.01f;

    [Header("Drag Plane")]
    [SerializeField]
    private float _dragPlaneZ = 0f;

    private bool _isDragging;
    private Vector2 _previousPointerPosition;
    private Vector3 _velocity;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
        {
            Debug.LogError("Camera reference is missing.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        HandleMouseDrag();

        if (!_isDragging)
            ApplyInertia();

        ClampCameraPosition();
    }

    private void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 pointerPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginDrag(pointerPosition);
        }
        else if (mouse.leftButton.isPressed)
        {
            UpdateDrag(pointerPosition);
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }

    private void BeginDrag(Vector2 pointerPosition)
    {
        _isDragging = true;
        _velocity = Vector3.zero;
        _previousPointerPosition = pointerPosition;
    }

    private void UpdateDrag(Vector2 pointerPosition)
    {
        if (!_isDragging)
            return;

        Vector3 previousWorldPosition =
            ScreenToWorldPosition(_previousPointerPosition);

        Vector3 currentWorldPosition =
            ScreenToWorldPosition(pointerPosition);

        Vector3 delta = previousWorldPosition - currentWorldPosition;
        delta.z = 0f;

        transform.position += delta;

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 currentVelocity = delta / deltaTime;

        // 프레임레이트에 덜 민감한 지수 보간
        float smoothing =
            1f - Mathf.Exp(-_velocitySmoothing * deltaTime);

        _velocity = Vector3.Lerp(
            _velocity,
            currentVelocity,
            smoothing);

        _previousPointerPosition = pointerPosition;
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void ApplyInertia()
    {
        transform.position += _velocity * Time.deltaTime;

        // 지수 감쇠이므로 프레임레이트 변화에 비교적 안정적
        float damping =
            Mathf.Exp(-_cameraDamping * Time.deltaTime);

        _velocity *= damping;

        if (_velocity.sqrMagnitude <= _stopSpeed * _stopSpeed)
            _velocity = Vector3.zero;
    }

    private void ClampCameraPosition()
    {
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;

        float minX = _worldMin.x + halfWidth;
        float maxX = _worldMax.x - halfWidth;
        float minY = _worldMin.y + halfHeight;
        float maxY = _worldMax.y - halfHeight;

        Vector3 position = transform.position;
        Vector3 clampedPosition = position;

        // 월드가 카메라 화면보다 작을 때 Clamp의 min > max 문제 방지
        clampedPosition.x = minX <= maxX
            ? Mathf.Clamp(position.x, minX, maxX)
            : (_worldMin.x + _worldMax.x) * 0.5f;

        clampedPosition.y = minY <= maxY
            ? Mathf.Clamp(position.y, minY, maxY)
            : (_worldMin.y + _worldMax.y) * 0.5f;

        // 경계 바깥쪽을 향하는 관성만 제거
        if (clampedPosition.x != position.x)
        {
            bool movingOutward =
                position.x < minX && _velocity.x < 0f ||
                position.x > maxX && _velocity.x > 0f;

            if (movingOutward)
                _velocity.x = 0f;
        }

        if (clampedPosition.y != position.y)
        {
            bool movingOutward =
                position.y < minY && _velocity.y < 0f ||
                position.y > maxY && _velocity.y > 0f;

            if (movingOutward)
                _velocity.y = 0f;
        }

        transform.position = clampedPosition;
    }

    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        float distanceToPlane =
            Mathf.Abs(_dragPlaneZ - _camera.transform.position.z);

        return _camera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceToPlane));
    }
}