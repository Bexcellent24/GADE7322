using UnityEngine;

public class Attacker : MonoBehaviour
{
    
    
    private float range;
    private float fireRate;
    private float damage;
    private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    private float fireCooldown;
    private IDamageable currentTarget;

    public void Initialize(GameObject bulletPrefab, float range, float fireRate, float damage)
    {
        if (bulletPrefab) this.bulletPrefab = bulletPrefab; 
        this.range = range;
        this.fireRate = fireRate;
        this.damage = damage;
    }
    
    void Update()
    {
        fireCooldown -= Time.deltaTime;

        // If we don't have a projectile to shoot, do nothing (prevents null instantiation)
        if (bulletPrefab == null)
            return;

        if (currentTarget == null || !currentTarget.IsAlive ||
            Vector3.Distance(transform.position, currentTarget.Transform.position) > range)
        {
            FindTarget();
        }

        if (currentTarget != null && fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = 1f / fireRate;
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        float closest = Mathf.Infinity;
        IDamageable nearest = null;

        foreach (var hit in hits)
        {
            var dmg = hit.GetComponent<IDamageable>();
            var actor = hit.GetComponent<Actor>();

            if (dmg != null && actor != null && dmg.IsAlive)
            {
                if (actor.faction != GetComponent<Actor>().faction)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < closest)
                    {
                        closest = dist;
                        nearest = dmg;
                    }
                }
            }
        }

        currentTarget = nearest;
    }

    private void Fire()
    {
        if (!bulletPrefab) return; // extra guard
        var spawnPos = firePoint ? firePoint.position : transform.position;
        var bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) bullet.Init(currentTarget, damage);

        AudioManager.Instance?.PlaySFX("Shoot");
    }
}