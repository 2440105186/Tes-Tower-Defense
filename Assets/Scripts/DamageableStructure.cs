using UnityEngine;
using System;

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
    
    public virtual float TakeDamage(float amount)
    {
        if (amount <= 0)
            return 0;
            
        float actualDamage = Mathf.Min(currentHealth, amount);
        currentHealth -= actualDamage;
        
        OnDamaged?.Invoke(this, actualDamage);
        
        if (currentHealth <= 0)
        {
            DestroyStructure();
        }
        
        return actualDamage;
    }
    
    protected virtual void DestroyStructure()
    {
        OnDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
}