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

    public void Begin()
    {
        if (waveRunner == null)
        {
            currentWave = 0;
            waveRunner = StartCoroutine(WaveLoop());
        }
    }

    IEnumerator WaveLoop()
    {
        // Wait for prefabs
        while (!lightEnemyPrefab && !mediumEnemyPrefab && !heavyEnemyPrefab)
            yield return null;

        // Infinite loop, runs until game ends
        while (true)
        {
            currentWave++;
            yield return StartCoroutine(SpawnWave(currentWave));

            // Wait for all enemies to die
            while (enemiesAlive > 0)
                yield return new WaitForSeconds(0.5f);

            OnWaveComplete?.Invoke(currentWave);

            // Calculate performance
            var perf = performance.GetPerformance(currentWave);
            lastScore = difficulty.CalculateScore(perf);

            // Delay before next wave
            float delay = difficulty.CalculateDelay(lastScore);
            if (delay > 0f)
            {
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

        // Spawn enemies
        float interval = (count <= 1) ? 0f : spawnDuration / count;

        for (int i = 0; i < count; i++)
        {
            while (enemiesAlive >= maxConcurrent)
                yield return null;

            var kind = composition.SelectType(weights);
            SpawnEnemy(kind);

            if (interval > 0f)
                yield return new WaitForSeconds(interval);
            else
                yield return null;
        }
    }

    void SpawnEnemy(EnemyKind kind)
    {
        GameObject prefab = GetPrefab(kind);
        if (!prefab)
        {
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
            Debug.LogWarning("[AdaptiveSpawner] No spawn points available!");
            return;
        }

        // Spawn at the selected point
        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        go.SetActive(true);

        // Add tracker
        var tracker = go.AddComponent<EnemyTracker>();
        tracker.Init(this, kind, Time.time);

        enemiesAlive++;
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
    }
}
