using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityHFSM;


public class CustomerObj : NpcObj
{
    public delegate bool TryMoveToEmptyChair(out PlaceableObj targetChair, out Vector2Int targetGridPos);

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
    private static readonly string ExittingState = nameof(CustomerState.Exiting);
    [Header("Behaviour")]
    [SerializeField, Min(0f)]
    private float _waitDuration = 2f;

    private readonly List<Vector3> _worldPath = new();

    private StateMachine _fsm;

    private int _pathIndex;
    private Vector2Int _targetGridPosition;
    private PlaceableObj _targetChair;

    public Vector2Int CurrentGridPosition { get; private set; }
    private System.Action _showHud;
    private System.Action _hideHud;
    private TryMoveToEmptyChair _tryMoveToEmptyChair;
    private System.Action _sitAction;
    private System.Action _exittingAction;
    public bool IsMoving =>
        _fsm != null &&
        _fsm.ActiveStateName == WanderingState &&
        _fsm.ActiveStateName == MovingToChairState;


    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float _moveSpeed = 2f;

    [SerializeField, Min(0.001f)]
    private float _arrivalDistance = 0.02f;

    private long _uid;

    public void Initialize(long uid, Vector2Int gridPosition, System.Action showHud, System.Action hideHud, TryMoveToEmptyChair tryMoveToEmptyChair, System.Action sitAction, System.Action exitingAction)
    {
        _uid = uid;
        _tryMoveToEmptyChair = tryMoveToEmptyChair;
        _showHud = showHud;
        _hideHud = hideHud;
        _sitAction = sitAction;
        _exittingAction = exitingAction;

        Stop();

        CurrentGridPosition = gridPosition;
        _targetGridPosition = gridPosition;

        if (GridManager.HasInstance)
        {
            transform.position = GridManager.Instance.GridToWorldPosition(gridPosition);
        }

        InitializeFsm();
        RandomizeAppearance();
    }

    private void InitializeFsm()
    {
        _fsm?.OnExit();

        _fsm = new StateMachine();

        _fsm.AddState(IdleState, new UniTaskState(onEnterAsync: IdleStateAsync));
        _fsm.AddState(WanderingState, new UniTaskState(onEnterAsync: WanderingStateAsync));
        _fsm.AddState(MovingToChairState, new UniTaskState(onEnterAsync: MovingToChairStateAsync));
        _fsm.AddState(SittingState, new UniTaskState(onEnterAsync: SittingStateAsync, onExit: state =>
        {
            _hideHud?.Invoke();
        }));
        _fsm.AddState(ExittingState, new UniTaskState(onEnterAsync: ExittingStateAsync));

        _fsm.SetStartState(IdleState);
        _fsm.Init();
    }

    private async UniTask IdleStateAsync(
        CancellationToken cancellationToken)
    {
        _skeletonAnimation.AnimationName = "idle";

        await UniTask.WaitForSeconds(Random.Range(0.3f, 2f), cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (_tryMoveToEmptyChair(out var targetChair, out var targetGridPos))
        {
            RequestMovingToChairState(targetChair, targetGridPos);
        }
        else
        {
            RequestWanderState();
        }

    }
    private async UniTask MovingToChairStateAsync(CancellationToken cancellationToken)
    {
        _skeletonAnimation.AnimationName = "walk";

        await MovingAsync(cancellationToken);

        RequestSittingState();
    }
    private async UniTask SittingStateAsync(CancellationToken cancellationToken)
    {
        if (_targetChair == null)
        {
            Debug.LogWarning("NPC target chair is missing.");
            RequestExitState();
            return;
        }

        _showHud?.Invoke();
        transform.position = _targetChair.transform.position;
        _skeletonAnimation.AnimationName = "dance";
        await UniTask.WaitForSeconds(3f, cancellationToken: cancellationToken);
        RequestExitState();
    }
    private async UniTask ExittingStateAsync(CancellationToken cancellationToken)
    {
        _exittingAction?.Invoke();
        RequestWanderState();
    }
    private async UniTask WanderingStateAsync(CancellationToken cancellationToken)
    {
        if (_worldPath.Count == 0)
        {
            RequestIdleState();
            return;
        }
        _skeletonAnimation.AnimationName = "walk";

        await MovingAsync(cancellationToken);

        RequestIdleState();
    }
    private async UniTask MovingAsync(CancellationToken cancellationToken)
    {
        float arrivalDistanceSqr = _arrivalDistance * _arrivalDistance;

        while (_pathIndex < _worldPath.Count)
        {
            Vector3 targetPosition = _worldPath[_pathIndex];

            while ((transform.position - targetPosition).sqrMagnitude > arrivalDistanceSqr)
            {
                bool isLeftDir = (transform.position.x - targetPosition.x) > 0;
                SetFlip(isLeftDir);
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    _moveSpeed * Time.deltaTime);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.position = targetPosition;
            _pathIndex++;
        }

        CurrentGridPosition = _targetGridPosition;

        _worldPath.Clear();
        _pathIndex = 0;
    }

    private void RequestWanderState()
    {
        GridManager gridManager = GridManager.Instance;

        Vector2Int targetGridPosition = gridManager.GetRandomGridPos();

        // 현재 위치와 동일한 위치라면 다시 대기
        if (targetGridPosition == CurrentGridPosition)
        {
            RequestIdleState();
            return;
        }
        if (!TrySetTargetPath(targetGridPosition))
        {
            RequestIdleState();
            return;
        }

        _fsm.RequestStateChange(WanderingState);
    }
    private void RequestSittingState()
    {
        _fsm?.RequestStateChange(SittingState);
    }
    private void RequestExitState()
    {
        _fsm?.RequestStateChange(ExittingState);
    }
    private bool TrySetTargetPath(Vector2Int targetGridPosition)
    {
        GridManager gridManager = GridManager.Instance;
        Vector3 targetWorldPosition = gridManager.GridToWorldPosition(targetGridPosition);
        if (!gridManager.TryFindWorldPath(transform.position, targetWorldPosition,
            out var path) || path == null || path.Count == 0)
        {
            return false;
        }

        _worldPath.Clear();
        _worldPath.AddRange(path);

        _targetGridPosition = targetGridPosition;
        _pathIndex = 0;
        return true;
    }
    private void RequestMovingToChairState(PlaceableObj chair, Vector2Int targetGrid)
    {
        if (chair == null)
        {
            Debug.LogWarning("NPC cannot move to a missing chair.");
            RequestIdleState();
            return;
        }

        _targetGridPosition = targetGrid;
        _targetChair = chair;
        if (!TrySetTargetPath(targetGrid))
        {
            Debug.Log("No Way");
            return;
        }

        _fsm?.RequestStateChange(MovingToChairState);
    }
    private void RequestIdleState()
    {
        _fsm?.RequestStateChange(IdleState);
    }

    public void Stop()
    {
        _worldPath.Clear();
        _pathIndex = 0;

        if (_fsm != null)
        {
            _fsm.RequestStateChange(IdleState);
        }
    }

    private void Update()
    {
        _fsm?.OnLogic();
    }

    private void OnDestroy()
    {
        _fsm?.OnExit();
        _fsm = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_worldPath.Count == 0 ||
            _pathIndex >= _worldPath.Count)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        Vector3 previousPosition = transform.position;

        for (int i = _pathIndex; i < _worldPath.Count; i++)
        {
            Vector3 targetPosition = _worldPath[i];

            Gizmos.DrawLine(
                previousPosition,
                targetPosition);

            previousPosition = targetPosition;
        }
    }
#endif

}
