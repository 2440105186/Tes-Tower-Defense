using UnityEngine;

[CreateAssetMenu(fileName = "NewMovementEffectData", menuName = "Tower Defense/Effects/Movement Effect")]
public class MovementEffectData : BaseEffectData
{
    [SerializeField] private float speedModifier = 0.5f;
    [SerializeField] private bool completelyBlocksMovement = false;
    [SerializeField] private bool affectsFlyingUnits = false;
    
    public float SpeedModifier => speedModifier;
    public bool CompletelyBlocksMovement => completelyBlocksMovement;
    public bool AffectsFlyingUnits => affectsFlyingUnits;
    
    public override IEnvironmentalEffect CreateEffect(EnvironmentalObject owner)
    {
        return new MovementEffect(this, owner);
    }
}