using System;

/// <summary>
/// Interface for any entity that can take damage in the game
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Current health of the entity
    /// </summary>
    float CurrentHealth { get; }
    
    /// <summary>
    /// Maximum health of the entity
    /// </summary>
    float MaxHealth { get; }
    
    /// <summary>
    /// Apply damage to the entity
    /// </summary>
    /// <param name="amount">Amount of damage to apply</param>
    /// <returns>Actual damage applied</returns>
    float TakeDamage(float amount);
    
    /// <summary>
    /// Event triggered when the entity is destroyed
    /// </summary>
    event Action<IDamageable> OnDestroyed;
    
    /// <summary>
    /// Event triggered when the entity takes damage
    /// </summary>
    event Action<IDamageable, float> OnDamaged;
}