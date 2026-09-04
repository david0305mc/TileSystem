using Lean.Pool;
using UnityEngine;

public class OverHeadUIManager : MonoBehaviour
{
    [SerializeField] private BaseWorldHud _baseHud;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _rootTransform;

    private Camera _worldCamera;
    void Start()
    {
        _worldCamera = Camera.main;
    }
    public BaseWorldHud AttachNpcHud(NpcObj npcObj)
    {
        BaseWorldHud npcHud = LeanPool.Spawn(_baseHud, _rootTransform);
        npcHud.Bind(npcObj, _worldCamera);
        return npcHud;
    }

    public void DetachNpcHud(BaseWorldHud npcHud)
    {
        LeanPool.Despawn(npcHud);
    }


    
}
