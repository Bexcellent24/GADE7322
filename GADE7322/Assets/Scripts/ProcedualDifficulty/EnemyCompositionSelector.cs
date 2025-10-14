using System.Collections.Generic;
using UnityEngine;

public class EnemyCompositionSelector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How strongly to bias toward threatening enemies when player does well")]
    [Range(0f, 3f)]  [SerializeField] private float threatBias = 1.5f;
    
    [Tooltip("Minimum enemies of a type before we start trusting threat data")]
    [Min(1)] [SerializeField] private int minDataPoints = 5;
    
    [Tooltip("If an enemy type has been spawned less than this, force it to spawn occasionally")]
    [Min(0)] [SerializeField] private int minSpawnsBeforeNormal = 3;
    
    [SerializeField] private float towerHunterWeight = 0.8f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;

    [System.Serializable]
    public class EnemyStats
    {
        public int spawned;
        public float totalSurvival;
        public float totalDamage;
        [HideInInspector] public float threat; // Calculated in runtime
        [HideInInspector] public float confidence; // How confident we are in this threat value
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

        // Estimate threat based on how long it survived and how much damage it dealt
        float avgSurvival = stats.totalSurvival / stats.spawned;
        float avgDamage = stats.totalDamage / stats.spawned;
        
        // Survival contributes half, damage contributes half
        stats.threat = (avgSurvival / 30f) * 0.5f + (avgDamage / 100f) * 0.5f;
        
        // Confidence grows as we get more data points
        // With minDataPoints=5: at 5 spawns = 100%, at 1 spawn = 20%
        stats.confidence = Mathf.Clamp01((float)stats.spawned / minDataPoints);
        
        if(logDebugLogs) Debug.Log($"[Composition] {kind} recorded death. AvgSurvival={avgSurvival:F1}, AvgDamage={avgDamage:F1}, Threat={stats.threat:F2}, Confidence={stats.confidence:F2}");
    }


    // Calculates spawn weights for each enemy type based on player performance
    public Dictionary<EnemyKind, float> GetWeights(float score, bool includeTowerHunter)
    {
        var weights = new Dictionary<EnemyKind, float>
        {
            { EnemyKind.Light, 10f },
            { EnemyKind.Medium, 2f },
            { EnemyKind.Heavy, 0.75f }
        };

        //Force under-represented types to spawn so they can gather threat data
        ApplyBootstrapWeights(weights, includeTowerHunter);

        // Only apply adjustment once we have enough data
        if (light.spawned > 0 || medium.spawned > 0 || heavy.spawned > 0)
        {
            float perfNormalized = score / 100f; // Convert score to 0–1 range
            float adjustment = Mathf.Lerp(-threatBias, threatBias, perfNormalized);

            // Calculate total "presence" to avoid volume bias
            int totalSpawned = light.spawned + medium.spawned + heavy.spawned;
            if(logDebugLogs) Debug.Log($"[Composition] Adjusting weights based on score {score:F1} (Adj={adjustment:F2}). Total spawned: {totalSpawned}");
            
            foreach (var kind in new[] { 
                EnemyKind.Light, 
                EnemyKind.Medium, 
                EnemyKind.Heavy })
            {
                EnemyStats stats = GetStats(kind);
                if (stats.spawned > 0)
                {
                    // Weight the threat by confidence (how much data we have)
                    // This prevents low-spawn-count enemies from dominating
                    float weightedThreat = stats.threat * stats.confidence;
                    
                    // Also scale down if this type was spawned way more than others
                    // If an enemy type is 80% of all spawns, it's probably not the best indicator of difficulty
                    float spawnRatio = (float)stats.spawned / totalSpawned;
                    float volumePenalty = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(spawnRatio - 0.3f)); // Reduce influence if >30% of spawns
                    
                    float before = weights[kind];
                    weights[kind] *= (1f + adjustment * weightedThreat * volumePenalty);
                    weights[kind] = Mathf.Max(0.1f, weights[kind]); // Never zero
                    
                    if(logDebugLogs) Debug.Log($"[Composition] {kind}: Spawned={stats.spawned} ({spawnRatio:P0}), Threat={stats.threat:F2}, Confidence={stats.confidence:F2}, VolumePenalty={volumePenalty:F2}, Weight {before:F2}→{weights[kind]:F2}");
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
            weights[EnemyKind.TowerHunter] = towerHunterWeight;
            if(logDebugLogs) Debug.Log("[Composition] TowerHunters enabled with weight 0.5");
        }
        
        return weights;
    }

    // Ensures under-represented enemy types spawn occasionally to gather threat data
    void ApplyBootstrapWeights(Dictionary<EnemyKind, float> weights, bool includeTowerHunter)
    {
        var types = new[] { EnemyKind.Light, EnemyKind.Medium, EnemyKind.Heavy };
        
        foreach (var kind in types)
        {
            EnemyStats stats = GetStats(kind);
            
            // If this type hasn't spawned enough, boost its weight significantly
            if (stats.spawned < minSpawnsBeforeNormal)
            {
                float before = weights[kind];
                weights[kind] *= 3f; // Multiply weight by 3 to encourage spawning
                if(logDebugLogs) Debug.Log($"[Composition] BOOTSTRAP: {kind} has only spawned {stats.spawned} times. Boosting weight {before:F2}→{weights[kind]:F2}");
            }
        }
        
        // Same for tower hunters if enabled
        if (includeTowerHunter)
        {
            EnemyStats stats = GetStats(EnemyKind.TowerHunter);
            if (stats.spawned < minSpawnsBeforeNormal)
            {
                weights[EnemyKind.TowerHunter] = 2f; // Give it a reasonable base weight
                if(logDebugLogs) Debug.Log($"[Composition] BOOTSTRAP: TowerHunter has only spawned {stats.spawned} times. Enabling with weight 2.0");
            }
        }
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
    
    // Formats weights as a readable spawn chance string for debug output
    public string GetWeightsDebugString(Dictionary<EnemyKind, float> weights)
    {
        float total = 0f;
        foreach (var w in weights.Values) total += w;

        var chances = new System.Collections.Generic.List<string>();
        foreach (var kvp in weights)
        {
            float percent = (kvp.Value / total) * 100f;
            chances.Add($"{kvp.Key}={percent:F1}%");
        }

        return string.Join(", ", chances);
    }
    
    public void LogThreatBreakdown()
    {
        Debug.Log("[Overview][THREAT LEVELS]");
        
        var types = new[] { EnemyKind.Light, EnemyKind.Medium, EnemyKind.Heavy, EnemyKind.TowerHunter };
        EnemyKind mostThreatening = EnemyKind.Light;
        float maxThreat = 0f;

        foreach (var kind in types)
        {
            EnemyStats stats = GetStats(kind);
            if (stats.spawned == 0)
            {
                Debug.Log($" [Overview] {kind}: No data yet");
            }
            else
            {
                Debug.Log($"[Overview]  {kind}: Threat={stats.threat:F2} | Confidence={stats.confidence:P0} | Avg Survival={stats.totalSurvival / stats.spawned:F1}s | Avg Damage={stats.totalDamage / stats.spawned:F1}");
                
                if (stats.threat > maxThreat)
                {
                    maxThreat = stats.threat;
                    mostThreatening = kind;
                }
            }
        }

        if (maxThreat > 0)
            Debug.Log($" [Overview] [GREATEST THREAT: {mostThreatening} ({maxThreat:F2})]");
    }
}