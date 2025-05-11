using UnityEngine;
using System;

public class TowerDetector : MonoBehaviour
{
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.2f);
    
    private SphereCollider sphereCollider;
    
    public event Action<Enemy> OnEnemyEntered;
    public event Action<Enemy> OnEnemyExited;
    public float Range => sphereCollider.radius;
    
    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        sphereCollider.isTrigger = true;
    }
    
    public void SetRange(float newRange)
    {
        if (sphereCollider != null)
        {
            sphereCollider.radius = newRange;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            OnEnemyEntered?.Invoke(enemy);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            OnEnemyExited?.Invoke(enemy);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            if (sphereCollider)
            {
                Gizmos.DrawSphere(transform.position, sphereCollider.radius);
            }
        }
    }
}