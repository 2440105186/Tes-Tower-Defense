using UnityEngine;

[CreateAssetMenu(fileName = "NewLOSEffectData", menuName = "Tower Defense/Effects/Line of Sight Effect")]
public class LineOfSightEffectData : BaseEffectData
{
    [Range(0f, 1f)]
    [SerializeField] private float blockStrength = 1.0f;
    [SerializeField] private bool blocksThermalVision = false;
    
    public float BlockStrength => blockStrength;
    public bool BlocksThermalVision => blocksThermalVision;
    
    public override IEnvironmentalEffect CreateEffect(EnvironmentalObject owner)
    {
        return new LineOfSightEffect(this, owner);
    }
}