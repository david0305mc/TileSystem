using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityHFSM;

public class WaiterObj : NpcObj
{
    public enum WaiterState
    {
        Idle,
        MovingToTable,
        Serving,
        ReturningToDisplayStand,
    }

    private static readonly string IdleState = nameof(WaiterState.Idle);
    private static readonly string MovingToTableState = nameof(WaiterState.MovingToTable);
    private static readonly string ServingState = nameof(WaiterState.Serving);
    private static readonly string ReturningToDisplayStandState = nameof(WaiterState.ReturningToDisplayStand);

    [Header("Behaviour")]
    [SerializeField, Min(0.3f)]
    private float _idleWaitDuration = 1f;

    private StateMachine _fsm;
    private long _targetTableUid;
    private WaiterTask _targetWaiterTask;
    private System.Func<WaiterTask> _getWaiterTask;

    public void Initialize(long uid, Vector2Int gridPosition, System.Func<WaiterTask> getWaiterTask)
    {
        _getWaiterTask = getWaiterTask;
        Deinitialize();
        InitializeNpc(uid, gridPosition);

        _fsm = new StateMachine();
        _fsm.AddState(
            IdleState,
            new UniTaskState(onEnterAsync: IdleStateAsync));
        _fsm.AddState(
            MovingToTableState,
            new UniTaskState(onEnterAsync: MovingToTableStateAsync));
        _fsm.AddState(
            ServingState,
            new UniTaskState(onEnterAsync: ServingStateAsync));
        _fsm.AddState(
            ReturningToDisplayStandState,
            new UniTaskState(onEnterAsync: ReturningToDisplayStandStateAsync));

        _fsm.SetStartState(IdleState);
        _fsm.Init();
    }

    public void Deinitialize()
    {
        StopMovement();
        _fsm?.OnExit();
        _fsm = null;
        _targetTableUid = 0;
        _targetWaiterTask = default;
    }

    private async UniTask IdleStateAsync(CancellationToken cancellationToken)
    {
        SetAnimation("idle");

        float maxWaitDuration = Mathf.Max(0.3f, _idleWaitDuration);
        await UniTask.WaitForSeconds(
            Random.Range(0.3f, maxWaitDuration),
            cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        UserData userData = UserDataManager.Instance.User;
        if (!userData.TryGetDisplayStandData(out var displayStandData))
        {
            Debug.LogWarning("Waiter cannot find a display stand.");
            return;
        }

        if (!userData.GetApproachGridPositions(displayStandData).Contains(CurrentGridPosition))
        {
            RequestReturningToDisplayStandState();
            return;
        }

        while (true)
        {
            _targetWaiterTask = _getWaiterTask?.Invoke();

            if (_targetWaiterTask != null)
            {
                switch (_targetWaiterTask.WaiterTaskType)
                {
                    case WaiterTaskType.ServeFood:
                        RequestMovingToTable(_targetWaiterTask.TableUid);
                        break;
                    case WaiterTaskType.CleanFood:
                        break;
                }
            }
            await UniTask.WaitForSeconds(0.1f, cancellationToken: cancellationToken);

        }
    }

    private async UniTask MovingToTableStateAsync(CancellationToken cancellationToken)
    {
        UserData userData = UserDataManager.Instance.User;
        if (!userData.TryGetPlaceableObjData(_targetTableUid, out var tableData)
            || tableData is not TableObjData)
        {
            Debug.LogWarning($"Waiter target table {_targetTableUid} is missing.");
            _targetTableUid = 0;
            RequestReturningToDisplayStandState();
            return;
        }

        if (!TrySetApproachDestination(tableData))
        {
            Debug.LogWarning($"Waiter cannot reach table {_targetTableUid}.");
            _targetTableUid = 0;
            RequestReturningToDisplayStandState();
            return;
        }

        if (HasPath)
        {
            SetAnimation("walk");
            await MoveToDestinationAsync(cancellationToken);
        }

        _targetTableUid = 0;
        RequestServingState();
    }

    private async UniTask ServingStateAsync(CancellationToken cancellationToken)
    {
        SetAnimation("idle");
        // return UniTask.CompletedTask;
        await UniTask.Yield();
        RequestReturningToDisplayStandState();
    }

    private async UniTask ReturningToDisplayStandStateAsync(CancellationToken cancellationToken)
    {
        UserData userData = UserDataManager.Instance.User;
        if (!userData.TryGetDisplayStandData(out var displayStandData))
        {
            Debug.LogWarning("Waiter cannot return because the display stand is missing.");
            RequestIdleState();
            return;
        }

        if (!TrySetApproachDestination(displayStandData))
        {
            Debug.LogWarning("Waiter cannot reach the display stand.");
            RequestIdleState();
            return;
        }

        if (HasPath)
        {
            SetAnimation("walk");
            await MoveToDestinationAsync(cancellationToken);
        }

        RequestIdleState();
    }

    private bool TrySetApproachDestination(PlaceableObjData placeableData)
    {
        foreach (var approachGridPosition in
                 UserDataManager.Instance.User.GetApproachGridPositions(placeableData))
        {
            if (approachGridPosition == CurrentGridPosition
                || TrySetDestination(approachGridPosition))
            {
                return true;
            }
        }

        return false;
    }

    private void RequestIdleState()
    {
        _fsm?.RequestStateChange(IdleState);
    }

    public bool RequestMovingToTable(long tableUid)
    {
        if (_fsm == null
            || !UserDataManager.Instance.User.TryGetPlaceableObjData(tableUid, out var tableData)
            || tableData is not TableObjData)
        {
            return false;
        }

        _targetTableUid = tableUid;
        _fsm.RequestStateChange(MovingToTableState);
        return true;
    }

    public void CompleteServing()
    {
        if (_fsm?.ActiveStateName == ServingState)
        {
            RequestReturningToDisplayStandState();
        }
    }

    private void RequestServingState()
    {
        _fsm?.RequestStateChange(ServingState);
    }

    private void RequestReturningToDisplayStandState()
    {
        _fsm?.RequestStateChange(ReturningToDisplayStandState);
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
