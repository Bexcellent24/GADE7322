using UnityEngine;
using System.Collections.Generic;

public class AuraAttacker : MonoBehaviour
{
    
   

    [Header("Visuals")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private Transform firePoint;

    private float tickTimer;
    private List<IDamageable> targetsInRange = new List<IDamageable>();
    private List<GameObject> activeBeams = new List<GameObject>();

    private float range;
    private float damagePerSecond;
    private float tickRate = 0.2f;
    
    public void Initialize(float range, float damagePerSecond)
    {
        this.range = range;
        this.damagePerSecond = damagePerSecond;
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
        
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        
        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>();
            var actor = hit.GetComponent<Actor>();

            // Only add enemies from opposite faction
            if (damageable != null && actor != null && damageable.IsAlive)
            {
                if (actor.faction != GetComponent<Actor>().faction)
                {
                    targetsInRange.Add(damageable);
                }
            }
        }
    }

    private void ApplyDamage()
    {
        if (targetsInRange.Count == 0) return;

        int damageThisTick = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * tickRate));

        foreach (var target in targetsInRange)
        {
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damageThisTick);
            }
        }
    }

    private void UpdateBeamVisuals()
    {
        if (laserBeamPrefab == null) return;

        // Remove destroyed beams
        for (int i = activeBeams.Count - 1; i >= 0; i--)
        {
            if (activeBeams[i] == null)
            {
                activeBeams.RemoveAt(i);
            }
        }

        // Destroy excess beams
        while (activeBeams.Count > targetsInRange.Count)
        {
            if (activeBeams.Count > 0)
            {
                Destroy(activeBeams[activeBeams.Count - 1]);
                activeBeams.RemoveAt(activeBeams.Count - 1);
            }
        }

        // Create new beams as needed
        while (activeBeams.Count < targetsInRange.Count)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject beam = Instantiate(laserBeamPrefab, spawnPos, Quaternion.identity, transform);
            activeBeams.Add(beam);
        }

        // Update beam positions
        for (int i = 0; i < targetsInRange.Count && i < activeBeams.Count; i++)
        {
            if (targetsInRange[i] != null && targetsInRange[i].IsAlive && activeBeams[i] != null)
            {
                UpdateBeam(activeBeams[i], targetsInRange[i]);
            }
        }
    }

    private void UpdateBeam(GameObject beam, IDamageable target)
    {
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 endPos = target.Transform.position;

        LineRenderer line = beam.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
        }
    }

    private void OnDestroy()
    {
        // Clean up all beams
        foreach (var beam in activeBeams)
        {
            if (beam != null)
                Destroy(beam);
        }
        activeBeams.Clear();
    }
}