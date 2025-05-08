using UnityEngine;
using System.Collections.Generic;

public class EnvironmentalObject : DamageableStructure
{
    [SerializeField] private EnvironmentalObjectData objectData;
    
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();
    private List<IEnvironmentalEffect> activeEffects = new List<IEnvironmentalEffect>();
    private List<ILineOfSightBlocker> lineOfSightBlockers = new List<ILineOfSightBlocker>();
    
    public EnvironmentalObjectData ObjectData => objectData;
    
    public event System.Action OnVisualStateChanged;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (objectData != null)
        {
            maxHealth = objectData.MaxHealth;
            currentHealth = maxHealth;
        }
    }
    
    public void Initialize(EnvironmentalObjectData data)
    {
        objectData = data;
        
        if (data.ModelPrefab != null)
        {
            GameObject model = Instantiate(data.ModelPrefab, transform);
        }
        
        if (data.IsDestructible)
        {
            maxHealth = data.MaxHealth;
            currentHealth = maxHealth;
        }
        else
        {
            maxHealth = float.MaxValue;
            currentHealth = float.MaxValue;
        }
        
        CreateEffectComponents();
    }
    
    private void CreateEffectComponents()
    {
        activeEffects.Clear();
        lineOfSightBlockers.Clear();
        
        if (objectData == null || objectData.Effects == null)
            return;
            
        foreach (var effectData in objectData.Effects)
        {
            var effect = effectData.CreateEffect(this);
            activeEffects.Add(effect);
            
            if (effect is ILineOfSightBlocker losBlocker)
            {
                lineOfSightBlockers.Add(losBlocker);
            }
        }
    }
    
    private void Start()
    {
        if (objectData != null)
        {
            OccupyCells(true);
            ApplyAllEffects();
        }
    }
    
    private void ApplyAllEffects()
    {
        if (gridManager == null) return;
        
        foreach (var effect in activeEffects)
        {
            foreach (var cellPos in occupiedCells)
            {
                effect.ApplyEffect(cellPos, gridManager);
            }
        }
        
        OnVisualStateChanged?.Invoke();
    }
    
    private void RemoveAllEffects()
    {
        if (gridManager == null) return;
        
        foreach (var effect in activeEffects)
        {
            foreach (var cellPos in occupiedCells)
            {
                effect.RemoveEffect(cellPos, gridManager);
            }
        }
    }
    
    private void OccupyCells(bool occupy)
    {
        if (gridManager == null || objectData == null) return;
        
        occupiedCells.Clear();
        
        for (int x = 0; x < objectData.Size.x; x++)
        {
            for (int y = 0; y < objectData.Size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(coordinate.x + x, coordinate.y + y);
                
                gridManager.SetCellOccupied(cellPos, occupy);
                
                if (occupy)
                {
                    occupiedCells.Add(cellPos);
                }
            }
        }
    }
    
    public override float TakeDamage(float amount)
    {
        if (!objectData.IsDestructible)
            return 0;
            
        return base.TakeDamage(amount);
    }
    
    protected override void DestroyStructure()
    {
        RemoveAllEffects();
        OccupyCells(false);
        base.DestroyStructure();
    }
    
    public bool CheckLineOfSight(Vector3 fromPosition, Vector3 toPosition, bool hasThermalVision)
    {
        if (lineOfSightBlockers.Count == 0)
            return true;
            
        foreach (var blocker in lineOfSightBlockers)
        {
            if (!blocker.CheckLineOfSight(fromPosition, toPosition, hasThermalVision))
                return false;
        }
        
        return true;
    }
}