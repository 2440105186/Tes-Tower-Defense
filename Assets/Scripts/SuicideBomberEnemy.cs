using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class SuicideBomberEnemy : Enemy
{
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float explosionRadius = 3f;

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
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, towerLayers);

        Transform closest = null;
        float bestDistSq = float.MaxValue;

        foreach (var col in hits)
        {
            // only dash at things you can actually damage
            if (!col.TryGetComponent<IDamageable>(out var dmg)) 
                continue;
            // don’t dash at yourself
            if (dmg is Enemy && ReferenceEquals(dmg, this))
                continue;

            Vector3 targetPos = col.transform.position;
            Vector3 origin = transform.position + Vector3.up * 0.5f; 
            Vector3 dir = (targetPos - origin).normalized;
            float   dist = Vector3.Distance(origin, targetPos);

            // LOS raycast: if we hit something in environmentLayers before the target, skip
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, environmentLayers))
            {
                // hit something else first
                continue;
            }

            // passes LOS, now compare squared distance
            float distSq = (col.transform.position - transform.position).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                closest = col.transform;
            }
        }

        if (closest == null) return false;

        dashTarget = closest;
        isDashing  = true;
        return true;
    }

    private void PerformDash()
    {
        print($"Dash Target: {dashTarget}");
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
                d.TakeDamage(attackDamage);

        Die();
    }
}