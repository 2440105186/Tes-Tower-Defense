using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class SuicideBomberEnemy : Enemy
{
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 30f;

    private bool isDashing;
    private Transform dashTarget;

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (isDashing)
        {
            PerformDash();
            return;
        }

        if (isAttackingGate && !gateDestroyed)
        {
            dashTarget = FindFirstObjectByType<Gate>()?.transform;
            if (dashTarget != null)
                isDashing = true;
            return;
        }

        if (TryStartDash())
            return;

        if (!hasReachedDestination)
            MoveAlongPath();
    }

    private bool TryStartDash()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRange, towerLayers);
        Transform closest = null;
        float closestDistSq = float.MaxValue;

        foreach (Collider col in hits)
        {
            float distSq = (col.transform.position - transform.position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closest = col.transform;
            }
        }

        if (closest == null)
            return false;

        dashTarget = closest;
        isDashing = true;
        return true;
    }

    private void PerformDash()
    {
        if (dashTarget == null)
        {
            Die();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, dashTarget.position, dashSpeed * Time.deltaTime);
        transform.LookAt(dashTarget);

        float distSq = (dashTarget.position - transform.position).sqrMagnitude;
        if (distSq <= 0.25f)
            Explode();
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in hits)
            if (col.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(explosionDamage);

        Die();
    }
}