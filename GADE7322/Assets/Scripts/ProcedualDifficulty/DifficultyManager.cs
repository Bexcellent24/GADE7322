using UnityEngine;

/// Calculates performance score and difficulty multipliers.
public class DifficultyManager : MonoBehaviour
{
    [Header("Base Scaling")]
    [Tooltip("Each wave gets 10% harder")]
    [SerializeField] private float baseWaveScaling = 0.10f;

    [Header("Performance Weights")]
    [SerializeField] private float towerHealthWeight = 0.4f;
    [SerializeField] private float currencyWeight = 0.4f;
    [SerializeField] private float defendersWeight = 0.2f;

    [Header("Tower Hunter")]
    [SerializeField] private int towerHunterUnlockWave = 3;
    [SerializeField] private float towerHunterMinScore = 70f;

    [Header("Wave Delays")]
    [SerializeField] private float minDelay = 0f;      // High performance
    [SerializeField] private float maxDelay = 8f;      // Low performance


    /// Calculate 0-100 performance score
    public float CalculateScore(WavePerformance perf)
    {
        // Tower health: 0-1 → 0-100
        float towerScore = perf.towerHealthPercent * 100f;

        // Currency: Compare to expected
        float currencyRatio = Mathf.Clamp01((float)perf.currency / perf.expectedCurrency);
        float currencyScore = currencyRatio * 100f;

        // Defenders lost: 0 lost = 100, 5+ lost = 0
        float defenderScore = Mathf.Clamp01(1f - (perf.defendersLost / 5f)) * 100f;

        // Weighted average
        float totalWeight = towerHealthWeight + currencyWeight + defendersWeight;
        return (towerScore * towerHealthWeight + 
                currencyScore * currencyWeight + 
                defenderScore * defendersWeight) / totalWeight;
    }


    /// Gets difficulty multiplier based on score
    public float GetMultiplier(float score)
    {
        if (score >= 80) return 1.3f;  // Dominating
        if (score >= 60) return 1.15f; // Doing well
        if (score >= 40) return 1.0f;  // Average
        if (score >= 20) return 0.9f;  // Struggling
        return 0.8f;                    // Near defeat
    }


    /// Calculate next wave enemy count
    public int CalculateWaveCount(int baseCount, int waveNumber, float score)
    {
        float baseMultiplier = Mathf.Pow(1f + baseWaveScaling, waveNumber - 1);
        float perfMultiplier = GetMultiplier(score);
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * baseMultiplier * perfMultiplier));
    }


    /// Should Tower Hunters spawn?
    public bool AllowTowerHunters(int wave, float score)
    {
        return wave >= towerHunterUnlockWave && score >= towerHunterMinScore;
    }


    /// Calculate delay before next wave
    public float CalculateDelay(float score)
    {
        return Mathf.Lerp(maxDelay, minDelay, score / 100f);
    }
}