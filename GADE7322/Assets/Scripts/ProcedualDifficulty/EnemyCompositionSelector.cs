using System.Collections.Generic;
using UnityEngine;

public class EnemyCompositionSelector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How strongly to bias toward threatening enemies when player does well")]
    [Range(0f, 3f)]  [SerializeField] private float threatBias = 1.5f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;

    [System.Serializable]
    public class EnemyStats
    {
        public int spawned;
        public float totalSurvival;
        public float totalDamage;
        [HideInInspector] public float threat; // Calculated in runtime
    }

    [SerializeField] private EnemyStats light = new EnemyStats();
    [SerializeField] private EnemyStats medium = new EnemyStats();
    [SerializeField] private EnemyStats heavy = new EnemyStats();
    [SerializeField] private EnemyStats towerHunter = new EnemyStats();


    // Records an enemy death for performance analysis
    public void RecordDeath(EnemyKind kind, float survival, float damage)
    {
        EnemyStats stats = GetStats(kind);
        stats.spawned++;
        stats.totalSurvival += survival;
        stats.totalDamage += damage;

        // Estimate "threat" based on how long it survived and how much damage it dealt
        float avgSurvival = stats.totalSurvival / stats.spawned;
        float avgDamage = stats.totalDamage / stats.spawned;
        
        // Survival contributes half, damage contributes half
        stats.threat = (avgSurvival / 30f) * 0.5f + (avgDamage / 100f) * 0.5f;
        
       if(logDebugLogs) Debug.Log($"[Composition] {kind} recorded death. AvgSurvival={avgSurvival:F1}, AvgDamage={avgDamage:F1}, Threat={stats.threat:F2}");
    }


    // Calculates spawn weights for each enemy type based on player performance
    public Dictionary<EnemyKind, float> GetWeights(float score, bool includeTowerHunter)
    {
        var weights = new Dictionary<EnemyKind, float>
        {
            { EnemyKind.Light, 10f },
            { EnemyKind.Medium, 2f },
            { EnemyKind.Heavy, 0.1f }
        };

        // Only apply adjustment once we have enough data
        if (light.spawned > 0 || medium.spawned > 0 || heavy.spawned > 0)
        {
            float perfNormalized = score / 100f; // Convert score to 0–1 range
            float adjustment = Mathf.Lerp(-threatBias, threatBias, perfNormalized);

            if(logDebugLogs) Debug.Log($"[Composition] Adjusting weights based on score {score:F1} (Adj={adjustment:F2})");
            
            foreach (var kind in new[] { 
                EnemyKind.Light, 
                EnemyKind.Medium, 
                EnemyKind.Heavy })
            {
                EnemyStats stats = GetStats(kind);
                if (stats.spawned > 0)
                {
                    // More threat for higher-performing player
                    float before = weights[kind];
                    weights[kind] *= (1f + adjustment * stats.threat);
                    weights[kind] = Mathf.Max(0.1f, weights[kind]); // Never zero
                    if(logDebugLogs) Debug.Log($"[Composition] {kind}: Threat={stats.threat:F2}, Weight {before:F2}→{weights[kind]:F2}");
                }
            }
        }
        else
        {
            if(logDebugLogs) Debug.Log("[Composition] No enemy data yet. Using default equal weights.");
        }

        // Optionally include tower hunters
        if (includeTowerHunter)
        {
            weights[EnemyKind.TowerHunter] = 0.5f;
            if(logDebugLogs) Debug.Log("[Composition] TowerHunters enabled with weight 0.5");
        }
        
        return weights;
    }
    
    // Selects a random enemy type using weighted probabilities
    public EnemyKind SelectType(Dictionary<EnemyKind, float> weights)
    {
        float total = 0f;
        foreach (var w in weights.Values) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative)
            {
                if(logDebugLogs) Debug.Log($"[Composition] Selected {kvp.Key} (Roll={roll:F2}/{total:F2})");
                return kvp.Key;
            }
        }

        if(logDebugLogs) Debug.LogWarning("[Composition] Weighted selection failed. Defaulting to Light enemy.");
        return EnemyKind.Light;
    }

    // Returns stats container for specified enemy type
    EnemyStats GetStats(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Light: return light;
            case EnemyKind.Medium: return medium;
            case EnemyKind.Heavy: return heavy;
            case EnemyKind.TowerHunter: return towerHunter;
            default: return light;
        }
    }
}