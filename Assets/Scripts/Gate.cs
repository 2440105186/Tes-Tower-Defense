using UnityEngine;
using System;

public class Gate : DamageableStructure
{
    public static event Action OnGateDestroyed;
    
    protected override void DestroyStructure()
    {
        OnGateDestroyed?.Invoke();
        Debug.Log("GATE DESTROYED");
        base.DestroyStructure();
    }
}