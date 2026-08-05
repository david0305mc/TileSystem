using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CameraControllerTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera _camera;

    [Header("World Bounds")]
    [SerializeField]
    private Vector2 _worldMin;

    [SerializeField]
    private Vector2 _worldMax;

    [Header("Inertia")]
    [SerializeField, Min(0f)]
    private float _cameraDamping = 8f;

    [SerializeField, Min(0f)]
    private float _velocitySmoothing = 20f;

    [SerializeField, Min(0f)]
    private float _stopSpeed = 0.01f;

    [Header("Zoom")]
    [SerializeField, Min(0.01f)]
    private float _minZoom = 2f;

    [SerializeField, Min(0.01f)]
    private float _maxZoom = 10f;

    [Tooltip("마우스 휠 한 칸당 변경되는 Orthographic Size입니다.")]
    [SerializeField, Min(0f)]
    private float _mouseZoomSpeed = 1f;

    [Tooltip("핀치 거리 변화에 대한 줌 민감도입니다.")]
    [SerializeField, Min(0.01f)]
    private float _pinchZoomSensitivity = 1f;

    [Tooltip("카메라 화면이 월드 영역보다 커지지 않도록 최대 줌아웃을 제한합니다.")]
    [SerializeField]
    private bool _limitZoomToWorldBounds = true;

    [Header("Drag Plane")]
    [SerializeField]
    private float _dragPlaneZ = 0f;

    [Header("Mobile")]
    [Tooltip("UI 위에서 조작을 시작하지 못하게 하려면 별도의 UI 체크 로직을 추가하세요.")]
    [SerializeField]
    private bool _enableTouchInput = true;

    private bool _isDragging;
    private bool _isPinching;

    private int _activeTouchId = -1;

    private Vector2 _previousPointerPosition;
    private float _previousPinchDistance;

    private Vector3 _velocity;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
        {
            Debug.LogError("Camera reference is missing.", this);
            enabled = false;
            return;
        }

        if (!_camera.orthographic)
        {
            Debug.LogError(
                "CameraControllerTest requires an orthographic camera.",
                this);

            enabled = false;
            return;
        }

        ValidateZoomRange();
        SetZoom(_camera.orthographicSize);
    }

    private void OnValidate()
    {
        ValidateZoomRange();
    }

    private void OnDisable()
    {
        CancelInput();
    }
    private void Update()
    {
        HandleTouchAndMouse();
        if (!_isDragging && !_isPinching)
            ApplyInertia();

        ClampCameraPosition();
    }

    private void HandleTouchAndMouse()
    {
        bool touchHandled = false;
        if (_enableTouchInput)
            touchHandled = HandleTouchInput();

        if (!touchHandled)
            HandleMouseInput();
    }

    private bool HandleTouchInput()
    {
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen == null)
            return false;

        TouchControl firstTouch = null;
        TouchControl secondTouch = null;
        int pressedTouchCount = 0;

        foreach (TouchControl touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            pressedTouchCount++;

            if (firstTouch == null)
            {
                firstTouch = touch;
            }
            else if (secondTouch == null)
            {
                secondTouch = touch;
            }
        }

        // 두 개 이상의 터치가 눌려 있으면 핀치 줌으로 처리한다.
        if (pressedTouchCount >= 2 &&
            firstTouch != null &&
            secondTouch != null)
        {
            Vector2 firstPosition = firstTouch.position.ReadValue();
            Vector2 secondPosition = secondTouch.position.ReadValue();

            if (!_isPinching)
            {
                BeginPinch(firstPosition, secondPosition);
            }
            else
            {
                UpdatePinch(firstPosition, secondPosition);
            }

            return true;
        }

        // 핀치가 끝나고 한 손가락만 남은 경우,
        // 남은 손가락 위치에서 드래그를 새로 시작해 점프를 방지한다.
        if (_isPinching)
        {
            EndPinch();

            if (firstTouch != null)
            {
                _activeTouchId = firstTouch.touchId.ReadValue();
                BeginDrag(firstTouch.position.ReadValue());
            }

            return true;
        }

        // 현재 추적 중인 단일 터치 처리
        if (_activeTouchId >= 0)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                int touchId = touch.touchId.ReadValue();

                if (touchId != _activeTouchId)
                    continue;

                Vector2 pointerPosition = touch.position.ReadValue();

                if (touch.press.wasReleasedThisFrame ||
                    !touch.press.isPressed)
                {
                    EndDrag();
                    _activeTouchId = -1;
                }
                else
                {
                    UpdateDrag(pointerPosition);
                }

                return true;
            }

            // 추적 중인 터치를 찾지 못한 경우
            EndDrag();
            _activeTouchId = -1;

            return pressedTouchCount > 0;
        }

        // 새 단일 터치 시작
        if (firstTouch != null)
        {
            _activeTouchId = firstTouch.touchId.ReadValue();
            BeginDrag(firstTouch.position.ReadValue());

            return true;
        }

        return false;
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        Vector2 pointerPosition = mouse.position.ReadValue();

        HandleMouseZoom(mouse, pointerPosition);

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

    private void HandleMouseZoom(
        Mouse mouse,
        Vector2 pointerPosition)
    {
        float scrollY = mouse.scroll.ReadValue().y;

        if (Mathf.Approximately(scrollY, 0f))
            return;

        // 일반적인 마우스 휠은 한 칸에 약 120 값을 반환한다.
        // 트랙패드 등에서는 더 작은 연속 값이 들어올 수 있다.
        float normalizedScroll = scrollY / 120f;

        float targetZoom =
            _camera.orthographicSize -
            normalizedScroll * -_mouseZoomSpeed;

        ZoomAtScreenPosition(targetZoom, pointerPosition);
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

        Vector3 delta =
            previousWorldPosition - currentWorldPosition;

        delta.z = 0f;

        transform.position += delta;

        float deltaTime =
            Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 currentVelocity =
            delta / deltaTime;

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

    private void BeginPinch(
        Vector2 firstPosition,
        Vector2 secondPosition)
    {
        _isPinching = true;
        _isDragging = false;
        _activeTouchId = -1;
        _velocity = Vector3.zero;

        _previousPinchDistance =
            Vector2.Distance(firstPosition, secondPosition);
    }

    private void UpdatePinch(
        Vector2 firstPosition,
        Vector2 secondPosition)
    {
        if (!_isPinching)
            return;

        float currentPinchDistance =
            Vector2.Distance(firstPosition, secondPosition);

        if (_previousPinchDistance <= Mathf.Epsilon ||
            currentPinchDistance <= Mathf.Epsilon)
        {
            _previousPinchDistance = currentPinchDistance;
            return;
        }

        Vector2 pinchCenter =
            (firstPosition + secondPosition) * 0.5f;

        // 손가락 사이가 넓어지면 줌인,
        // 손가락 사이가 좁아지면 줌아웃한다.
        float distanceRatio =
            _previousPinchDistance / currentPinchDistance;

        float adjustedRatio =
            Mathf.Pow(distanceRatio, _pinchZoomSensitivity);

        float targetZoom =
            _camera.orthographicSize * adjustedRatio;

        ZoomAtScreenPosition(targetZoom, pinchCenter);

        _previousPinchDistance = currentPinchDistance;
    }

    private void EndPinch()
    {
        _isPinching = false;
        _previousPinchDistance = 0f;
    }

    private void ZoomAtScreenPosition(
        float targetZoom,
        Vector2 screenPosition)
    {
        float clampedZoom = ClampZoom(targetZoom);

        if (Mathf.Approximately(
            clampedZoom,
            _camera.orthographicSize))
        {
            return;
        }

        // 줌 전 커서 또는 핀치 중심의 월드 위치
        Vector3 worldPositionBeforeZoom =
            ScreenToWorldPosition(screenPosition);

        _camera.orthographicSize = clampedZoom;

        // 줌 후 같은 화면 좌표의 월드 위치
        Vector3 worldPositionAfterZoom =
            ScreenToWorldPosition(screenPosition);

        // 두 위치의 차이만큼 카메라를 이동하여
        // 줌 중심이 화면에서 움직이지 않도록 한다.
        Vector3 positionDelta =
            worldPositionBeforeZoom -
            worldPositionAfterZoom;

        positionDelta.z = 0f;
        transform.position += positionDelta;

        _velocity = Vector3.zero;

        ClampCameraPosition();
    }

    private void SetZoom(float zoom)
    {
        _camera.orthographicSize = ClampZoom(zoom);
        ClampCameraPosition();
    }

    private float ClampZoom(float zoom)
    {
        float maximumZoom = GetMaximumZoom();

        return Mathf.Clamp(
            zoom,
            Mathf.Min(_minZoom, maximumZoom),
            maximumZoom);
    }

    private float GetMaximumZoom()
    {
        float maximumZoom = Mathf.Max(_minZoom, _maxZoom);

        if (!_limitZoomToWorldBounds ||
            _camera == null ||
            _camera.aspect <= 0f)
        {
            return maximumZoom;
        }

        float worldWidth =
            Mathf.Max(0f, _worldMax.x - _worldMin.x);

        float worldHeight =
            Mathf.Max(0f, _worldMax.y - _worldMin.y);

        if (worldWidth <= 0f || worldHeight <= 0f)
            return maximumZoom;

        // orthographicSize는 화면 높이의 절반이다.
        float maximumZoomFromHeight =
            worldHeight * 0.5f;

        float maximumZoomFromWidth =
            worldWidth / (_camera.aspect * 2f);

        float worldBoundsMaximum =
            Mathf.Min(
                maximumZoomFromHeight,
                maximumZoomFromWidth);

        return Mathf.Min(maximumZoom, worldBoundsMaximum);
    }

    private void ApplyInertia()
    {
        if (_velocity == Vector3.zero)
            return;

        transform.position +=
            _velocity * Time.deltaTime;

        // 지수 감쇠이므로 프레임레이트 변화에 비교적 안정적
        float damping =
            Mathf.Exp(-_cameraDamping * Time.deltaTime);

        _velocity *= damping;

        if (_velocity.sqrMagnitude <=
            _stopSpeed * _stopSpeed)
        {
            _velocity = Vector3.zero;
        }
    }

    private void ClampCameraPosition()
    {
        if (_camera == null)
            return;

        float halfHeight =
            _camera.orthographicSize;

        float halfWidth =
            halfHeight * _camera.aspect;

        float minX =
            _worldMin.x + halfWidth;

        float maxX =
            _worldMax.x - halfWidth;

        float minY =
            _worldMin.y + halfHeight;

        float maxY =
            _worldMax.y - halfHeight;

        Vector3 position = transform.position;
        Vector3 clampedPosition = position;

        // 월드가 카메라 화면보다 작을 때
        // Mathf.Clamp의 min > max 문제 방지
        clampedPosition.x = minX <= maxX
            ? Mathf.Clamp(position.x, minX, maxX)
            : (_worldMin.x + _worldMax.x) * 0.5f;

        clampedPosition.y = minY <= maxY
            ? Mathf.Clamp(position.y, minY, maxY)
            : (_worldMin.y + _worldMax.y) * 0.5f;

        // 경계 바깥쪽을 향하는 관성만 제거
        if (!Mathf.Approximately(
            clampedPosition.x,
            position.x))
        {
            bool movingOutward =
                (position.x < minX && _velocity.x < 0f) ||
                (position.x > maxX && _velocity.x > 0f);

            if (movingOutward)
                _velocity.x = 0f;
        }

        if (!Mathf.Approximately(
            clampedPosition.y,
            position.y))
        {
            bool movingOutward =
                (position.y < minY && _velocity.y < 0f) ||
                (position.y > maxY && _velocity.y > 0f);

            if (movingOutward)
                _velocity.y = 0f;
        }

        transform.position = clampedPosition;
    }

    private Vector3 ScreenToWorldPosition(
        Vector2 screenPosition)
    {
        float distanceToPlane =
            Mathf.Abs(
                _dragPlaneZ -
                _camera.transform.position.z);

        return _camera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceToPlane));
    }

    private void CancelInput()
    {
        _isDragging = false;
        _isPinching = false;

        _activeTouchId = -1;
        _previousPinchDistance = 0f;

        _velocity = Vector3.zero;
    }

    private void ValidateZoomRange()
    {
        _minZoom = Mathf.Max(0.01f, _minZoom);
        _maxZoom = Mathf.Max(_minZoom, _maxZoom);
    }
}