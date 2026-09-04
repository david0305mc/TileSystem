using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class OverHeadUIManager : MonoBehaviour
{
    [SerializeField] private BaseWorldHud _baseHud;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _rootTransform;


    private Dictionary<IWorldHudTarget, BaseWorldHud> _activeHuds = new();
    private Camera _worldCamera;
    void Start()
    {
        _worldCamera = Camera.main;
    }
    public BaseWorldHud ShowHud(IWorldHudTarget hudTarget)
    {
        if (_activeHuds.TryGetValue(hudTarget, out var baseWorldHud))
        {
            HideHud(hudTarget);
        }
        BaseWorldHud worldHud = LeanPool.Spawn(_baseHud, _rootTransform);
        worldHud.Bind(hudTarget, _worldCamera);
        _activeHuds.Add(hudTarget, worldHud);
        return worldHud;
    }

    public void HideHud(IWorldHudTarget hudTarget)
    {
        if (_activeHuds.TryGetValue(hudTarget, out var baseWorldHud))
        {
            LeanPool.Despawn(baseWorldHud);
            _activeHuds.Remove(hudTarget);
        }
    }



}
