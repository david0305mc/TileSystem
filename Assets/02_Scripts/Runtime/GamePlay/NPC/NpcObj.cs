using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;
using UnityHFSM;

public class NpcObj : MonoBehaviour
{
    public enum NpcState
    {
        WAITING,
        MOVING,
    }

    private static readonly string WaitingState = nameof(NpcState.WAITING);
    private static readonly string MovingState = nameof(NpcState.MOVING);


    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float _moveSpeed = 2f;

    [SerializeField, Min(0.001f)]
    private float _arrivalDistance = 0.02f;

    [Header("Behaviour")]
    [SerializeField, Min(0f)]
    private float _waitDuration = 2f;

    private readonly List<Vector3> _worldPath = new();

    private StateMachine _fsm;

    private int _pathIndex;
    private Vector2Int _targetGridPosition;

    public Vector2Int CurrentGridPosition { get; private set; }

    public bool IsMoving =>
        _fsm != null &&
        _fsm.ActiveStateName == MovingState;

    public void Initialize(Vector2Int gridPosition)
    {
        Stop();

        CurrentGridPosition = gridPosition;
        _targetGridPosition = gridPosition;

        if (GridManager.HasInstance)
        {
            transform.position =
                GridManager.Instance.GridToWorldPosition(gridPosition);
        }

        InitializeFsm();
    }

    private void InitializeFsm()
    {
        _fsm?.OnExit();

        _fsm = new StateMachine();

        _fsm.AddState(
            WaitingState,
            new UniTaskState(onEnterAsync: WaitingStateAsync));

        _fsm.AddState(
            MovingState,
            new UniTaskState(onEnterAsync: MovingStateAsync));

        _fsm.SetStartState(WaitingState);
        _fsm.Init();
    }

    private async UniTask WaitingStateAsync(
        CancellationToken cancellationToken)
    {
        skeletonAnimation.AnimationName = "idle";
        await UniTask.WaitForSeconds(
            _waitDuration,
            cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        TryMoveToRandomTarget();
    }

    private async UniTask MovingStateAsync(
        CancellationToken cancellationToken)
    {
        if (_worldPath.Count == 0)
        {
            RequestWaitingState();
            return;
        }

        _pathIndex = 0;
        skeletonAnimation.AnimationName = "walk";
        float arrivalDistanceSqr =
            _arrivalDistance * _arrivalDistance;

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

        OnMoveCompleted();
    }
    private void SetFlip(bool isLeft)
    {
        skeletonAnimation.Skeleton.ScaleX = isLeft ? -1f : 1f;
    }

    private void TryMoveToRandomTarget()
    {
        if (!GridManager.HasInstance)
        {
            RequestWaitingState();
            return;
        }

        GridManager gridManager = GridManager.Instance;

        Vector2Int targetGridPosition =
            gridManager.GetRandomGridPos();

        // 현재 위치와 동일한 위치라면 다시 대기
        if (targetGridPosition == CurrentGridPosition)
        {
            RequestWaitingState();
            return;
        }

        Vector3 targetWorldPosition =
            gridManager.GridToWorldPosition(targetGridPosition);

        if (!gridManager.TryFindWorldPath(
                transform.position,
                targetWorldPosition,
                out var path) ||
            path == null ||
            path.Count == 0)
        {
            RequestWaitingState();
            return;
        }

        _worldPath.Clear();
        _worldPath.AddRange(path);

        _targetGridPosition = targetGridPosition;
        _pathIndex = 0;

        _fsm.RequestStateChange(MovingState);
    }

    private void OnMoveCompleted()
    {
        CurrentGridPosition = _targetGridPosition;

        _worldPath.Clear();
        _pathIndex = 0;

        RequestWaitingState();
    }

    private void RequestWaitingState()
    {
        _fsm?.RequestStateChange(WaitingState);
    }

    public void Stop()
    {
        _worldPath.Clear();
        _pathIndex = 0;

        if (_fsm != null)
        {
            _fsm.RequestStateChange(WaitingState);
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