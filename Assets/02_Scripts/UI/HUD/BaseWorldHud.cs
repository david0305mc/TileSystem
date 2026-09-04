using UnityEngine;

public class BaseWorldHud : MonoBehaviour
{
    private IWorldHudTarget _target;
    private Camera _camera;
    public void Bind(IWorldHudTarget hudTarget, Camera camera)
    {
        _camera = camera;
        _target = hudTarget;
    }

    void LateUpdate()
    {
        if (_target != null)
        {
            var screenPosition = _camera.WorldToScreenPoint(_target.HudAnchor.position);
            transform.position = screenPosition;
        }
    }
}
