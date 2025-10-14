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
    
    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;


    // Calculates a 0–100 performance score from the player's wave results
    public float CalculateScore(WavePerformance perf)
    {
        // Convert tower health to a 0–100 score
        float towerScore = perf.towerHealthPercent * 100f;

        // Compare actual currency to expected
        float currencyRatio = Mathf.Clamp01((float)perf.currency / perf.expectedCurrency);
        float currencyScore = currencyRatio * 100f;

        // Fewer defender deaths = higher score
        float defenderScore = Mathf.Clamp01(1f - (perf.defendersLost / 5f)) * 100f;

        // Combine scores using weights
        float totalWeight = towerHealthWeight + currencyWeight + defendersWeight;
        float finalScore = (towerScore * towerHealthWeight +
                            currencyScore * currencyWeight +
                            defenderScore * defendersWeight) / totalWeight;
        
        if(logDebugLogs) Debug.Log($"[Difficulty] Score breakdown → Tower: {towerScore:F1}, Currency: {currencyScore:F1}, Defenders: {defenderScore:F1}, Final Score: {finalScore:F1}");

        return finalScore;
    }
    
    public void LogScoreBreakdown(float finalScore)
    {
        Debug.Log($"[Overview][FINAL SCORE: {finalScore:F1}/100]");
        
        if (finalScore >= 80) Debug.Log("[Overview]  Status: DOMINATING → Next wave +30% enemies");
        else if (finalScore >= 60) Debug.Log("[Overview]  Status: DOING WELL → Next wave +15% enemies");
        else if (finalScore >= 40) Debug.Log("[Overview]  Status: AVERAGE → Next wave normal");
        else if (finalScore >= 20) Debug.Log("[Overview]  Status: STRUGGLING → Next wave -10% enemies");
        else Debug.Log("[Overview]  Status: NEAR DEFEAT → Next wave -20% enemies");
    }


    // Returns a difficulty multiplier (how much harder the next wave will be)
    public float GetMultiplier(float score)
    {
        float multiplier;

        if (score >= 80) multiplier = 1.3f;   // Dominating
        else if (score >= 60) multiplier = 1.15f; // Doing well
        else if (score >= 40) multiplier = 1.0f;  // Average
        else if (score >= 20) multiplier = 0.9f;  // Struggling
        else multiplier = 0.8f;                  // Near defeat

        if(logDebugLogs) Debug.Log($"[Difficulty] Multiplier for score {score:F1}: {multiplier:F2}");
        return multiplier;
    }


    // Calculate next wave enemy count
    public int CalculateWaveCount(int baseCount, int waveNumber, float score)
    {
        // Base scaling increases count per wave exponentially
        float baseMultiplier = Mathf.Pow(1f + baseWaveScaling, waveNumber - 1);

        // Adjust based on player performance
        float perfMultiplier = GetMultiplier(score);

        int result = Mathf.Max(1, Mathf.RoundToInt(baseCount * baseMultiplier * perfMultiplier));

        if(logDebugLogs) Debug.Log($"[Difficulty] Wave {waveNumber}: BaseCount={baseCount}, BaseMult={baseMultiplier:F2}, PerfMult={perfMultiplier:F2} → Total={result}");

        return result;
    }


    // Determines if tower-hunter enemies should be allowed this wave
    public bool AllowTowerHunters(int wave, float score)
    {
        bool allowed = wave >= towerHunterUnlockWave && score >= towerHunterMinScore;
        if(logDebugLogs) Debug.Log($"[Difficulty] TowerHunters allowed? {allowed} (Wave={wave}, Score={score:F1})");
        return allowed;
    }
    
    // Calculate delay before next wave
    public float CalculateDelay(float score)
    {
        float delay = Mathf.Lerp(maxDelay, minDelay, score / 100f);
        if(logDebugLogs) Debug.Log($"[Difficulty] Delay for next wave: {delay:F1}s (Score={score:F1})");
        return delay;
    }
}