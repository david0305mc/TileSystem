using System;
using R3;
using UnityEngine;


public struct HitContext
{
    public GameObject Attacker;
    public GameObject Target;
    public Vector3 HitPoint;
}


public class WaiterTask
{
    public long TaskUid { get; set; }
    public WaiterTaskType Type { get; set; }
    public long TableUid { get; set; }
    public long WaiterUid { get; set; }
    public long CustomerUid { get; set; }
    public bool IsAssigned { get; set; }
}