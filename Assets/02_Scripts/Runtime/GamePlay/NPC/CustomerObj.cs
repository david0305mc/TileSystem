using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityHFSM;
using Action = System.Action;

public class CustomerObj : NpcObj
{
    public delegate bool TryMoveToEmptyChair(
        out PlaceableObj targetChair,
        out Vector2Int targetGridPosition);

    public enum CustomerState
    {
        Idle,
        Wandering,
        MovingToChair,
        Sitting,
        Eating,
        Exiting
    }

    private static readonly string IdleState = nameof(CustomerState.Idle);
    private static readonly string WanderingState = nameof(CustomerState.Wandering);
    private static readonly string MovingToChairState = nameof(CustomerState.MovingToChair);
    private static readonly string SittingState = nameof(CustomerState.Sitting);
    private static readonly string ExitingState = nameof(CustomerState.Exiting);

    [Header("Behaviour")]
    [SerializeField, Min(0f)]
    private float _waitDuration = 2f;

    private StateMachine _fsm;
    private PlaceableObj _targetChair;

    private Action _showHud;
    private Action _hideHud;
    private TryMoveToEmptyChair _tryMoveToEmptyChair;
    private System.Action<long> _sitAction;
    private Action _exitingAction;

    public void Initialize(
        long uid,
        Vector2Int gridPosition,
        Action showHud,
        Action hideHud,
        TryMoveToEmptyChair tryMoveToEmptyChair,
        System.Action<long> sitAction,
        Action exitingAction)
    {
        _showHud = showHud;
        _hideHud = hideHud;
        _tryMoveToEmptyChair = tryMoveToEmptyChair;
        _sitAction = sitAction;
        _exitingAction = exitingAction;
        _targetChair = null;

        InitializeNpc(uid, gridPosition);
        InitializeFsm();
    }

    public void Stop()
    {
        StopMovement();

        if (_fsm != null)
        {
            _fsm.RequestStateChange(IdleState);
        }
    }

    public void Deinitialize()
    {
        StopMovement();

        _fsm?.OnExit();
        _fsm = null;

        // _hideHud?.Invoke();
        _exitingAction?.Invoke();

        _targetChair = null;
        _showHud = null;
        _hideHud = null;
        _tryMoveToEmptyChair = null;
        _sitAction = null;
        _exitingAction = null;
    }

    private void InitializeFsm()
    {
        _fsm?.OnExit();
        _fsm = new StateMachine();

        _fsm.AddState(
            IdleState,
            new UniTaskState(onEnterAsync: IdleStateAsync));
        _fsm.AddState(
            WanderingState,
            new UniTaskState(onEnterAsync: WanderingStateAsync));
        _fsm.AddState(
            MovingToChairState,
            new UniTaskState(onEnterAsync: MovingToChairStateAsync));
        _fsm.AddState(
            SittingState,
            new UniTaskState(
                onEnterAsync: SittingStateAsync,
                onExit: _ => _hideHud?.Invoke()));
        _fsm.AddState(
            ExitingState,
            new UniTaskState(onEnterAsync: ExitingStateAsync));

        _fsm.SetStartState(IdleState);
        _fsm.Init();
    }

    private async UniTask IdleStateAsync(CancellationToken cancellationToken)
    {
        SetAnimation("idle");

        float maxWaitDuration = Mathf.Max(0.3f, _waitDuration);
        await UniTask.WaitForSeconds(
            Random.Range(0.3f, maxWaitDuration),
            cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (_tryMoveToEmptyChair != null &&
            _tryMoveToEmptyChair(
                out var targetChair,
                out var targetGridPosition))
        {
            RequestMovingToChairState(targetChair, targetGridPosition);
            return;
        }

        RequestWanderState();
    }

    private async UniTask WanderingStateAsync(
        CancellationToken cancellationToken)
    {
        if (!HasPath)
        {
            RequestIdleState();
            return;
        }

        SetAnimation("walk");
        await MoveToDestinationAsync(cancellationToken);
        RequestIdleState();
    }

    private async UniTask MovingToChairStateAsync(
        CancellationToken cancellationToken)
    {
        SetAnimation("walk");
        await MoveToDestinationAsync(cancellationToken);
        RequestSittingState();
    }

    private async UniTask SittingStateAsync(
        CancellationToken cancellationToken)
    {
        if (_targetChair == null)
        {
            Debug.LogWarning("Customer target chair is missing.");
            RequestExitState();
            return;
        }

        // _showHud?.Invoke();
        _sitAction?.Invoke(_targetChair.Uid);

        transform.position = _targetChair.transform.position;
        SetAnimation("dance");

        await UniTask.WaitForSeconds(
            3f,
            cancellationToken: cancellationToken);

        RequestExitState();
    }

    private UniTask ExitingStateAsync(CancellationToken cancellationToken)
    {
        _exitingAction?.Invoke();
        _targetChair = null;
        RequestWanderState();

        return UniTask.CompletedTask;
    }

    private void RequestWanderState()
    {
        if (!GridManager.HasInstance)
        {
            RequestIdleState();
            return;
        }

        GridManager gridManager = GridManager.Instance;
        Vector2Int targetGridPosition = gridManager.GetRandomGridPos();

        // 현재 위치와 동일하면 다시 대기한다.
        if (targetGridPosition == CurrentGridPosition)
        {
            RequestIdleState();
            return;
        }

        if (!TrySetDestination(targetGridPosition))
        {
            RequestIdleState();
            return;
        }

        _fsm?.RequestStateChange(WanderingState);
    }

    private void RequestMovingToChairState(
        PlaceableObj chair,
        Vector2Int targetGridPosition)
    {
        if (chair == null)
        {
            Debug.LogWarning("Customer cannot move to a missing chair.");
            RequestIdleState();
            return;
        }

        _targetChair = chair;

        if (!TrySetDestination(targetGridPosition))
        {
            Debug.LogWarning("Customer cannot reach the reserved chair.");
            _exitingAction?.Invoke();
            _targetChair = null;
            RequestIdleState();
            return;
        }

        _fsm?.RequestStateChange(MovingToChairState);
    }

    private void RequestSittingState()
    {
        _fsm?.RequestStateChange(SittingState);
    }

    private void RequestExitState()
    {
        _fsm?.RequestStateChange(ExitingState);
    }

    private void RequestIdleState()
    {
        _fsm?.RequestStateChange(IdleState);
    }

    private void Update()
    {
        _fsm?.OnLogic();
    }

    private void OnDisable()
    {
        Deinitialize();
    }
}
