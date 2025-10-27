using UnityEngine;

[RequireComponent(typeof(WaterGlobeNavigator))]
[DisallowMultipleComponent]
public class SwarmEnemy : MonoBehaviour, IDamageable
{
    [Header("Combat")]
    public float attackRange  = 1.75f;
    public float attackRate   = 0.6f;   
    public float attackDamage = 6f;     

    [Header("Targeting")]
    public LayerMask towerMask;
    public string towerTag = "Tower";

    [Header("Aura")]
    [Tooltip("Aura component that deals DPS and renders 4 beams.")]
    public AuraAttackerMulti aura;
    [Tooltip("Enemy layer")]
    public LayerMask auraTargetLayers;
    [Tooltip("Beam sockets.")]
    public Transform[] beamSockets;

    WaterGlobeNavigator nav; // Sperical movement 
    Transform target; // Target steering
    readonly Collider[] hits = new Collider[32]; // Buffer for non-allocated colliders
    Health health; 

    // IDamageable implementations
    public int CurrentHealth => health ? health.Current : 0;
    public bool IsAlive => health && health.IsAlive;
    public Transform Transform => this.transform;

    void Awake()
    {
        nav = GetComponent<WaterGlobeNavigator>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        // Convert regular DPS to aura DPS
        float dps = (attackRate > 0f) ? (attackDamage / attackRate) : attackDamage;

        if (!aura) aura = GetComponent<AuraAttackerMulti>();
        if (aura)
        {
            // Push range and DPS
            aura.Initialize(attackRange, dps);

            var so = typeof(AuraAttackerMulti).GetField("beamSockets",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (so != null && beamSockets != null && beamSockets.Length > 0) so.SetValue(aura, beamSockets);

            // Push target layers 
            var tl = typeof(AuraAttackerMulti).GetField("targetLayers",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (tl != null) tl.SetValue(aura, auraTargetLayers);
        }
        else
        {
            Debug.LogWarning("[SwarmEnemy] No AuraAttackerMulti assigned");
        }
    }

    void Update()
    {
        // Re-acquire the steering target every 30 frames
        if (Time.frameCount % 30 == 0) target = FindClosestTower();

        // steer along the sphere towards the target
        if (target)
        {
            var world = WaterWorldManager.Instance;
            var center = world ? world.PlanetCenter.position : Vector3.zero;
            var dir = (target.position - center).normalized;
            nav.SetLocalGoalDirection(dir);
        }
        else
        {
            nav.SetLocalGoalDirection(null);
        }
    }

    // Returns the nearest transform that has a tower based on layer and optional tag.
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

    public void TakeDamage(int amount)
    {
        if (!health) return;
        int before = health.Current;
        health.TakeDamage(amount);
        
    }
}
