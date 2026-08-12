using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityHFSM;

public class NpcObj : MonoBehaviour
{
    public enum NpcState
    {
        WAITING,
        MOVING,
    }

    [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
    [SerializeField, Min(0.001f)] private float _arrivalDistance = 0.02f;

    private List<Vector3> _worldPath;
    private int _pathIndex;
    private StateMachine _fsm;

    public Vector2Int CurrentGridPosition { get; private set; }
    public bool IsMoving => _worldPath != null && _pathIndex < _worldPath.Count;

    public void Initialize(Vector2Int gridPosition)
    {
        Stop();
        CurrentGridPosition = gridPosition;

        if (GridManager.HasInstance)
        {
            transform.position = GridManager.Instance.GridToWorldPosition(gridPosition);
        }
        InitializeFsm();
    }

    private void InitializeFsm()
    {
        _fsm?.OnExit();
        _fsm = new StateMachine();

        string waitingState = nameof(NpcState.WAITING);
        string movingState = nameof(NpcState.MOVING);

        _fsm.AddState(waitingState, new UniTaskState(onEnterAsync: WaitStateAsync));
        _fsm.AddState(movingState, new UniTaskState(onEnterAsync: MovingStateAsync));
        _fsm.SetStartState(waitingState);
        _fsm.Init();
    }

    private async UniTask WaitStateAsync(CancellationToken cancellationToken)
    {
        await UniTask.WaitForSeconds(2f, cancellationToken: cancellationToken);
        MoveRandomTarget();
    }

    private async UniTask MovingStateAsync(CancellationToken cancellationToken)
    {
        _pathIndex = 0;
        while (_pathIndex < _worldPath.Count)
        {
            var targetPos = _worldPath[_pathIndex];
            while (Vector2.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancellationToken);
            }
            transform.position = targetPos;
            _pathIndex++;
        }
        _fsm.RequestStateChange(nameof(NpcState.WAITING));
    }

    private void MoveRandomTarget()
    {
        var gridManager = GridManager.Instance;
        var randomGridPos = gridManager.GetRandomGridPos();
        var randomWorldPos = gridManager.GridToWorldPosition(randomGridPos);
        if (gridManager.TryFindWorldPath(transform.position, randomWorldPos, out var worldPath))
        {
            _worldPath = worldPath;
            _fsm.RequestStateChange(nameof(NpcState.MOVING));
        }
        else
        {
            _fsm.RequestStateChange(nameof(NpcState.WAITING));
        }
    }

    public void Stop()
    {
        _worldPath = null;
        _pathIndex = 0;
    }
    void OnDestroy()
    {
        _fsm?.OnExit();
        _fsm = null;
    }

    private void Update()
    {
        _fsm?.OnLogic();
    }

    private void OnDrawGizmosSelected()
    {
        if (_worldPath == null || _pathIndex >= _worldPath.Count)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        var previousPosition = transform.position;

        for (int i = _pathIndex; i < _worldPath.Count; i++)
        {
            Gizmos.DrawLine(previousPosition, _worldPath[i]);
            previousPosition = _worldPath[i];
        }
    }
}
