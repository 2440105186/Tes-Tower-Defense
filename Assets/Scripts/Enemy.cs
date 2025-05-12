using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    #region Stats & Detection
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

    public event Action<IDamageable> OnDestroyed;
    public event Action<IDamageable, float> OnDamaged;
    #endregion

    #region Navigation
    [Header("Pathfinding")]
    [SerializeField] private float cellArrivalThreshold = 0.1f;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private LayerMask gridLayerMask;

    private GridManager gridManager;
    private List<Vector2Int> pathCells = new();
    public int CurrentPathIndex { get; private set; } = 0;
    private Vector3 currentTargetPosition;
    private bool hasReachedDestination = false;

    private bool isAttackingGate = false;
    private bool isGateDestroyed = false;
    private int gateTargetPathIndex = -1;
    #endregion

    #region Combat
    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private LayerMask towerLayers;
    [SerializeField] private LayerMask environmentLayers;
    [SerializeField] private Transform firePoint;

    private float attackCooldown;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        CurrentHealth = maxHealth;
        CurrentMoveSpeed = baseMoveSpeed;
        CurrentDetectedBy = baseDetectedBy;

        gridManager = GridManager.Instance;
        if (gridManager == null) Debug.LogError("No GridManager found; navigation will fail!");
    }

    private void OnEnable() => Gate.OnGateDestroyed += HandleGateDestroyed;
    private void OnDisable() => Gate.OnGateDestroyed -= HandleGateDestroyed;

    private void Start()
    {
        InitializePath();
    }

    private void Update()
    {
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;

        if (isAttackingGate && !isGateDestroyed)
        {
            HandleGateAttack();
            return;
        }

        if (!hasReachedDestination)
        {
            ScanForTargets();
            MoveAlongPath();
        }
    }
    #endregion

    #region Movement & Path
    private void InitializePath()
    {
        if (gridManager == null) return;

        pathCells = gridManager.GetPathCells();
        if (pathCells.Count == 0)
        {
            Debug.LogWarning("Enemy has no path!");
            hasReachedDestination = true;
            return;
        }

        // find gate index
        for (int i = 0; i < pathCells.Count; i++)
            if (pathCells[i] == new Vector2Int(-1, -1))
                gateTargetPathIndex = Mathf.Max(0, i - 1);

        CurrentPathIndex = 0;
        UpdateTargetPosition();
    }

    private void UpdateTargetPosition()
    {
        if (CurrentPathIndex >= pathCells.Count)
        {
            ReachDestination();
            return;
        }

        var cell = pathCells[CurrentPathIndex];
        if (cell == new Vector2Int(-1, -1))
        {
            ReachDestination();
            return;
        }

        if (gridManager.TryGetCellObject(cell, out var cellObj))
            currentTargetPosition = new Vector3(
                cellObj.transform.position.x,
                heightOffset,
                cellObj.transform.position.z
            );
        else
            currentTargetPosition = new Vector3(
                cell.x * gridManager.CellSize,
                heightOffset,
                cell.y * gridManager.CellSize
            );

        if (CurrentPathIndex == gateTargetPathIndex && !isGateDestroyed)
            isAttackingGate = true;
    }

    private void MoveAlongPath()
    {
        if (isAttackingGate && !isGateDestroyed) return;

        Vector3 toTarget = currentTargetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= cellArrivalThreshold * cellArrivalThreshold)
        {
            CurrentPathIndex++;
            UpdateTargetPosition();
            return;
        }

        RotateTowards(toTarget);
        transform.position += transform.forward * (CurrentMoveSpeed * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
    }

    private void ReachDestination()
    {
        hasReachedDestination = true;
        Die();
    }
    #endregion

    #region Combat Routines
    private void ScanForTargets()
    {
        if (attackCooldown > 0f || isAttackingGate) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, towerLayers);
        float minDist = float.MaxValue;
        IDamageable best = null;
        Transform bestT = null;

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg) && !ReferenceEquals(dmg, this))
            {
                // line-of-sight?
                Vector3 dir = col.transform.position - firePoint.position;
                if (Physics.Raycast(firePoint.position, dir.normalized, out RaycastHit hit, attackRange, environmentLayers)
                    && hit.transform != col.transform)
                    continue;

                float d = dir.sqrMagnitude;
                if (d < minDist)
                {
                    minDist = d;
                    best = dmg;
                    bestT = col.transform;
                }
            }
        }

        currentTarget = best;
        currentTargetTransform = bestT;
        if (currentTarget != null && attackCooldown <= 0f) FireAt(currentTargetTransform);
    }

    private void FireAt(Transform target)
    {
        Quaternion rot = Quaternion.LookRotation(target.position - firePoint.position);
        EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, rot);
        attackCooldown = 1f / attackRate;
    }

    private void HandleGateAttack()
    {
        if (attackCooldown > 0f) return;

        var gate = FindFirstObjectByType<Gate>();
        if (gate == null)
        {
            isGateDestroyed = true;
            isAttackingGate = false;
            return;
        }

        currentTarget = gate;
        currentTargetTransform = gate.transform;
        Quaternion rot = Quaternion.LookRotation(gate.transform.position - firePoint.position);
        EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, rot);
        attackCooldown = 1f / attackRate;
    }

    private void HandleGateDestroyed()
    {
        isGateDestroyed = true;
        isAttackingGate = false;
    }
    #endregion

    #region IDamageable
    public float TakeDamage(float amount)
    {
        if (amount <= 0f) return 0f;
        float dealt = Mathf.Min(CurrentHealth, amount);
        CurrentHealth -= dealt;
        OnDamaged?.Invoke(this, dealt);

        if (CurrentHealth <= 0f) Die();

        return dealt;
    }

    protected virtual void Die()
    {
        OnDestroyed?.Invoke(this);
        enabled = false;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject);
    }
    #endregion

    #region Modifiers API
    public void SetMoveSpeed(float multiplier) => CurrentMoveSpeed = baseMoveSpeed * multiplier;
    public void ResetMoveSpeed() => CurrentMoveSpeed = baseMoveSpeed;
    public void SetDetectedBy(VisionModes mode) => CurrentDetectedBy = baseDetectedBy & ~mode;
    public void ResetDetectedBy() => CurrentDetectedBy = baseDetectedBy;
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (pathCells != null && gridManager != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathCells.Count - 1; i++)
            {
                var a = pathCells[i];
                var b = pathCells[i + 1];
                if (a == new Vector2Int(-1, -1) || b == new Vector2Int(-1, -1)) continue;

                Vector3 pa = gridManager.TryGetCellObject(a, out var ao)
                    ? ao.transform.position
                    : new Vector3(a.x, heightOffset, a.y) * gridManager.CellSize;
                Vector3 pb = gridManager.TryGetCellObject(b, out var bo)
                    ? bo.transform.position
                    : new Vector3(b.x, heightOffset, b.y) * gridManager.CellSize;

                Gizmos.DrawLine(pa, pb);
            }
        }
    }
    #endregion
}