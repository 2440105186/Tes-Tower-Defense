using System;

public interface IDamageable
{
    float CurrentHealth { get; }
    
    float MaxHealth { get; }
    
    float TakeDamage(float amount);
    
    event Action<IDamageable> OnDestroyed;
    
    event Action<IDamageable, float> OnDamaged;
}