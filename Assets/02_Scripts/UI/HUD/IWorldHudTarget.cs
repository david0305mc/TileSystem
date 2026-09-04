

using UnityEngine;

public interface IWorldHudTarget
{
    public Transform HudAnchor { get; }
    public BaseWorldHud BaseHud { get; set; }
    
}
