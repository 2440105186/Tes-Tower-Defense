using UnityEngine;

public class Mud : CellModifier
{
    [Header( "Mud Settings" )]
    [SerializeField] private float slowDownFactor = 0.5f;
    public float SlowDownFactor => slowDownFactor;

    public override void ApplyModifier(Enemy enemy)
    {
        enemy.SetMoveSpeed(slowDownFactor);
    }

    public override void RemoveModifier(Enemy enemy)
    {
        enemy.ResetMoveSpeed();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Enemy>(out var enemy))
        {
            ApplyModifier(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<Enemy>(out var enemy))
        {
            RemoveModifier(enemy);
        }
    }
}