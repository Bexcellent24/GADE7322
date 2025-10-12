using UnityEngine;
using System.Collections.Generic;

public class AuraAttacker : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float tickRate = 0.2f;
    [SerializeField] private LayerMask targetLayers;   
    [SerializeField] private bool popShieldOnBlock = true; 

    [Header("Visuals")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private Transform firePoint;

    private float range;
    private float damagePerSecond;

    private float tickTimer;
    private readonly List<IDamageable> targetsInRange = new();
    private readonly List<GameObject> activeBeams = new();

    private Actor selfActor;
    private int shieldLayer;
    private static readonly Collider[] hitsBuffer = new Collider[128]; 

    public void Initialize(float range, float damagePerSecond)
    {
        this.range = range;
        this.damagePerSecond = damagePerSecond;
    }

    void Awake()
    {
        selfActor = GetComponent<Actor>();
        if (!selfActor)
            Debug.LogWarning("[AuraAttacker] No Actor found on attacker.");

        shieldLayer = LayerMask.NameToLayer("Shield");
        if (shieldLayer == -1)
            Debug.LogWarning("[AuraAttacker] 'Shield' layer not found. LOS blocking will be skipped.");
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickRate)
        {
            FindTargetsInRange();
            ApplyDamage();
            tickTimer = 0f;
        }

        UpdateBeamVisuals();
    }

    private void FindTargetsInRange()
    {
        targetsInRange.Clear();

        int count = (targetLayers.value == 0)
            ? Physics.OverlapSphereNonAlloc(transform.position, range, hitsBuffer)
            : Physics.OverlapSphereNonAlloc(transform.position, range, hitsBuffer, targetLayers);

        for (int i = 0; i < count; i++)
        {
            var col = hitsBuffer[i];
            if (!col) continue;

            var damageable = col.GetComponent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) continue;

            var actor = col.GetComponent<Actor>();
            if (actor == null || selfActor == null) continue;

            if (actor.faction == selfActor.faction) continue;

            targetsInRange.Add(damageable);
        }
    }

    private void ApplyDamage()
    {
        if (targetsInRange.Count == 0) return;

        int damageThisTick = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * tickRate));
        Vector3 startPos = firePoint ? firePoint.position : transform.position;

        foreach (var target in targetsInRange)
        {
            if (target == null || !target.IsAlive) continue;

            Transform t = target.Transform;
            if (!t) continue;

            // Shield line-of-sight check
            if (IsBlockedByShield(startPos, t.position, out FrontalShield shieldHit))
            {
                if (popShieldOnBlock && shieldHit != null)
                {
                    
                }
                continue;
            }

            target.TakeDamage(damageThisTick);
        }
    }

    private void UpdateBeamVisuals()
    {
        if (laserBeamPrefab == null) return;

        // purge nulls
        for (int i = activeBeams.Count - 1; i >= 0; i--)
            if (activeBeams[i] == null) activeBeams.RemoveAt(i);

        // match beam count to targets
        while (activeBeams.Count > targetsInRange.Count)
        {
            var go = activeBeams[^1];
            if (go) Destroy(go);
            activeBeams.RemoveAt(activeBeams.Count - 1);
        }
        while (activeBeams.Count < targetsInRange.Count)
        {
            Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
            var beam = Instantiate(laserBeamPrefab, spawnPos, Quaternion.identity, transform);
            activeBeams.Add(beam);
        }

        // position beams
        Vector3 startPos = firePoint ? firePoint.position : transform.position;
        for (int i = 0; i < targetsInRange.Count && i < activeBeams.Count; i++)
        {
            var target = targetsInRange[i];
            var beam = activeBeams[i];
            if (target == null || !target.IsAlive || beam == null) continue;

            Vector3 endPos = target.Transform.position;

            // If blocked by a shield, snap the end of the beam to the shield hit point
            if (IsBlockedByShield(startPos, endPos, out _, out Vector3 shieldHitPoint))
            {
                endPos = shieldHitPoint;
            }

            var line = beam.GetComponent<LineRenderer>();
            if (line)
            {
                line.positionCount = 2;
                line.SetPosition(0, startPos);
                line.SetPosition(1, endPos);
            }
        }
    }

    
    private bool IsBlockedByShield(Vector3 from, Vector3 to, out FrontalShield shield)
    {
        return IsBlockedByShield(from, to, out shield, out _);
    }

    private bool IsBlockedByShield(Vector3 from, Vector3 to, out FrontalShield shield, out Vector3 hitPoint)
    {
        shield = null;
        hitPoint = default;

        if (shieldLayer == -1) return false; // no shield layer configured

        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return false;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, 1 << shieldLayer, QueryTriggerInteraction.Collide))
        {
            shield = hit.collider ? hit.collider.GetComponentInParent<FrontalShield>() : null;
            if (shield != null)
            {
                hitPoint = hit.point;
                return true;
            }
        }
        return false;
    }

    private void OnDestroy()
    {
        foreach (var beam in activeBeams)
            if (beam) Destroy(beam);
        activeBeams.Clear();
    }
}
