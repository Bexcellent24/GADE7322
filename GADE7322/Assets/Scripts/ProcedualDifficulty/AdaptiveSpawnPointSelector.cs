using System.Collections.Generic;
using UnityEngine;


/// Tracks death locations and favors spawn points away from combat.
public class AdaptiveSpawnPointSelector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Max deaths to track")]
    [SerializeField] private int maxDeaths = 50;

    [Tooltip("Radius around deaths that count as 'hot'")]
    [SerializeField] private float hotZoneRadius = 15f;

    [Tooltip("How strongly to avoid hot zones")]
    [Range(0f, 3f)] public float avoidanceStrength = 1.5f;

    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;
    
    [SerializeField]private List<Vector3> deathPositions = new List<Vector3>();


    // Store death positions for heatmap logic
    public void RecordDeath(Vector3 pos)
    {
        deathPositions.Add(pos);
        if (deathPositions.Count > maxDeaths)
            deathPositions.RemoveAt(0);
        
        if(logDebugLogs) Debug.Log($"[SpawnSelector] Recorded death at {pos}. Total tracked: {deathPositions.Count}");
    }
    
    // Chooses a spawn point away from death clusters
    public Transform SelectSpawnPoint(List<Transform> points)
    {
        if (points == null || points.Count == 0)
        {
            if(logDebugLogs) Debug.LogWarning("[SpawnSelector] No spawn points provided.");
            return null;
        }
        if (points.Count == 1) return points[0];
        if (deathPositions.Count == 0) return points[Random.Range(0, points.Count)];

        // Calculate heat for each spawn point
        var weights = new Dictionary<Transform, float>();

        foreach (var point in points)
        {
            if (!point) continue;

            float heat = 0f;
            foreach (var deathPos in deathPositions)
            {
                float dist = Vector3.Distance(point.position, deathPos);
                if (dist < hotZoneRadius)
                    heat += (1f - dist / hotZoneRadius);
            }

            // Invert heat to weight (low heat = high weight)
            weights[point] = 1f / (1f + heat * avoidanceStrength);
        }

        // Weighted random selection
        float total = 0f;
        foreach (var w in weights.Values) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative)
            {
                if(logDebugLogs) Debug.Log($"[SpawnSelector] Selected spawn point: {kvp.Key.name}");
                return kvp.Key;
            }

        }

        return points[0];
    }

    void OnDrawGizmos()
    {
        if (deathPositions == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        foreach (var pos in deathPositions)
        {
            Gizmos.DrawSphere(pos, 0.5f);
            Gizmos.DrawWireSphere(pos, hotZoneRadius);
        }
    }
}