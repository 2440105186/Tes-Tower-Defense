using UnityEngine;

public class TallGrass : CellModifier
{
    [Header( "Tall Grass Settings")]
    [SerializeField] private VisionModes hideFrom = VisionModes.Visual;
    public VisionModes HideFrom => hideFrom;
    
    public override void ApplyModifier(Enemy enemy)
    {
        enemy.SetDetectedBy(hideFrom);
    }

    public override void RemoveModifier(Enemy enemy)
    {
        enemy.ResetDetectedBy();
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