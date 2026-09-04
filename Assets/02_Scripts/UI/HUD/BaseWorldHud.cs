using System;
using Lean.Pool;
using UnityEngine;

public class BaseWorldHud : MonoBehaviour, IPoolable
{
    private Transform _hudAnchor;
    private Camera _worldCamera;
    private Action<BaseWorldHud> _bindingInvalidated;

    public bool TryBind(
        Transform hudAnchor,
        Camera worldCamera,
        Action<BaseWorldHud> bindingInvalidated)
    {
        Unbind();

        if (hudAnchor == null || worldCamera == null)
        {
            return false;
        }

        _hudAnchor = hudAnchor;
        _worldCamera = worldCamera;
        _bindingInvalidated = bindingInvalidated;
        return true;
    }

    public void Unbind()
    {
        _hudAnchor = null;
        _worldCamera = null;
        _bindingInvalidated = null;
    }

    public void OnSpawn()
    {
        Unbind();
    }

    public void OnDespawn()
    {
        Unbind();
    }

    private void LateUpdate()
    {
        if (_hudAnchor == null || _worldCamera == null)
        {
            _bindingInvalidated?.Invoke(this);
            return;
        }

        transform.position = _worldCamera.WorldToScreenPoint(_hudAnchor.position);
    }
}
