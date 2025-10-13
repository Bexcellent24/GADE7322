using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(50)]
public class AdaptiveEnemyWaveSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject lightEnemyPrefab;
    [SerializeField] private GameObject mediumEnemyPrefab;
    [SerializeField] private GameObject heavyEnemyPrefab;
    [SerializeField] private GameObject towerHunterEnemyPrefab;

    [Header("Systems")]
    [SerializeField] private PerformanceTracker performance;
    [SerializeField] private DifficultyManager difficulty;
    [SerializeField] private EnemyCompositionSelector composition;
    [SerializeField] private AdaptiveSpawnPointSelector spawnSelector;

    [Header("Spawn Settings")]
    [SerializeField] private int baseWaveCount = 10;
    [SerializeField] private float spawnDuration = 5f;
    [SerializeField] private int maxConcurrent = 60;
    public List<Transform> spawnPoints;
    
    [Header("Debug Settings")]
    [SerializeField] private bool logDebugLogs = true;
    
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private float lastScore = 50f;
    private Coroutine waveRunner;

    public static event Action<int> OnWaveStarted;
    public static event Action<int, float> OnWaveCountdown;
    public static event Action<int> OnWaveComplete;

    void Start()
    {
        // Auto-find systems
        if (!performance) performance = FindObjectOfType<PerformanceTracker>();
        if (!difficulty) difficulty = FindObjectOfType<DifficultyManager>();
        if (!composition) composition = FindObjectOfType<EnemyCompositionSelector>();
        if (!spawnSelector) spawnSelector = FindObjectOfType<AdaptiveSpawnPointSelector>();
    }

    // Starts the wave system
    public void Begin()
    {
        if (waveRunner == null)
        {
            currentWave = 0;
            waveRunner = StartCoroutine(WaveLoop());
        }
    }

    // Main loop that controls wave flow
    IEnumerator WaveLoop()
    {
        // Wait for prefabs
        while (!lightEnemyPrefab && !mediumEnemyPrefab && !heavyEnemyPrefab)
            yield return null;

        // Infinite loop, runs until game ends
        while (true)
        {
            currentWave++;
            if(logDebugLogs) Debug.Log($"[Spawner] Starting wave {currentWave}");
            yield return StartCoroutine(SpawnWave(currentWave));

            // Wait for all enemies to die
            while (enemiesAlive > 0)
                yield return new WaitForSeconds(0.5f);

            if(logDebugLogs) Debug.Log($"[Spawner] Wave {currentWave} complete.");
            OnWaveComplete?.Invoke(currentWave);

            // Calculate performance
            var perf = performance.GetPerformance(currentWave);
            lastScore = difficulty.CalculateScore(perf);
            if(logDebugLogs) Debug.Log($"[Spawner] Performance score for wave {currentWave}: {lastScore:F1}");

            // Delay before next wave
            float delay = difficulty.CalculateDelay(lastScore);
            if (delay > 0f)
            {
                if(logDebugLogs) Debug.Log($"[Spawner] Waiting {delay:F1}s before next wave...");
                float t = delay;
                while (t > 0f)
                {
                    OnWaveCountdown?.Invoke(currentWave + 1, t);
                    yield return null;
                    t -= Time.deltaTime;
                }
            }
        }
    }

    // Handles spawning all enemies for a wave
    IEnumerator SpawnWave(int wave)
    {
        // Start tracking
        performance.StartWave(wave);
        OnWaveStarted?.Invoke(wave);

        // Calculate count
        int count = (wave == 1) 
            ? baseWaveCount 
            : difficulty.CalculateWaveCount(baseWaveCount, wave, lastScore);

        // Get composition
        bool allowHunters = difficulty.AllowTowerHunters(wave, lastScore);
        var weights = composition.GetWeights(lastScore, allowHunters);
        
        string weightsDebug = composition.GetWeightsDebugString(weights);
        if(logDebugLogs) Debug.Log($"[Spawner] Wave {wave}: {count} enemies. Spawn chances: {weightsDebug}");

        if(logDebugLogs) Debug.Log($"[Spawner] Wave {wave}: spawning {count} enemies. Hunters allowed: {allowHunters}");
        
        // Spawn enemies
        float interval = (count <= 1) ? 0f : spawnDuration / count;

        for (int i = 0; i < count; i++)
        {
            while (enemiesAlive >= maxConcurrent)
            {
                if(logDebugLogs) Debug.Log("[Spawner] Max concurrent enemies reached, waiting...");
                yield return null;
            }

            var kind = composition.SelectType(weights);
            SpawnEnemy(kind);

            if (interval > 0f)
                yield return new WaitForSeconds(interval);
            else
                yield return null;
        }
    }

    // Spawns a single enemy of given type
    void SpawnEnemy(EnemyKind kind)
    {
        GameObject prefab = GetPrefab(kind);
        if (!prefab)
        {
            if(logDebugLogs) Debug.LogWarning("[Spawner] Missing prefab for enemy type, defaulting to light enemy.");
            prefab = lightEnemyPrefab;
            if (!prefab) return;
        }

        // Use adaptive spawn point selection or fallback to first point
        Transform spawnPoint = null;
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            spawnPoint = spawnSelector 
                ? spawnSelector.SelectSpawnPoint(spawnPoints)
                : spawnPoints[Random.Range(0, spawnPoints.Count)];
        }

        if (!spawnPoint)
        {
            if(logDebugLogs) Debug.LogWarning("[AdaptiveSpawner] No spawn points available!");
            return;
        }

        // Spawn at the selected point
        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        go.SetActive(true);

        // Add tracker
        var tracker = go.AddComponent<EnemyTracker>();
        tracker.Init(this, kind, Time.time);

        enemiesAlive++;
        if(logDebugLogs) Debug.Log($"[Spawner] Spawned {kind} at {spawnPoint.name}. Total alive: {enemiesAlive}");
    }

    GameObject GetPrefab(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Light: return lightEnemyPrefab;
            case EnemyKind.Medium: return mediumEnemyPrefab;
            case EnemyKind.Heavy: return heavyEnemyPrefab;
            case EnemyKind.TowerHunter: return towerHunterEnemyPrefab;
            default: return lightEnemyPrefab;
        }
    }

    public void OnEnemyDied(EnemyKind kind, float survival, float damage, Vector3 pos)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        composition.RecordDeath(kind, survival, damage);
        if (spawnSelector) spawnSelector.RecordDeath(pos);
        if(logDebugLogs) Debug.Log($"[Spawner] {kind} died. Remaining enemies: {enemiesAlive}");
    }
}
