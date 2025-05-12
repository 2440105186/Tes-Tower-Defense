using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class RangedEnemy : Enemy
{
    protected override void TryAttack()
    {
        if (attackCooldown > 0f)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, towerLayers);
        Transform closestTower = null;
        float closestDistSq = float.MaxValue;

        foreach (Collider col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg) && !ReferenceEquals(dmg, this))
            {
                Vector3 toTower = col.transform.position - firePoint.position;
                if (Physics.Raycast(firePoint.position, toTower.normalized, out RaycastHit hit, attackRange, environmentLayers)
                    && hit.transform != col.transform)
                    continue;

                float distSq = toTower.sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestTower = col.transform;
                }
            }
        }

        if (closestTower != null)
        {
            Quaternion rot = Quaternion.LookRotation(closestTower.position - firePoint.position);
            EnemyProjectilePool.Instance.SpawnProjectile(attackDamage, firePoint.position, rot);
            attackCooldown = 1f / attackRate;
        }
    }
}