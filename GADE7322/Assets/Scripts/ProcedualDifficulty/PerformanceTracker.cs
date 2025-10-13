using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How much currency should player have per wave for 'average' performance")]
    [SerializeField] private int currencyPerWave = 120;
    
    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;

    private int defendersLostThisWave;
    private Health mainTowerHealth;

    void Awake()
    {
        Health.OnDefenderDied += OnDefenderLost;
    }

    void OnDestroy()
    {
        Health.OnDefenderDied -= OnDefenderLost;
    }
    
    // Called at start of each wave to reset stats
    public void StartWave(int waveNumber)
    {
        defendersLostThisWave = 0;

        // Find main tower 
        if (mainTowerHealth == null)
        {
            foreach (var health in FindObjectsOfType<Health>())
            {
                // The main tower is the one that triggers game over
                if (health.CompareTag("MainTower"))
                {
                    mainTowerHealth = health;
                    break;
                }
            }
        }
    }
    
    /// Gets wave performance data
    public WavePerformance GetPerformance(int waveNumber)
    {
        float towerHealthPercent = 1f;
        if (mainTowerHealth != null)
            towerHealthPercent = (float)mainTowerHealth.Current / mainTowerHealth.Max;

        int currency = CurrencyManager.Instance ? CurrencyManager.Instance.CurrentCurrency : 0;
        int expectedCurrency = currencyPerWave;

        if(logDebugLogs) Debug.Log($"[PerformanceTracker] Wave {waveNumber} performance: Tower {towerHealthPercent:P0}, Currency {currency}/{expectedCurrency}, Defenders lost {defendersLostThisWave}");
        return new WavePerformance
        {
            towerHealthPercent = towerHealthPercent,
            currency = currency,
            expectedCurrency = expectedCurrency,
            defendersLost = defendersLostThisWave
        };
    }

    void OnDefenderLost()
    {
        defendersLostThisWave++;
        if(logDebugLogs) Debug.Log($"[PerformanceTracker] Defender lost. Total this wave: {defendersLostThisWave}");
    }
}

[System.Serializable]
public struct WavePerformance
{
    public float towerHealthPercent;  // 0-1
    public int currency;
    public int expectedCurrency;
    public int defendersLost;
}