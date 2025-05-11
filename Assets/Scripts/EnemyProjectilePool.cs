using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectilePool : MonoBehaviour
{
    public static EnemyProjectilePool Instance { get; private set; }
    
    [SerializeField] GameObject prefab;
    
    private readonly Queue<Projectile> pool = new Queue<Projectile>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public Projectile Get() {
        if (pool.Count > 0) return pool.Dequeue();
        var go = Instantiate(prefab);
        go.TryGetComponent<Projectile>(out var p);
        p.OnDestroy += Return;
        return p;
    }
    void Return(Projectile p) { pool.Enqueue(p); }

    public void SpawnProjectile(float damage, Vector3 position, Quaternion rotation)
    {
        var p = Get();
        p.transform.position = position;
        p.transform.rotation = rotation;
        p.Initialize(damage);
    }
}
