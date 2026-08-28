using UnityEngine;

public class OverHeadUIManager : SingletonMono<OverHeadUIManager>
{

    [SerializeField] private NpcHud _npcHud;
    protected override void Awake()
    {
        base.Awake();
    }

    public void AttachNpcHud()
    {
        var npcHud = Lean.Pool.LeanPool.Spawn(_npcHud);
    }

    public void DetachNpcHud()
    {
        
    }


    
}
