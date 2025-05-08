using UnityEngine;

public class MovementEffect : IEnvironmentalEffect
{
    private MovementEffectData data;
    private EnvironmentalObject owner;
    
    public MovementEffect(MovementEffectData effectData, EnvironmentalObject owner)
    {
        this.data = effectData;
        this.owner = owner;
    }
    
    public void ApplyEffect(Vector2Int cellPosition, GridManager gridManager)
    {
        if (data.CompletelyBlocksMovement)
        {
            gridManager.AddCellEffect(cellPosition, new CellEffect(
                CellEffectType.MovementBlocker,
                1.0f,
                owner
            ));
        }
        else
        {
            gridManager.AddCellEffect(cellPosition, new CellEffect(
                CellEffectType.MovementModifier,
                data.SpeedModifier,
                owner
            ));
        }
    }
    
    public void RemoveEffect(Vector2Int cellPosition, GridManager gridManager)
    {
        gridManager.RemoveCellEffectsFromSource(cellPosition, owner);
    }
    
    public string GetEffectDescription()
    {
        if (data.CompletelyBlocksMovement)
            return "Blocks movement completely.";
        else
            return $"Changes movement speed by {data.SpeedModifier * 100}%.";
    }
    
    public float GetEffectStrength()
    {
        return data.CompletelyBlocksMovement ? 0f : data.SpeedModifier;
    }
}