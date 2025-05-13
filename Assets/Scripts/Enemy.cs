using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private VisionModes baseDetectedBy = VisionModes.Visual;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    public float CurrentMoveSpeed { get; private set; }
    public float BaseMoveSpeed => baseMoveSpeed;

    public VisionModes CurrentDetectedBy { get; private set; }
    public VisionModes BaseDetectedBy => baseDetectedBy;

    public int CurrentPathIndex => pathIndex;

    public event Action<IDamageable> OnDestroyed;
    public event Action<IDamageable, float> OnDamaged;

    [Header("Pathfinding")]
    [SerializeField] private float cellArrivalThreshold = 0.1f;
    [SerializeField] private float heightOffset = 0.5f;

    protected GridManager gridManager;
    protected List<Vector2Int> pathCells = new();
    protected int pathIndex;
    protected Vector3 targetPosition;
    protected bool hasReachedDestination;

    protected bool isAttackingGate;
    protected bool gateDestroyed;
    protected int gateTargetIndex;

    [Header("Combat")]
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackRange = 5f;
    [SerializeField] protected float attackRate = 1f;
    [SerializeField] protected LayerMask towerLayers;
    [SerializeField] protected LayerMask environmentLayers;
    [SerializeField] protected Transform firePoint;

    protected float attackCooldown;

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentMoveSpeed = baseMoveSpeed;
        CurrentDetectedBy = baseDetectedBy;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
            Debug.LogError("No GridManager found; navigation will fail!");
    }

    protected virtual void Start()
    {
        pathIndex = 0;
        InitializePath();
    }

    protected virtual void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (isAttackingGate && !gateDestroyed)
        {
            HandleGateAttack();
            return;
        }

        if (!hasReachedDestination)
        {
            TryAttack();
            MoveAlongPath();
        }
    }

    protected void InitializePath()
    {
        pathCells = gridManager.GetPathCells();
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (pathCells[i] == new Vector2Int(-1, -1))
                gateTargetIndex = Mathf.Max(0, i - 1);
        }
        UpdateTargetPosition();
    }

    protected void UpdateTargetPosition()
    {
        if (pathIndex >= pathCells.Count)
        {
            hasReachedDestination = true;
            Die();
            return;
        }

        Vector2Int cell = pathCells[pathIndex];
        if (cell == new Vector2Int(-1, -1))
        {
            hasReachedDestination = true;
            Die();
            return;
        }

        if (gridManager.TryGetCellObject(cell, out var cellObj))
            targetPosition = new Vector3(cellObj.transform.position.x, heightOffset, cellObj.transform.position.z);
        else
            targetPosition = new Vector3(cell.x * gridManager.CellSize, heightOffset, cell.y * gridManager.CellSize);

        if (pathIndex == gateTargetIndex && !gateDestroyed)
            isAttackingGate = true;
    }

    protected virtual void MoveAlongPath()
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        float thresholdSq = cellArrivalThreshold * cellArrivalThreshold;
        if (dir.sqrMagnitude <= thresholdSq)
        {
            pathIndex++;
            UpdateTargetPosition();
            return;
        }

        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
        transform.position += transform.forward * CurrentMoveSpeed * Time.deltaTime;
    }

    protected virtual void TryAttack()
    {
        if (attackCooldown > 0f)
            return;

        if (firePoint == null)
        {
            Debug.LogWarning($"No fire point!");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, towerLayers);
        Transform closestTarget = null;
        float closestDistSq = float.MaxValue;

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg) && dmg is not Enemy && !ReferenceEquals(dmg, this))
            {
                Vector3 toTower = col.transform.position - firePoint.position;
                if (Physics.Raycast(firePoint.position, toTower.normalized, out RaycastHit hit, attackRange, environmentLayers)
                    && hit.transform != col.transform)
                    continue;

                float distSq = toTower.sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestTarget = col.transform;
                }
            }
        }

        if (closestTarget != null)
        {
            Quaternion rot = Quaternion.LookRotation(closestTarget.position - firePoint.position);
            EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, rot);
            attackCooldown = 1f / attackRate;

            if (closestTarget.TryGetComponent<Gate>(out var _))
            {
                isAttackingGate = true;
            }
        }
    }

    protected virtual void HandleGateAttack()
    {
        if (attackCooldown > 0f)
            return;

        Gate gate = FindFirstObjectByType<Gate>();
        if (gate == null)
        {
            gateDestroyed = true;
            isAttackingGate = false;
            return;
        }

        Vector3 toGate = gate.transform.position - firePoint.position;
        if (!Physics.Raycast(firePoint.position, toGate.normalized, out RaycastHit hit, attackRange, environmentLayers)
            || hit.transform == gate.transform)
        {
            Quaternion rot = Quaternion.LookRotation(toGate);
            EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, rot);
            attackCooldown = 1f / attackRate;
        }
    }

    public void SetMoveSpeed(float multiplier) => CurrentMoveSpeed = baseMoveSpeed * multiplier;

    public void ResetMoveSpeed() => CurrentMoveSpeed = baseMoveSpeed;

    public void SetDetectedBy(VisionModes mode) => CurrentDetectedBy = baseDetectedBy & ~mode;

    public void ResetDetectedBy() => CurrentDetectedBy = baseDetectedBy;

    public float TakeDamage(float amount)
    {
        float dealt = Mathf.Min(CurrentHealth, amount);
        CurrentHealth -= dealt;
        OnDamaged?.Invoke(this, dealt);
        if (CurrentHealth <= 0f) Die();
        return dealt;
    }

    protected virtual void Die()
    {
        OnDestroyed?.Invoke(this);
        GetComponent<Collider>().enabled = false;
        enabled = false;
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw path if available
        if (pathCells != null && gridManager != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathCells.Count - 1; i++)
            {
                var a = pathCells[i];
                var b = pathCells[i + 1];
                if (a == new Vector2Int(-1, -1) || b == new Vector2Int(-1, -1))
                    continue;

                Vector3 pa = gridManager.TryGetCellObject(a, out var ao)
                    ? ao.transform.position + Vector3.up * heightOffset
                    : new Vector3(a.x * gridManager.CellSize, heightOffset, a.y * gridManager.CellSize);
                Vector3 pb = gridManager.TryGetCellObject(b, out var bo)
                    ? bo.transform.position + Vector3.up * heightOffset
                    : new Vector3(b.x * gridManager.CellSize, heightOffset, b.y * gridManager.CellSize);

                Gizmos.DrawLine(pa, pb);
            }
        }
    }
}