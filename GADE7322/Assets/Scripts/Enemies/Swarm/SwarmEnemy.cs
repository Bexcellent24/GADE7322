// Assets/Scripts/Enemies/Swarm/SwarmEnemy.cs
using UnityEngine;

[RequireComponent(typeof(WaterGlobeNavigator))]
public class SwarmEnemy : MonoBehaviour, IDamageable
{
    [Header("Combat")]
    public float attackRange  = 1.75f;
    public float attackRate   = 0.6f;
    public float attackDamage = 6f;

    [Header("Targeting")]
    public LayerMask towerMask;
    public string towerTag = "Tower";

    [Header("VFX hook (optional)")]
    public ParticleSystem swarmParticles;
    SwarmParticlesController swarmCtl;

    WaterGlobeNavigator nav;
    Transform target;
    float atkTimer;
    readonly Collider[] hits = new Collider[32];

    Health health; // use central health system

    public int CurrentHealth => health ? health.Current : 0;
    public bool IsAlive => health && health.IsAlive;
    public Transform Transform => this.transform;

    void Awake()
    {
        nav = GetComponent<WaterGlobeNavigator>();
        health = GetComponent<Health>();
        swarmCtl = swarmParticles
            ? swarmParticles.GetComponent<SwarmParticlesController>()
            : GetComponentInChildren<SwarmParticlesController>();
    }

    void Update()
    {
        // acquire target every ~0.5s (30 frames @ 60fps)
        if (Time.frameCount % 30 == 0) target = FindClosestTower();

        // steer along great-circle toward target
        if (target)
        {
            var world = WaterWorldManager.Instance;
            var center = world ? world.PlanetCenter.position : Vector3.zero;
            var dir = (target.position - center).normalized;
            nav.SetLocalGoalDirection(dir);
        }
        else
        {
            // no target, revert to global behaviour
            nav.SetLocalGoalDirection(null);
        }

        // attack tick
        if (target && IsAlive)
        {
            atkTimer += Time.deltaTime;
            if (atkTimer >= attackRate)
            {
                atkTimer = 0f;
                if (Vector3.Distance(transform.position, target.position) <= attackRange &&
                    target.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(Mathf.RoundToInt(attackDamage));
                }
            }
        }
    }

    Transform FindClosestTower()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, 25f, hits, towerMask);
        float best = float.PositiveInfinity;
        Transform tBest = null;
        for (int i = 0; i < n; i++)
        {
            var t = hits[i].transform;
            if (!t) continue;
            if (!string.IsNullOrEmpty(towerTag) && !t.CompareTag(towerTag)) continue;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; tBest = t; }
        }
        return tBest;
    }

    // IDamageable — forward to central Health so UI/currency/etc. work
    public void TakeDamage(int amount)
    {
        if (!health) return;
        int before = health.Current;
        health.TakeDamage(amount);

        if (swarmCtl && health.Current != before)
        {
            swarmCtl.SetMaxHealth(health.Max);
            swarmCtl.SetCurrentHealth(health.Current);
        }
    }
}
