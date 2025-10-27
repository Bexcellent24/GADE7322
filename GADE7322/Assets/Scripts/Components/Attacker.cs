using UnityEngine;

public class Attacker : MonoBehaviour
{
    private float range;
    private float fireRate;
    private float damage;
    private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;

    private float fireCooldown;
    private IDamageable currentTarget;
    private EnemyTracker tracker;
    
    public void Initialize(GameObject bulletPrefab, float range, float fireRate, float damage)
    {
        this.bulletPrefab = bulletPrefab;
        this.range = range;
        this.fireRate = fireRate;
        this.damage = damage;
    }
    
    void Start()
    {
        // will be null if this is a defender - no problem, don't worry about it
        tracker = GetComponent<EnemyTracker>();
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

    private Transform GetClosestFirePoint()
    {
        if (firePoints == null || firePoints.Length == 0)
            return transform;
        
        if (firePoints.Length == 1)
            return firePoints[0];
        
        // Multiple fire points so find closest to target
        Transform closest = firePoints[0];
        float closestDist = Vector3.Distance(firePoints[0].position, currentTarget.Transform.position);
        
        for (int i = 1; i < firePoints.Length; i++)
        {
            float dist = Vector3.Distance(firePoints[i].position, currentTarget.Transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = firePoints[i];
            }
        }
        
        return closest;
    }

    private void Fire()
    {
        Transform selectedFirePoint = GetClosestFirePoint();
        
        var bulletObj = Instantiate(bulletPrefab, selectedFirePoint.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<BaseBullet>();
        if (bullet != null)
        {
            bullet.Init(currentTarget, damage, GetComponent<Actor>().faction);
            
            if (tracker != null)
            {
                bullet.SetDamageTracker(tracker);
            }
            
        }
        
        
        AudioManager.Instance?.PlaySFX("Shoot");
    }
}