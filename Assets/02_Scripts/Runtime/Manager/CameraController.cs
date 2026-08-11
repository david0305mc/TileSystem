using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class CameraController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private Vector2 minPosition = new(-10f, -10f);
    [SerializeField] private Vector2 maxPosition = new(10f, 10f);

    [Header("Inertia")]
    [Tooltip("관성 이동 속도 배율")]
    [SerializeField] private float inertiaMultiplier = 1f;

    [Tooltip("값이 클수록 빠르게 멈춥니다.")]
    [SerializeField] private float inertiaDeceleration = 6f;

    [Tooltip("이 속도보다 느려지면 관성을 종료합니다.")]
    [SerializeField] private float inertiaStopThreshold = 0.05f;

    [Tooltip("관성의 최대 이동 속도")]
    [SerializeField] private float maxInertiaSpeed = 20f;

    [Header("Zoom")]
    [SerializeField] private float minOrthographicSize = 3f;
    [SerializeField] private float maxOrthographicSize = 8f;
    [SerializeField] private float mouseZoomSpeed = 0.5f;
    [SerializeField] private float pinchZoomSpeed = 0.01f;

    private Camera targetCamera;
    public Camera Camera => targetCamera;

    private Vector2 previousPointerPosition;
    private bool isDragging;

    private Vector3 inertiaVelocity;
    private Vector3 previousCameraPosition;

    private void Awake()
    {
        targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError(
                "[CameraController] Main Camera를 찾을 수 없습니다.");
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

// #if UNITY_EDITOR || UNITY_STANDALONE
//         HandleMouseDrag();
//         HandleMouseZoom();
// #endif

//         HandleTouchInput();
        HandleInertia();
    }

    #region Mouse

    public void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 pointerPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
            {
                return;
            }

            BeginDrag(pointerPosition);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }

        if (!isDragging || !mouse.leftButton.isPressed)
        {
            return;
        }

        UpdateDrag(pointerPosition);
    }

    public void HandleMouseZoom()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        float scrollDelta = mouse.scroll.ReadValue().y;

        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        float scrollDirection = Mathf.Sign(scrollDelta);

        Zoom(scrollDirection * mouseZoomSpeed);
    }

    #endregion

    #region Touch

    public void HandleTouchInput()
    {
        var touches = Touch.activeTouches;

        if (touches.Count == 0)
        {
            return;
        }

        if (touches.Count >= 2)
        {
            isDragging = false;
            inertiaVelocity = Vector3.zero;

            HandlePinchZoom(touches[0], touches[1]);
            return;
        }

        HandleSingleTouchDrag(touches[0]);
    }

    private void HandleSingleTouchDrag(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
            {
                if (IsPointerOverUI(touch.touchId))
                {
                    isDragging = false;
                    return;
                }

                BeginDrag(touch.screenPosition);
                break;
            }

            case TouchPhase.Moved:
            {
                if (!isDragging)
                {
                    return;
                }

                UpdateDrag(touch.screenPosition);
                break;
            }

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
            {
                EndDrag();
                break;
            }
        }
    }

    private void HandlePinchZoom(
        Touch firstTouch,
        Touch secondTouch)
    {
        Vector2 firstCurrentPosition =
            firstTouch.screenPosition;

        Vector2 secondCurrentPosition =
            secondTouch.screenPosition;

        Vector2 firstPreviousPosition =
            firstCurrentPosition - firstTouch.delta;

        Vector2 secondPreviousPosition =
            secondCurrentPosition - secondTouch.delta;

        float previousDistance = Vector2.Distance(
            firstPreviousPosition,
            secondPreviousPosition);

        float currentDistance = Vector2.Distance(
            firstCurrentPosition,
            secondCurrentPosition);

        float distanceDelta =
            currentDistance - previousDistance;

        Zoom(-distanceDelta * pinchZoomSpeed);
    }

    #endregion

    #region Drag

    public void BeginDrag(Vector2 pointerPosition)
    {
        previousPointerPosition = pointerPosition;
        previousCameraPosition = transform.position;

        isDragging = true;
        inertiaVelocity = Vector3.zero;
    }

    public void UpdateDrag(Vector2 currentPointerPosition)
    {
        previousCameraPosition = transform.position;

        MoveCamera(previousPointerPosition, currentPointerPosition);

        CalculateDragVelocity();

        previousPointerPosition = currentPointerPosition;
    }

    public void EndDrag()
    {
        isDragging = false;
    }

    private void CalculateDragVelocity()
    {
        if (Time.unscaledDeltaTime <= 0f)
        {
            return;
        }

        Vector3 frameMovement =
            transform.position - previousCameraPosition;

        Vector3 currentVelocity =
            frameMovement / Time.unscaledDeltaTime;

        currentVelocity = Vector3.ClampMagnitude(
            currentVelocity,
            maxInertiaSpeed);

        // 마지막 한 프레임의 튀는 값 대신
        // 최근 속도와 부드럽게 섞어 자연스러운 관성을 만듭니다.
        inertiaVelocity = Vector3.Lerp(
            inertiaVelocity,
            currentVelocity,
            0.5f);
    }

    #endregion

    #region Inertia

    private void HandleInertia()
    {
        if (isDragging)
        {
            return;
        }

        if (inertiaVelocity.sqrMagnitude <=
            inertiaStopThreshold * inertiaStopThreshold)
        {
            inertiaVelocity = Vector3.zero;
            return;
        }

        Vector3 previousPosition = transform.position;

        transform.position +=
            inertiaVelocity *
            inertiaMultiplier *
            Time.unscaledDeltaTime;

        ClampPosition();
        RemoveBlockedVelocity(previousPosition);

        // 프레임레이트와 관계없이 자연스럽게 감속합니다.
        float damping =
            Mathf.Exp(
                -inertiaDeceleration *
                Time.unscaledDeltaTime);

        inertiaVelocity *= damping;
    }

    private void RemoveBlockedVelocity(
        Vector3 previousPosition)
    {
        Vector3 currentPosition = transform.position;

        bool blockedX =
            !Mathf.Approximately(
                currentPosition.x,
                previousPosition.x) == false;

        bool blockedY =
            !Mathf.Approximately(
                currentPosition.y,
                previousPosition.y) == false;

        if ((currentPosition.x <= minPosition.x &&
             inertiaVelocity.x < 0f) ||
            (currentPosition.x >= maxPosition.x &&
             inertiaVelocity.x > 0f))
        {
            inertiaVelocity.x = 0f;
        }

        if ((currentPosition.y <= minPosition.y &&
             inertiaVelocity.y < 0f) ||
            (currentPosition.y >= maxPosition.y &&
             inertiaVelocity.y > 0f))
        {
            inertiaVelocity.y = 0f;
        }
    }

    #endregion

    private void MoveCamera(
        Vector2 previousScreenPosition,
        Vector2 currentScreenPosition)
    {
        Vector3 previousWorldPosition =
            ScreenToWorldPosition(previousScreenPosition);

        Vector3 currentWorldPosition =
            ScreenToWorldPosition(currentScreenPosition);

        Vector3 delta =
            previousWorldPosition - currentWorldPosition;

        delta.z = 0f;

        transform.position += delta * dragSpeed;

        ClampPosition();
    }

    private Vector3 ScreenToWorldPosition(
        Vector2 screenPosition)
    {
        Vector3 position = new(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(
                targetCamera.transform.position.z));

        return targetCamera.ScreenToWorldPoint(position);
    }

    private void Zoom(float amount)
    {
        targetCamera.orthographicSize = Mathf.Clamp(
            targetCamera.orthographicSize + amount,
            minOrthographicSize,
            maxOrthographicSize);

        ClampPosition();
    }

    private void ClampPosition()
    {
        Vector3 position = transform.position;

        position.x = Mathf.Clamp(
            position.x,
            minPosition.x,
            maxPosition.x);

        position.y = Mathf.Clamp(
            position.y,
            minPosition.y,
            maxPosition.y);

        transform.position = position;
    }

    private bool IsPointerOverUI(int pointerId = -1)
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