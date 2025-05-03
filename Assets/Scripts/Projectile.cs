using System;
using UnityEngine;

/// <summary>
/// Represents a projectile fired by a tower
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float maxLifetime = 5f;
    
    private float damage;
    private Rigidbody rigidBody;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Destroy projectile after maximum lifetime
        Destroy(gameObject, maxLifetime);
    }
    
    /// <summary>
    /// Initialize projectile with damage and target
    /// </summary>
    /// <param name="damageAmount">Amount of damage this projectile deals</param>
    /// <param name="targetTransform">Target to track (can be null for unguided projectiles)</param>
    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
        rigidBody.linearVelocity = transform.forward * speed;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit something that can take damage
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}