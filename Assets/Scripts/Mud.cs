using UnityEngine;

public class Mud : CellModifier
{
    [Header( "Mud Settings" )]
    [SerializeField] private float slowDownFactor = 0.5f;
    public float SlowDownFactor => slowDownFactor;
}
