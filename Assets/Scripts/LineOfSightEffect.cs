using UnityEngine;

public class LineOfSightEffect : IEnvironmentalEffect, ILineOfSightBlocker
{
    private LineOfSightEffectData data;
    private EnvironmentalObject owner;
    
    public LineOfSightEffect(LineOfSightEffectData effectData, EnvironmentalObject owner)
    {
        this.data = effectData;
        this.owner = owner;
    }
    
    public void ApplyEffect(Vector2Int cellPosition, GridManager gridManager)
    {
        CellEffectType effectType = data.BlocksThermalVision 
            ? CellEffectType.ThermalVisionBlocker 
            : CellEffectType.LineOfSightBlocker;
            
        gridManager.AddCellEffect(cellPosition, new CellEffect(
            effectType,
            data.BlockStrength,
            owner
        ));
    }
    
    public void RemoveEffect(Vector2Int cellPosition, GridManager gridManager)
    {
        gridManager.RemoveCellEffectsFromSource(cellPosition, owner);
    }
    
    public string GetEffectDescription()
    {
        string result = data.EffectDescription;
        if (data.BlocksThermalVision)
            result += " Blocks thermal vision.";
        return result;
    }
    
    public float GetEffectStrength()
    {
        return data.BlockStrength;
    }
    
    public bool CheckLineOfSight(Vector3 fromPosition, Vector3 toPosition, bool hasThermalVision)
    {
        if (hasThermalVision && !data.BlocksThermalVision)
            return true;
            
        Vector3 direction = toPosition - fromPosition;
        float distance = direction.magnitude;
        
        Ray ray = new Ray(fromPosition, direction.normalized);
        
        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            if (hit.collider.gameObject == owner.gameObject)
            {
                return false;
            }
        }
        
        return true;
    }
}