using System.Collections.Generic;
using System.Threading;
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
    private static readonly string MovingToTable = nameof(WaiterState.MovingToTable);
    private static readonly string ServingState = nameof(WaiterState.Serving);
    private static readonly string ReturningToDisplayStandState = nameof(WaiterState.ReturningToDisplayStand);


    [Header("Behaviour")]

    private StateMachine _fsm;


    public void Initialize(long uid, Vector2Int gridPosition)
    {
        InitializeNpc(uid, gridPosition);
        _fsm = new StateMachine();
        _fsm.AddState(nameof(WaiterState.Idle), new UniTaskState(onEnterAsync: async (cancelltaionToken) =>
        {

        }));
        _fsm.Init();
        SetAnimation("idle");
    }

    private async UniTask IdleStateAsync(CancellationToken cancellationToken)
    {
        SetAnimation("idle");

        float maxWaitDuration = 0.1f;
        await UniTask.WaitForSeconds(
            Random.Range(0.3f, maxWaitDuration),
            cancellationToken: cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<Vector2Int> approchGrids = UserDataManager.Instance.User.GetApproachGridPositions(UserDataManager.Instance.User.DisplayStandData);
        if (!approchGrids.Contains(CurrentGridPosition))
        {
            RequestReturningToDisplayStandState();
        }
    }
    private async UniTask MovingToTableStateAsync(CancellationToken cancellationToken)
    {

    }
    private async UniTask ServingStateAsync(CancellationToken cancellationToken)
    {

    }
    private async UniTask ReturningToDisplayStandStateAsync(CancellationToken cancellationToken)
    {

    }
    private void RequestIdleState()
    {
        _fsm?.RequestStateChange(IdleState);
    }

    public void RequestMovingToTable(long talbeUid)
    {
        _fsm?.RequestStateChange(MovingToTable);
    }

    private void RequestServingState()
    {
        _fsm?.RequestStateChange(ServingState);
    }
    private void RequestReturningToDisplayStandState()
    {
        _fsm?.RequestStateChange(ReturningToDisplayStandState);
    }
}
