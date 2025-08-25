using UnityEngine;

public class Attacker : MonoBehaviour
{
    private float range;
    private float fireRate;
     private GameObject bulletPrefab;
     private Transform firePoint;

    private float fireCooldown;
    private IDamageable currentTarget;

    public void Initialize(GameObject bulletPrefab, float range, float fireRate, Transform firePoint)
    {
        this.bulletPrefab = bulletPrefab;
        this.range = range;
        this.fireRate = fireRate;
        this.firePoint = firePoint;
    }
    
    void Update()
    {
        fireCooldown -= Time.deltaTime;

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
        var bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Init(currentTarget);
        }
    }
}