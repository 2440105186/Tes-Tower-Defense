﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private LayerMask towerLayers;
    [SerializeField] private LayerMask environmentLayers;
    [SerializeField] private VisionModes baseDetectedBy = VisionModes.Visual;
    
    [Header("Movement")]
    [SerializeField] private float cellArrivalThreshold = 0.1f;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private LayerMask gridLayerMask;
    
    [Header("Combat")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    
    public event System.Action<IDamageable> OnDestroyed;
    public event System.Action<IDamageable, float> OnDamaged;
    public float CurrentHealth => currentHealth;
    public float CurrentMoveSpeed;
    public float BaseMoveSpeed => baseMoveSpeed;
    public VisionModes CurrentDetectedBy;
    public VisionModes BaseDetectedBy => baseDetectedBy;
    public float MaxHealth => maxHealth;
    public int CurrentPathIndex { get; private set; } = 0;
    
    private GridManager gridManager;
    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private Vector3 currentTargetPosition;
    private bool hasReachedDestination = false;
    
    private bool isAttackingGate = false;
    private bool isGateDestroyed = false;
    private int gateTargetPathIndex = -1;
    
    private float attackCooldown = 0f;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;
    
    private void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        currentHealth = maxHealth;
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("No GridManager found in the scene! Enemy will not be able to navigate.");
        }
    }

    public void SetMoveSpeed(float speedModifier)
    {
        CurrentMoveSpeed *= speedModifier;
    }

    public void ResetMoveSpeed()
    {
        CurrentMoveSpeed = baseMoveSpeed;
    }
    
    public void SetDetectedBy(VisionModes mode)
    {
        CurrentDetectedBy = baseDetectedBy & ~mode;
    }

    public void ResetDetectedBy()
    {
        CurrentDetectedBy = baseDetectedBy;
    }
    
    private void OnEnable()
    {
        Gate.OnGateDestroyed += OnGateDestroyed;
    }
    
    private void OnDisable()
    {
        Gate.OnGateDestroyed -= OnGateDestroyed;
    }
    
    private void Start()
    {
        InitializePath();
        CurrentMoveSpeed  = baseMoveSpeed;
        CurrentDetectedBy = baseDetectedBy;
    }
    
    private void Update()
    {
        // Handle attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
        
        // If attacking gate, don't move and focus on the gate
        if (isAttackingGate && !isGateDestroyed)
        {
            ScanForGate();
            return;
        }
        
        // Continue movement if not at destination
        if (!hasReachedDestination)
        {
            // Try to find and attack targets while moving
            ScanForTargets();
            MoveAlongPath();
        }
    }
    
    private void InitializePath()
    {
        if (gridManager == null) return;
        
        // Get the path cells from the grid manager
        pathCells = gridManager.GetPathCells();
        
        if (pathCells.Count > 0)
        {
            // Find the gate target position (second to last cell)
            FindGatePosition();
            
            // Start at the first path cell
            CurrentPathIndex = 0;
            UpdateTargetPosition();
        }
        else
        {
            Debug.LogWarning("No path cells found! Enemy has nowhere to go.");
            hasReachedDestination = true;
        }
    }
    
    private void FindGatePosition()
    {
        if (pathCells.Count < 2) return;
        
        // Look for the end marker (-1, -1)
        int endMarkerIndex = -1;
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (pathCells[i].x == -1 && pathCells[i].y == -1)
            {
                endMarkerIndex = i;
                break;
            }
        }
        
        // If we found an end marker, set the gate position to the cell before it
        if (endMarkerIndex > 0)
        {
            gateTargetPathIndex = endMarkerIndex - 1;
        }
        else if (pathCells.Count > 1)
        {
            // If no end marker, use the second to last cell
            gateTargetPathIndex = pathCells.Count - 2;
        }
    }
    
    private void UpdateTargetPosition()
    {
        if (CurrentPathIndex >= pathCells.Count)
        {
            hasReachedDestination = true;
            return;
        }

        Vector2Int targetCell = pathCells[CurrentPathIndex];

        // Special case: if we hit a cell with coordinates (-1,-1), this is the end point
        if (targetCell.x == -1 && targetCell.y == -1)
        {
            hasReachedDestination = true;
            OnReachedDestination();
            return;
        }

        // Get the actual cell GameObject position directly
        if (gridManager.TryGetCellObject(targetCell, out GameObject cellObject))
        {
            // Use the exact cell position (with height offset)
            currentTargetPosition = new Vector3(
                cellObject.transform.position.x,
                heightOffset,
                cellObject.transform.position.z
            );
        }
        else
        {
            // Fallback to calculated position if cell object not found
            float cellSize = gridManager.CellSize;
            currentTargetPosition = new Vector3(
                targetCell.x * cellSize,
                heightOffset,
                targetCell.y * cellSize
            );
        }

        // Check if we're at the gate target position
        if (CurrentPathIndex == gateTargetPathIndex && !isGateDestroyed)
        {
            isAttackingGate = true;
        }
    }
    
    private void MoveAlongPath()
    {
        // Skip movement if attacking gate
        if (isAttackingGate && !isGateDestroyed) return;
        
        // Calculate distance to target
        Vector3 directionToTarget = currentTargetPosition - transform.position;
        directionToTarget.y = 0; // Ignore vertical difference
        float distanceToTarget = directionToTarget.magnitude;
        
        // Check if reached current target
        if (distanceToTarget <= cellArrivalThreshold)
        {
            // Move to next point in path
            CurrentPathIndex++;
            
            if (CurrentPathIndex >= pathCells.Count)
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
            CurrentPathIndex++;
            
            if (CurrentPathIndex >= pathCells.Count)
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
            transform.position += transform.forward * CurrentMoveSpeed * Time.deltaTime;
        }
    }
    
    private void ScanForGate()
    {
        // If gate already destroyed, continue path
        if (isGateDestroyed)
        {
            isAttackingGate = false;
            return;
        }
    
        // Skip if on cooldown
        if (attackCooldown > 0)
            return;
    
        // Look for gate specifically
        Gate gate = FindFirstObjectByType<Gate>();
        if (gate != null)
        {
            // If there's a Gate component with IDamageable, set it as the target
            IDamageable damageableGate = gate.GetComponent<IDamageable>();
            if (damageableGate != null)
            {
                currentTarget = damageableGate;
                currentTargetTransform = gate.transform;
            
                // Only attack the gate, don't rotate to face it
                if (currentTargetTransform != null && firePoint != null)
                {
                    // Calculate direction to target
                    Vector3 directionToTarget = currentTargetTransform.position - firePoint.position;
            
                    // Create rotation towards target for the projectile only
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, targetRotation);
            
                    // Reset attack cooldown
                    attackCooldown = 1f / attackRate;
                }
            }
        }
        else
        {
            // If no gate found, it means it was destroyed - continue path
            isGateDestroyed = true;
            isAttackingGate = false;
        }
    }
    
    private void OnGateDestroyed()
    {
        isGateDestroyed = true;
        isAttackingGate = false;
        currentTarget = null;
        currentTargetTransform = null;
        
        Debug.Log("Enemy detected gate destruction, continuing to final destination");
    }
    
    private void ScanForTargets()
    {
        // Skip if on cooldown or if attacking gate
        if (attackCooldown > 0 || isAttackingGate)
            return;
            
        // Scan for targets
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, towerLayers);
        
        // Find closest valid target
        float closestDistance = float.MaxValue;
        IDamageable closestTarget = null;
        Transform closestTransform = null;
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable target = hitCollider.GetComponent<IDamageable>();
            if (target != null && target is not Enemy) // Avoid targeting self
            {
                // Check if target is blocked by environment
                if (IsTargetBlocked(hitCollider.transform))
                {
                    continue; // Skip this target if it's blocked
                }
                
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
    
    private bool IsTargetBlocked(Transform target)
    {
        if (target == null)
            return true;
            
        Vector3 directionToTarget = target.position - firePoint.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        // Perform raycast to check if there's an environmental blocker between enemy and target
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, directionToTarget.normalized, out hit, distanceToTarget, environmentLayers))
        {
            // If we hit something that's not the target, it means there's a blocker in the way
            if (hit.transform != target)
            {
                return true; // Target is blocked
            }
        }
        
        return false; // Target is not blocked
    }
    
    private void Attack()
    {
        if (currentTargetTransform == null || firePoint == null)
            return;

        // Calculate direction to target
        Vector3 directionToTarget = currentTargetTransform.position - firePoint.position;

        // Create rotation towards target for the projectile only
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, targetRotation);

        // Reset attack cooldown
        attackCooldown = 1f / attackRate;
    }
    
    private void OnReachedDestination()
    {
        // Destroy this enemy
        Die();
    }
    
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
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw path if available
        if (pathCells != null && pathCells.Count > 0 && gridManager != null)
        {
            Gizmos.color = Color.green;
            
            for (int i = 0; i < pathCells.Count; i++)
            {
                // Skip end marker
                if (pathCells[i].x == -1 && pathCells[i].y == -1)
                    continue;
                    
                Vector3 cellPosition;
                if (gridManager.TryGetCellObject(pathCells[i], out GameObject cellObject))
                {
                    // Use the exact cell position
                    cellPosition = new Vector3(
                        cellObject.transform.position.x,
                        heightOffset,
                        cellObject.transform.position.z
                    );
                }
                else
                {
                    // Fallback to calculated position
                    float cellSize = gridManager.CellSize;
                    cellPosition = new Vector3(
                        pathCells[i].x * cellSize,
                        heightOffset,
                        pathCells[i].y * cellSize
                    );
                }
                
                // Highlight gate position
                if (i == gateTargetPathIndex)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(cellPosition, 0.3f);
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.DrawSphere(cellPosition, 0.2f);
                }
                
                // Draw line between path points
                if (i < pathCells.Count - 1 && !(pathCells[i+1].x == -1 && pathCells[i+1].y == -1))
                {
                    Vector3 nextCellPosition;
                    if (gridManager.TryGetCellObject(pathCells[i+1], out GameObject nextCellObject))
                    {
                        // Use the exact cell position
                        nextCellPosition = new Vector3(
                            nextCellObject.transform.position.x,
                            heightOffset,
                            nextCellObject.transform.position.z
                        );
                    }
                    else
                    {
                        // Fallback to calculated position
                        float cellSize = gridManager.CellSize;
                        nextCellPosition = new Vector3(
                            pathCells[i+1].x * cellSize,
                            heightOffset,
                            pathCells[i+1].y * cellSize
                        );
                    }
                    
                    Gizmos.DrawLine(cellPosition, nextCellPosition);
                }
            }
            
            // Highlight current target position
            if (!hasReachedDestination && CurrentPathIndex < pathCells.Count)
            {
                Vector2Int targetCell = pathCells[CurrentPathIndex];
                // Skip end marker
                if (!(targetCell.x == -1 && targetCell.y == -1))
                {
                    Vector3 targetPosition;
                    if (gridManager.TryGetCellObject(targetCell, out GameObject targetCellObject))
                    {
                        // Use the exact cell position
                        targetPosition = new Vector3(
                            targetCellObject.transform.position.x,
                            heightOffset,
                            targetCellObject.transform.position.z
                        );
                    }
                    else
                    {
                        // Fallback to calculated position
                        float cellSize = gridManager.CellSize;
                        targetPosition = new Vector3(
                            targetCell.x * cellSize,
                            heightOffset,
                            targetCell.y * cellSize
                        );
                    }
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(targetPosition, 0.3f);
                }
            }
        }
    }
}