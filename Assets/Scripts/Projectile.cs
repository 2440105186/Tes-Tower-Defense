using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float maxLifetime = 5f;
    
    private float damage;
    private Rigidbody rigidBody;
    
    public event Action<Projectile> OnDestroy;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
        rigidBody.linearVelocity = transform.forward * speed;
    }
    
    private void Start()
    {
        // Destroy(gameObject, maxLifetime);
        StartCoroutine(nameof(HideOnMaxLifetime));
    }

    private IEnumerator HideOnMaxLifetime()
    {
        yield return new WaitForSeconds(maxLifetime);
        gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            // Destroy(gameObject);
            OnDestroy?.Invoke(this);
        }
    }
}