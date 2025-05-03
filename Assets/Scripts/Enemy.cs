using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an enemy that can follow a path and attack damageable structures
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
    
    // Events from IDamageable
    public event System.Action<IDamageable> OnDestroyed;
    public event System.Action<IDamageable, float> OnDamaged;
    
    // Properties from IDamageable
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
        
        // Find the grid manager
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
        
        // Sort path cells to create a logical path
        // This is a simple implementation and might need to be adjusted based on your specific path design
        SortPathCells();
        
        if (pathCells.Count > 0)
        {
            // Set the first target position
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
    /// Sort path cells to create a logical path
    /// This implementation assumes a linear path from one end to the other
    /// </summary>
    private void SortPathCells()
    {
        // This is a very basic implementation
        // In a real scenario, you might want to use a more sophisticated algorithm, like A*
        // or have a predefined path through waypoints
        
        if (pathCells.Count <= 1) return;
        
        // Find the nearest path cell to start with
        Vector2Int currentPos;
        if (gridManager.TryGetCellAtPosition(transform.position, out currentPos))
        {
            Vector2Int startCell = FindNearestPathCell(currentPos);
            List<Vector2Int> sortedPath = new List<Vector2Int> { startCell };
            HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int> { startCell };
            
            // Simple greedy algorithm to find the next nearest cell
            while (sortedPath.Count < pathCells.Count)
            {
                Vector2Int currentCell = sortedPath[sortedPath.Count - 1];
                Vector2Int nextCell = FindNearestUnvisitedNeighbor(currentCell, visitedCells);
                
                if (nextCell == new Vector2Int(-1, -1)) // No more neighbors
                    break;
                    
                sortedPath.Add(nextCell);
                visitedCells.Add(nextCell);
            }
            
            pathCells = sortedPath;
        }
    }
    
    /// <summary>
    /// Find the nearest path cell to the given position
    /// </summary>
    private Vector2Int FindNearestPathCell(Vector2Int position)
    {
        float minDist = float.MaxValue;
        Vector2Int nearest = new Vector2Int(-1, -1);
        
        foreach (Vector2Int cell in pathCells)
        {
            float dist = Vector2Int.Distance(position, cell);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = cell;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Find the nearest unvisited neighbor path cell
    /// </summary>
    private Vector2Int FindNearestUnvisitedNeighbor(Vector2Int cell, HashSet<Vector2Int> visitedCells)
    {
        // Check the four adjacent cells (up, right, down, left)
        Vector2Int[] neighbors = new Vector2Int[]
        {
            new Vector2Int(cell.x, cell.y + 1),
            new Vector2Int(cell.x + 1, cell.y),
            new Vector2Int(cell.x, cell.y - 1),
            new Vector2Int(cell.x - 1, cell.y)
        };
        
        float minDist = float.MaxValue;
        Vector2Int nearest = new Vector2Int(-1, -1);
        
        foreach (Vector2Int neighbor in neighbors)
        {
            if (pathCells.Contains(neighbor) && !visitedCells.Contains(neighbor))
            {
                float dist = Vector2Int.Distance(cell, neighbor);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = neighbor;
                }
            }
        }
        
        // If no adjacent cells, find the nearest unvisited path cell
        if (nearest.x == -1)
        {
            foreach (Vector2Int pathCell in pathCells)
            {
                if (!visitedCells.Contains(pathCell))
                {
                    float dist = Vector2Int.Distance(cell, pathCell);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = pathCell;
                    }
                }
            }
        }
        
        return nearest;
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
                
                // Handle reaching the end of the path
                // (e.g., deal damage to player base, destroy self, etc.)
                OnReachedDestination();
            }
            else
            {
                UpdateTargetPosition();
            }
        }
        else
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
        // Example: Notify game manager
        // GameManager gameManager = FindObjectOfType<GameManager>();
        // if (gameManager != null)
        // {
        //     gameManager.OnEnemyReachedEnd(this);
        // }
        
        // Optionally destroy self after a delay
        Destroy(gameObject, 2f);
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
        
        // Notify game manager for score/resources
        // GameManager gameManager = FindFirstObjectByType<GameManager>();
        // if (gameManager != null)
        // {
        //     gameManager.OnEnemyKilled(this);
        // }
        
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
                Vector3 cellPosition = new Vector3(pathCells[i].x * cellSize, heightOffset, pathCells[i].y * cellSize);
                Gizmos.DrawSphere(cellPosition, 0.2f);
                
                // Draw line between path points
                if (i < pathCells.Count - 1)
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
                Vector3 targetPosition = new Vector3(targetCell.x * cellSize, heightOffset, targetCell.y * cellSize);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPosition, 0.3f);
            }
        }
    }
}