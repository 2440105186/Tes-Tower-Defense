using UnityEngine;
using System;

/// <summary>
/// Base class for any structure that can be damaged in the game
/// </summary>
public abstract class DamageableStructure : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public Vector2Int coordinate {get; protected set;}
    
    protected GridManager gridManager;
    
    public event Action<IDamageable> OnDestroyed;
    public event Action<IDamageable, float> OnDamaged;
    
    protected virtual void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        coordinate = gridManager.TryGetCellAtPosition(transform.position, out var cell) ? cell : Vector2Int.one * -1;
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// Apply damage to the structure
    /// </summary>
    /// <param name="amount">Amount of damage to apply</param>
    /// <returns>Actual damage applied</returns>
    public virtual float TakeDamage(float amount)
    {
        if (amount <= 0)
            return 0;
            
        float actualDamage = Mathf.Min(currentHealth, amount);
        currentHealth -= actualDamage;
        
        // Trigger damage event
        OnDamaged?.Invoke(this, actualDamage);
        
        // Check if structure is destroyed
        if (currentHealth <= 0)
        {
            DestroyStructure();
        }
        
        return actualDamage;
    }
    
    /// <summary>
    /// Handle structure destruction
    /// </summary>
    protected virtual void DestroyStructure()
    {
        // Trigger destroyed event
        OnDestroyed?.Invoke(this);
        
        // Implement specific destroy behavior in child classes
        Destroy(gameObject);
    }
}