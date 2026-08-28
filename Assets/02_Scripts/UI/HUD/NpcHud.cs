using UnityEngine;

public class NpcHud : MonoBehaviour
{
    private NpcObj _target;
    private Camera _camera;
    public void Bind(NpcObj npcObj, Camera camera)
    {
        _camera = camera;
        _target = npcObj;
    }

    void LateUpdate()
    {
        if (_target != null)
        {
            var screenPosition =_camera.WorldToScreenPoint(_target.HudAnchor.position);
            transform.position = screenPosition;
        }
    }
}
