using UnityEngine;

public class WaiterObj : NpcObj
{
    public void Initialize(long uid, Vector2Int gridPosition)
    {
        InitializeNpc(uid, gridPosition);
        SetAnimation("idle");
    }
}
