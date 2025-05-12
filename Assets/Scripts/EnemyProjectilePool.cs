using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectilePool : MonoBehaviour
{
    public static EnemyProjectilePool Instance { get; private set; }
    
    [SerializeField] GameObject prefab;
    
    private readonly Queue<Projectile> pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private Projectile Get() {
        Projectile p;
        if (pool.Count > 0) {
            p = pool.Dequeue();
        } else {
            var go = Instantiate(prefab);
            go.TryGetComponent(out p);
            p.OnDestroy += Return;
        }
        p.gameObject.SetActive(true);
        return p;
    }

    private void Return(Projectile p) {
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
    }

    public void SpawnProjectile(float damage, Vector3 position, Quaternion rotation)
    {
        var p = Get();
        p.transform.position = position;
        p.transform.rotation = rotation;
        p.Initialize(damage);
    }
}
