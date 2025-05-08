using UnityEngine;

public interface IEnvironmentalEffect
{
    void ApplyEffect(Vector2Int cellPosition, GridManager gridManager);
    void RemoveEffect(Vector2Int cellPosition, GridManager gridManager);
    string GetEffectDescription();
    float GetEffectStrength();
}