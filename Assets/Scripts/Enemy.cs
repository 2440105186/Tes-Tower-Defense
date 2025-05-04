using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an enemy that follows a path and attacks structures
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private LayerMask targetLayers;
    
    [Header("Movement")]
    [SerializeField] private float cellArrivalThreshold = 0.1f;
    [SerializeField] private float heightOffset = 0.5f;
    
    [Header("Combat")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    
    // IDamageable implementation
    public event System.Action<IDamageable> OnDestroyed;
    public event System.Action<IDamageable, float> OnDamaged;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    
    // Path following
    private GridManager gridManager;
    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private int currentPathIndex = 0;
    private Vector3 currentTargetPosition;
    private bool hasReachedDestination = false;
    
    // Combat
    private float attackCooldown = 0f;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("No GridManager found in the scene! Enemy will not be able to navigate.");
        }
    }
    
    private void Start()
    {
        InitializePath();
    }
    
    private void Update()
    {
        // Handle attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
        
        // Try to find and attack targets while moving
        ScanForTargets();
        
        // Continue movement if not at destination
        if (!hasReachedDestination)
        {
            MoveAlongPath();
        }
    }
    
    /// <summary>
    /// Initialize the enemy's path using the grid manager's path cells
    /// </summary>
    private void InitializePath()
    {
        if (gridManager == null) return;
        
        // Get the path cells from the grid manager
        pathCells = gridManager.GetPathCells();
        
        if (pathCells.Count > 0)
        {
            // Start at the first path cell
            currentPathIndex = 0;
            UpdateTargetPosition();
        }
        else
        {
            Debug.LogWarning("No path cells found! Enemy has nowhere to go.");
            hasReachedDestination = true;
        }
    }
    
    /// <summary>
    /// Update the target position based on the current path index
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (currentPathIndex >= pathCells.Count)
        {
            hasReachedDestination = true;
            return;
        }
        
        Vector2Int targetCell = pathCells[currentPathIndex];
        
        // Special case: if we hit a cell with coordinates (-1,-1), this is the end point
        if (targetCell.x == -1 && targetCell.y == -1)
        {
            hasReachedDestination = true;
            OnReachedDestination();
            return;
        }
        
        float cellSize = gridManager.CellSize;
        
        // Convert grid coordinates to world position
        currentTargetPosition = new Vector3(
            targetCell.x * cellSize,
            heightOffset,
            targetCell.y * cellSize
        );
    }
    
    /// <summary>
    /// Move along the path to the current target position
    /// </summary>
    private void MoveAlongPath()
    {
        // Calculate distance to target
        Vector3 directionToTarget = currentTargetPosition - transform.position;
        directionToTarget.y = 0; // Ignore vertical difference
        float distanceToTarget = directionToTarget.magnitude;
        
        // Check if reached current target
        if (distanceToTarget <= cellArrivalThreshold)
        {
            // Move to next point in path
            currentPathIndex++;
            
            if (currentPathIndex >= pathCells.Count)
            {
                hasReachedDestination = true;
                OnReachedDestination();
            }
            else
            {
                UpdateTargetPosition();
            }
            return; // Skip movement this frame after changing targets
        }
        
        // Skip movement if almost at destination to prevent spinning
        if (distanceToTarget < 0.05f)
        {
            // Force completion if very close
            transform.position = new Vector3(currentTargetPosition.x, transform.position.y, currentTargetPosition.z);
            currentPathIndex++;
            
            if (currentPathIndex >= pathCells.Count)
            {
                hasReachedDestination = true;
                OnReachedDestination();
            }
            else
            {
                UpdateTargetPosition();
            }
            return;
        }
        
        // Only proceed with movement if direction is valid
        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            // Move towards target
            Vector3 moveDirection = directionToTarget.normalized;
            
            // Rotate towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            
            // Move forward
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Scan for potential targets within attack range
    /// </summary>
    private void ScanForTargets()
    {
        // Skip if on cooldown
        if (attackCooldown > 0)
            return;
            
        // Scan for targets
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        
        // Find closest valid target
        float closestDistance = float.MaxValue;
        IDamageable closestTarget = null;
        Transform closestTransform = null;
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable target = hitCollider.GetComponent<IDamageable>();
            if (target != null && target != this) // Avoid targeting self
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                    closestTransform = hitCollider.transform;
                }
            }
        }
        
        // Update current target
        currentTarget = closestTarget;
        currentTargetTransform = closestTransform;
        
        // Attack if target found
        if (currentTarget != null && attackCooldown <= 0)
        {
            Attack();
        }
    }
    
    /// <summary>
    /// Attack the current target
    /// </summary>
    private void Attack()
    {
        if (currentTargetTransform == null || firePoint == null)
            return;
    
        // Calculate direction to target
        Vector3 directionToTarget = currentTargetTransform.position - firePoint.position;
    
        // Create rotation towards target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
    
        // Fire projectile with the rotation pointing to the target
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, targetRotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
    
        if (projectile != null)
        {
            projectile.Initialize(attackDamage);
        }
    
        // Reset attack cooldown
        attackCooldown = 1f / attackRate;
    }
    
    /// <summary>
    /// Handle reaching the end of the path
    /// </summary>
    private void OnReachedDestination()
    {
        Debug.Log("Enemy reached destination");
        
        // Look for a gate to attack
        Gate gate = FindFirstObjectByType<Gate>();
        if (gate != null)
        {
            // If there's a Gate component with IDamageable, set it as the target
            IDamageable damageableGate = gate.GetComponent<IDamageable>();
            if (damageableGate != null)
            {
                currentTarget = damageableGate;
                currentTargetTransform = gate.transform;
            }
        }
    }
    
    /// <summary>
    /// Take damage from player attacks
    /// </summary>
    public float TakeDamage(float amount)
    {
        if (amount <= 0)
            return 0;
            
        float actualDamage = Mathf.Min(currentHealth, amount);
        currentHealth -= actualDamage;
        
        // Trigger damage event
        OnDamaged?.Invoke(this, actualDamage);
        
        // Check if destroyed
        if (currentHealth <= 0)
        {
            Die();
        }
        
        return actualDamage;
    }
    
    /// <summary>
    /// Handle enemy death
    /// </summary>
    private void Die()
    {
        // Trigger destroyed event
        OnDestroyed?.Invoke(this);
        
        // Disable movement and combat
        enabled = false;
        
        // Disable collider
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Destroy the enemy after animation plays
        Destroy(gameObject, 2f);
    }
    
    /// <summary>
    /// Draw gizmos for visual debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw path if available
        if (pathCells != null && pathCells.Count > 0 && gridManager != null)
        {
            Gizmos.color = Color.green;
            float cellSize = gridManager.CellSize;
            
            for (int i = 0; i < pathCells.Count; i++)
            {
                // Skip end marker
                if (pathCells[i].x == -1 && pathCells[i].y == -1)
                    continue;
                    
                Vector3 cellPosition = new Vector3(pathCells[i].x * cellSize, heightOffset, pathCells[i].y * cellSize);
                Gizmos.DrawSphere(cellPosition, 0.2f);
                
                // Draw line between path points
                if (i < pathCells.Count - 1 && !(pathCells[i+1].x == -1 && pathCells[i+1].y == -1))
                {
                    Vector3 nextCellPosition = new Vector3(
                        pathCells[i + 1].x * cellSize,
                        heightOffset,
                        pathCells[i + 1].y * cellSize
                    );
                    Gizmos.DrawLine(cellPosition, nextCellPosition);
                }
            }
            
            // Highlight current target position
            if (!hasReachedDestination && currentPathIndex < pathCells.Count)
            {
                Vector2Int targetCell = pathCells[currentPathIndex];
                // Skip end marker
                if (!(targetCell.x == -1 && targetCell.y == -1))
                {
                    Vector3 targetPosition = new Vector3(targetCell.x * cellSize, heightOffset, targetCell.y * cellSize);
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(targetPosition, 0.3f);
                }
            }
        }
    }
}