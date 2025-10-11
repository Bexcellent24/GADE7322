using UnityEngine;

public class BomberBullet : BaseBullet
{
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private GameObject explosionEffectPrefab;

    protected override void OnImpact()
    {
        Explode();
    }

    private void Explode()
    {
        // Spawn explosion effect if available
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Find all targets in explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>();
            var actor = hit.GetComponent<Actor>();

            // Damage enemies only (opposite faction)
            if (damageable != null && actor != null && damageable.IsAlive)
            {
                if (actor.faction != attackerFaction)
                {
                    damageable.TakeDamage((int)damage);
                }
            }
        }

        // Play explosion sound
        AudioManager.Instance?.PlaySFX("Explosion");

        Destroy(gameObject);
    }
}
