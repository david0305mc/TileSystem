using Lean.Pool;
using UnityEngine;

public class OverHeadUIManager : MonoBehaviour
{
    [SerializeField] private NpcHud _npcHud;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _rootTransform;

    private Camera _worldCamera;
    void Start()
    {
        _worldCamera = Camera.main;
    }
    public NpcHud AttachNpcHud(NpcObj npcObj)
    {
        NpcHud npcHud = LeanPool.Spawn(_npcHud, _rootTransform);
        npcHud.Bind(npcObj, _worldCamera);
        return npcHud;
    }

    public void DetachNpcHud(NpcHud npcHud)
    {
        LeanPool.Despawn(npcHud);
    }


    
}
