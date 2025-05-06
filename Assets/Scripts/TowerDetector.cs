using System;
using UnityEngine;

public class TowerDetector : MonoBehaviour
{
    private SphereCollider detectionCollider;
    private Tower parentTower;
    
    public event Action<Enemy> OnEnemyEntered;
    public event Action<Enemy> OnEnemyExited;
    
    private void Awake()
    {
        detectionCollider = GetComponent<SphereCollider>();
        parentTower = GetComponentInParent<Tower>();
        
        if (parentTower == null)
        {
            Debug.LogError("TowerDetector must be a child of a Tower component!", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Notify parent tower that enemy entered range
            OnEnemyEntered?.Invoke(enemy);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Notify parent tower that enemy left range
            OnEnemyExited?.Invoke(enemy);
        }
    }
}