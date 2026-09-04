using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class OverHeadUIManager : MonoBehaviour
{
    [SerializeField] private BaseWorldHud _baseHud;
    [SerializeField] private Transform _rootTransform;
    [SerializeField] private Camera _worldCamera;

    private readonly Dictionary<IWorldHudTarget, BaseWorldHud> _activeHuds = new();
    private readonly Dictionary<BaseWorldHud, IWorldHudTarget> _targetsByHud = new();

    private void Awake()
    {
        ResolveWorldCamera();
    }

    public bool ShowHud(IWorldHudTarget hudTarget)
    {
        if (!TryGetHudAnchor(hudTarget, out Transform hudAnchor))
        {
            return false;
        }

        if (_activeHuds.TryGetValue(hudTarget, out BaseWorldHud activeHud))
        {
            if (activeHud != null)
            {
                return true;
            }

            _activeHuds.Remove(hudTarget);
            if (!ReferenceEquals(activeHud, null))
            {
                _targetsByHud.Remove(activeHud);
            }
        }

        Camera worldCamera = ResolveWorldCamera();
        if (_baseHud == null || worldCamera == null)
        {
            return false;
        }

        Transform parent = _rootTransform != null ? _rootTransform : transform;
        BaseWorldHud hud = LeanPool.Spawn(_baseHud, parent);
        if (hud == null)
        {
            return false;
        }

        if (!hud.TryBind(hudAnchor, worldCamera, HandleInvalidBinding))
        {
            LeanPool.Despawn(hud);
            return false;
        }

        _activeHuds.Add(hudTarget, hud);
        _targetsByHud.Add(hud, hudTarget);
        return true;
    }

    public bool HideHud(IWorldHudTarget hudTarget)
    {
        if (ReferenceEquals(hudTarget, null) ||
            !_activeHuds.Remove(hudTarget, out BaseWorldHud hud))
        {
            return false;
        }

        if (!ReferenceEquals(hud, null))
        {
            _targetsByHud.Remove(hud);
        }

        if (hud != null)
        {
            hud.Unbind();
            LeanPool.Despawn(hud);
        }

        return true;
    }

    private Camera ResolveWorldCamera()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        return _worldCamera;
    }

    private void HandleInvalidBinding(BaseWorldHud hud)
    {
        if (ReferenceEquals(hud, null) ||
            !_targetsByHud.TryGetValue(hud, out IWorldHudTarget hudTarget))
        {
            return;
        }

        HideHud(hudTarget);
    }

    private static bool TryGetHudAnchor(
        IWorldHudTarget hudTarget,
        out Transform hudAnchor)
    {
        hudAnchor = null;

        if (ReferenceEquals(hudTarget, null) ||
            hudTarget is Object unityObject && unityObject == null)
        {
            return false;
        }

        hudAnchor = hudTarget.HudAnchor;
        return hudAnchor != null;
    }
}
