using System.Collections.Generic;
using UnityEngine;

public class EnemyCompositionSelector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How strongly to bias toward threatening enemies when player does well")]
    [Range(0f, 3f)]  [SerializeField] private float threatBias = 1.5f;

    [System.Serializable]
    public class EnemyStats
    {
        public int spawned;
        public float totalSurvival;
        public float totalDamage;
        [HideInInspector] public float threat; // Calculated
    }

    [SerializeField] private EnemyStats light = new EnemyStats();
    [SerializeField] private EnemyStats medium = new EnemyStats();
    [SerializeField] private EnemyStats heavy = new EnemyStats();
    [SerializeField] private EnemyStats towerHunter = new EnemyStats();


    /// Record enemy death
    public void RecordDeath(EnemyKind kind, float survival, float damage)
    {
        EnemyStats stats = GetStats(kind);
        stats.spawned++;
        stats.totalSurvival += survival;
        stats.totalDamage += damage;

        // Calculate threat: normalized survival + damage
        float avgSurvival = stats.totalSurvival / stats.spawned;
        float avgDamage = stats.totalDamage / stats.spawned;
        stats.threat = (avgSurvival / 30f) * 0.5f + (avgDamage / 100f) * 0.5f;
    }


    /// Get spawn weights based on performance
    public Dictionary<EnemyKind, float> GetWeights(float score, bool includeTowerHunter)
    {
        var weights = new Dictionary<EnemyKind, float>
        {
            { EnemyKind.Light, 1f },
            { EnemyKind.Medium, 1f },
            { EnemyKind.Heavy, 1f }
        };

        // If we have data adjust based on threat and performance
        if (light.spawned > 0 || medium.spawned > 0 || heavy.spawned > 0)
        {
            float perfNormalized = score / 100f; // 0-1

            foreach (var kind in new[] { 
                EnemyKind.Light, 
                EnemyKind.Medium, 
                EnemyKind.Heavy })
            {
                EnemyStats stats = GetStats(kind);
                if (stats.spawned > 0)
                {
                    // High perf → more high-threat enemies
                    float adjustment = Mathf.Lerp(-threatBias, threatBias, perfNormalized);
                    weights[kind] *= (1f + adjustment * stats.threat);
                    weights[kind] = Mathf.Max(0.1f, weights[kind]); // Never zero
                }
            }
        }

        if (includeTowerHunter)
            weights[EnemyKind.TowerHunter] = 0.5f;

        return weights;
    }
    
    /// Select random enemy type based on weights
    public EnemyKind SelectType(Dictionary<EnemyKind, float> weights)
    {
        float total = 0f;
        foreach (var w in weights.Values) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative) return kvp.Key;
        }

        return EnemyKind.Light;
    }

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