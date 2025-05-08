using UnityEngine;
using System;

public class Gate : DamageableStructure
{
    // Static event to notify when any gate is destroyed
    public static event Action OnGateDestroyed;
    
    protected override void DestroyStructure()
    {
        // Notify listeners that the gate is destroyed
        OnGateDestroyed?.Invoke();
        
        // Log game status
        Debug.Log("GATE DESTROYED");
        
        // Call the base implementation
        base.DestroyStructure();
    }
}