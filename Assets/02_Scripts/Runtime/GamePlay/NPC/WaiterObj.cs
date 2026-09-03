using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityHFSM;

public class WaiterObj : NpcObj
{
    public enum WaiterState
    {
        Idle,
        MovingToDisplayStand,
        Serving,
    }

    private static readonly string IdleState = nameof(WaiterState.Idle);
    private static readonly string MovingToDisplayState = nameof(WaiterState.MovingToDisplayStand);
    private static readonly string ServingState = nameof(WaiterState.Serving);

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
        
        

        // if (_tryMoveToEmptyChair != null &&
        //     _tryMoveToEmptyChair(
        //         out var targetChair,
        //         out var targetGridPosition))
        // {
        //     RequestMovingToChairState(targetChair, targetGridPosition);
        //     return;
        // }

        // RequestWanderState();
    }

}
