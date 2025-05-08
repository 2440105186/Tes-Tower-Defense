using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    #region Serialized Fields
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float baseMovementSpeed = 2f;
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
    #endregion

    #region Events and Properties
    public event System.Action<IDamageable> OnDestroyed;
    public event System.Action<IDamageable, float> OnDamaged;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int CurrentPathIndex { get; private set; } = 0;
    #endregion

    #region Private Fields
    private GridManager gridManager;
    private List<Vector2Int> pathCells = new List<Vector2Int>();
    private Vector3 currentTargetPosition;
    private Vector2Int lastGridPosition = new Vector2Int(-1, -1);
    private bool hasReachedDestination = false;
    private float currentMovementSpeed;
    
    private bool isAttackingGate = false;
    private bool isGateDestroyed = false;
    private int gateTargetPathIndex = -1;
    
    private float attackCooldown = 0f;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        InitializeComponents();
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
    }
    
    private void Update()
    {
        UpdateAttackCooldown();
        
        if (isAttackingGate && !isGateDestroyed)
        {
            ScanForGate();
            return;
        }
        
        if (!hasReachedDestination)
        {
            ScanForTargets();
            MoveAlongPath();
        }
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        currentHealth = maxHealth;
        currentMovementSpeed = baseMovementSpeed;
        gridManager = FindFirstObjectByType<GridManager>();
        
        if (gridManager == null)
        {
            Debug.LogError("No GridManager found in the scene! Enemy will not be able to navigate.");
        }
    }
    
    private void InitializePath()
    {
        if (gridManager == null) return;
        
        pathCells = gridManager.GetPathCells();
        
        if (pathCells.Count > 0)
        {
            FindGatePosition();
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
        
        int endMarkerIndex = FindEndMarkerIndex();
        
        if (endMarkerIndex > 0)
        {
            gateTargetPathIndex = endMarkerIndex - 1;
        }
        else if (pathCells.Count > 1)
        {
            gateTargetPathIndex = pathCells.Count - 2;
        }
    }
    
    private int FindEndMarkerIndex()
    {
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (IsEndMarker(pathCells[i]))
            {
                return i;
            }
        }
        return -1;
    }
    #endregion

    #region Movement
    private void UpdateTargetPosition()
    {
        if (CurrentPathIndex >= pathCells.Count)
        {
            hasReachedDestination = true;
            return;
        }
        
        Vector2Int targetCell = pathCells[CurrentPathIndex];
        
        if (IsEndMarker(targetCell))
        {
            hasReachedDestination = true;
            OnReachedDestination();
            return;
        }
        
        currentTargetPosition = CalculateCellPosition(targetCell);
        
        if (CurrentPathIndex == gateTargetPathIndex && !isGateDestroyed)
        {
            isAttackingGate = true;
        }
    }
    
    private Vector3 CalculateCellPosition(Vector2Int cellCoordinates)
    {
        if (gridManager.TryGetCellObject(cellCoordinates, out GameObject cellObject))
        {
            return new Vector3(
                cellObject.transform.position.x,
                heightOffset,
                cellObject.transform.position.z
            );
        }
        
        float cellSize = gridManager.CellSize;
        return new Vector3(
            cellCoordinates.x * cellSize,
            heightOffset,
            cellCoordinates.y * cellSize
        );
    }
    
    private bool IsEndMarker(Vector2Int cell)
    {
        return cell.x == -1 && cell.y == -1;
    }
    
    private void MoveAlongPath()
    {
        if (isAttackingGate && !isGateDestroyed) return;
    
        // Get the current cell position
        Vector2Int currentCell = GetCurrentGridCell();
    
        // Check if we've moved to a new cell
        if (currentCell != lastGridPosition)
        {
            // Apply any cell effects (like mud) to enemy movement
            UpdateMovementSpeedForCell(currentCell);
            lastGridPosition = currentCell;
        }
    
        // Calculate direction to target
        Vector3 directionToTarget = currentTargetPosition - transform.position;
        directionToTarget.y = 0; // Keep movement on the horizontal plane
        float distanceToTarget = directionToTarget.magnitude;
    
        // Check if we've reached the target cell
        if (distanceToTarget <= cellArrivalThreshold)
        {
            AdvanceToNextPathPoint();
            return;
        }
    
        // Handle very small distances (avoid jittering)
        if (distanceToTarget < 0.05f)
        {
            ForceCompletionOfCurrentPoint();
            return;
        }
    
        // Move towards the target if we have a valid direction
        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            // Update rotation to face movement direction
            UpdateRotation(currentTargetPosition);
        
            // Move forward at the current speed (affected by modifiers like mud)
            transform.position += transform.forward * (currentMovementSpeed * Time.deltaTime);
        
            // Visualize movement speed (optional)
            DebugMovementSpeed();
        }
    }
    
    private Vector2Int GetCurrentGridCell()
    {
        if (gridManager == null) return new Vector2Int(-1, -1);
    
        Vector2Int cell;
        if (gridManager.TryGetCellAtPosition(transform.position, out cell))
        {
            return cell;
        }
    
        return new Vector2Int(-1, -1);
    }
    
    private void UpdateMovementSpeedForCell(Vector2Int cell)
    {
        if (gridManager == null || !IsValidCell(cell)) 
        {
            // Reset to base speed if we're not on a valid grid cell
            currentMovementSpeed = baseMovementSpeed;
            return;
        }
    
        // Get the movement modifier from the cell (handles mud and other effects)
        float speedModifier = gridManager.GetCellMovementModifier(cell);
    
        // Log debug info
        if (speedModifier < 1.0f)
        {
            Debug.Log($"Enemy at cell {cell} with modifier {speedModifier:F2}");
        }
    
        // Apply the modifier to base speed
        float previousSpeed = currentMovementSpeed;
        currentMovementSpeed = baseMovementSpeed * speedModifier;
    
        // Log if speed changed significantly
        if (Mathf.Abs(previousSpeed - currentMovementSpeed) > 0.1f)
        {
            Debug.Log($"Enemy speed changed: {previousSpeed:F2} → {currentMovementSpeed:F2} (Modifier: {speedModifier:P0})");
        }
    }
    
    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < gridManager.GridSizeX && cell.y < gridManager.GridSizeY;
    }

    private void DebugMovementSpeed()
    {
        if (currentMovementSpeed < baseMovementSpeed * 0.9f)
        {
            // Only debug when speed is significantly reduced
            Debug.DrawRay(transform.position, transform.forward * currentMovementSpeed, Color.red, 0.1f);
        }
    }
    
    private void AdvanceToNextPathPoint()
    {
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
    }
    
    private void ForceCompletionOfCurrentPoint()
    {
        transform.position = new Vector3(currentTargetPosition.x, transform.position.y, currentTargetPosition.z);
        AdvanceToNextPathPoint();
    }
    
    private void UpdateRotation(Vector3 targetPosition)
    {
        if (currentMovementSpeed <= 0.05f) return;
        
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        moveDirection.y = 0;
        
        if (moveDirection.sqrMagnitude <= 0.001f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }
    
    private void UpdateMovementSpeed()
    {
        if (gridManager == null) return;
        
        Vector2Int currentCell;
        if (!gridManager.TryGetCellAtPosition(transform.position, out currentCell)) 
        {
            currentMovementSpeed = baseMovementSpeed;
            return;
        }
        
        if (currentCell == lastGridPosition) return;
        
        Debug.Log($"Enemy at cell {currentCell}");
        
        CellType cellType = gridManager.GetCellType(currentCell);
        Debug.Log($"Cell type: {cellType}");
        
        float speedModifier = gridManager.GetCellMovementModifier(currentCell);
        Debug.Log($"Speed modifier: {speedModifier}");
        
        float previousSpeed = currentMovementSpeed;
        currentMovementSpeed = baseMovementSpeed * speedModifier;
        
        Debug.Log($"Enemy speed changed: {previousSpeed} -> {currentMovementSpeed} (Base: {baseMovementSpeed})");
        
        lastGridPosition = currentCell;
    }
    #endregion

    #region Combat
    private void UpdateAttackCooldown()
    {
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
    }
    
    private void ScanForGate()
    {
        if (isGateDestroyed)
        {
            isAttackingGate = false;
            return;
        }
    
        if (attackCooldown > 0) return;
    
        Gate gate = FindFirstObjectByType<Gate>();
        if (gate == null)
        {
            isGateDestroyed = true;
            isAttackingGate = false;
            return;
        }
        
        IDamageable damageableGate = gate.GetComponent<IDamageable>();
        if (damageableGate == null) return;
        
        currentTarget = damageableGate;
        currentTargetTransform = gate.transform;
        
        FireProjectileAt(currentTargetTransform);
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
        if (attackCooldown > 0 || isAttackingGate) return;
            
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        
        FindClosestTarget(hitColliders);
        
        if (currentTarget != null && attackCooldown <= 0)
        {
            Attack();
        }
    }
    
    private void FindClosestTarget(Collider[] hitColliders)
    {
        float closestDistance = float.MaxValue;
        IDamageable closestTarget = null;
        Transform closestTransform = null;
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable target = hitCollider.GetComponent<IDamageable>();
            if (target == null || target == this) continue;
            
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
                closestTransform = hitCollider.transform;
            }
        }
        
        currentTarget = closestTarget;
        currentTargetTransform = closestTransform;
    }
    
    private void Attack()
    {
        FireProjectileAt(currentTargetTransform);
    }
    
    private void FireProjectileAt(Transform target)
    {
        if (target == null || firePoint == null) return;
        
        Vector3 directionToTarget = target.position - firePoint.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, targetRotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(attackDamage);
        }
        
        attackCooldown = 1f / attackRate;
    }
    #endregion

    #region Health
    public float TakeDamage(float amount)
    {
        if (amount <= 0) return 0;
            
        float actualDamage = Mathf.Min(currentHealth, amount);
        currentHealth -= actualDamage;
        
        OnDamaged?.Invoke(this, actualDamage);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        return actualDamage;
    }
    
    private void Die()
    {
        OnDestroyed?.Invoke(this);
        
        enabled = false;
        
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        Destroy(gameObject, 2f);
    }
    
    private void OnReachedDestination()
    {
        Die();
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        DrawAttackRangeGizmo();
        DrawSpeedInfoGizmo();
        DrawPathGizmos();
    }
    
    private void DrawAttackRangeGizmo()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    private void DrawSpeedInfoGizmo()
    {
        if (!Application.isPlaying) return;
        
        Vector3 textPosition = transform.position + Vector3.up * 2f;
        
        string speedInfo = $"Speed: {currentMovementSpeed:F2} / {baseMovementSpeed:F2}\n" +
                           $"Modifier: {(currentMovementSpeed / baseMovementSpeed):P0}";
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(textPosition, speedInfo);
#endif
    }
    
    private void DrawPathGizmos()
    {
        if (pathCells == null || pathCells.Count == 0 || gridManager == null) return;
        
        Gizmos.color = Color.green;
        
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (IsEndMarker(pathCells[i])) continue;
            
            Vector3 cellPosition = CalculateCellPosition(pathCells[i]);
            
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
            
            DrawPathLineToNextCell(i, cellPosition);
        }
        
        DrawCurrentTargetGizmo();
    }
    
    private void DrawPathLineToNextCell(int index, Vector3 cellPosition)
    {
        if (index >= pathCells.Count - 1) return;
        if (IsEndMarker(pathCells[index+1])) return;
        
        Vector3 nextCellPosition = CalculateCellPosition(pathCells[index+1]);
        Gizmos.DrawLine(cellPosition, nextCellPosition);
    }
    
    private void DrawCurrentTargetGizmo()
    {
        if (hasReachedDestination || CurrentPathIndex >= pathCells.Count) return;
        
        Vector2Int targetCell = pathCells[CurrentPathIndex];
        if (IsEndMarker(targetCell)) return;
        
        Vector3 targetPosition = CalculateCellPosition(targetCell);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 0.3f);
    }
    #endregion
}