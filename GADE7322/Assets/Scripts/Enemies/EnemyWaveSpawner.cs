using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(50)]
public class EnemyWaveSpawner : MonoBehaviour
{
    public enum EnemyKind { Light, Medium, Heavy }

    [Serializable]
    public struct Wave
    {
        [Min(0)] public int count;
        [Min(0f)] public float duration;
    }

    [Header("Enemy Prefabs (distinct visuals)")]
    public GameObject lightEnemyPrefab;
    public GameObject mediumEnemyPrefab;
    public GameObject heavyEnemyPrefab;

    [Header("Water Globe")]
    public Transform globeCenter;
    public float waterRadius = 10f;
    public float hoverOffset = 0f;
    public Vector3 goalPoleDir = Vector3.down;
    public LayerMask landMask;

    [Header("Planet (optional height fallback)")]
    public MarchingCubesPlanet planet;

    [Header("Spawn Ring (around top pole)")]
    [Range(0f, 45f)] public float spawnRingDegrees = 8f;
    public float spawnTangentialJitter = 0.75f;

    [Header("Waves")]
    public List<Wave> waves = new()
    {
        new Wave { count = 10, duration = 5f }
    };
    public float timeBetweenWaves = 5f;
    public bool loopWaves = false;
    public int maxConcurrent = 60;

    [Header("Optional Spawn Points")]
    public List<Transform> spawnPoints;

    [Header("Mode")]
    [Tooltip("If true, each spawn picks a random available prefab (uniform).")]
    public bool pureRandom = true;

    [Header("Run")]
    public bool verboseLogs = true;

    int _alive;
    Coroutine _runner;

    public static event Action<int> OnWaveStarted;
    public static event Action<int, float> OnWaveCountdown;

    public void Begin()
    {
        if (_runner == null)
            _runner = StartCoroutine(Run());
    }

    void EnsureRunning()
    {
        if (_runner == null) _runner = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (verboseLogs) Debug.Log("[Spawner] Run() start");

        // Wait until at least one prefab exists so we don’t stall forever.
        while (!lightEnemyPrefab && !mediumEnemyPrefab && !heavyEnemyPrefab) yield return null;
        if (!planet) planet = FindObjectOfType<MarchingCubesPlanet>();

        int globalWaveIndex = 0;

        do
        {
            for (int w = 0; w < waves.Count; w++)
            {
                globalWaveIndex++;
                var wave = waves[w];
                OnWaveStarted?.Invoke(globalWaveIndex);

                float gap = (wave.count <= 1 || wave.duration <= 0f) ? 0f : wave.duration / wave.count;

                for (int i = 0; i < wave.count; i++)
                {
                    while (_alive >= maxConcurrent) yield return null;

                    // Pure random: pick a random kind each spawn (uniform among assigned prefabs).
                    EnemyKind kind = PickPureRandomKind();
                    SpawnInternal(kind);

                    if (gap > 0f) yield return new WaitForSeconds(gap);
                    else yield return null;
                }

                if (timeBetweenWaves > 0f)
                {
                    float t = timeBetweenWaves;
                    while (t > 0f)
                    {
                        OnWaveCountdown?.Invoke(w + 2, t);
                        yield return null;
                        t -= Time.deltaTime;
                    }
                }
            }
        } while (loopWaves);

        if (verboseLogs) Debug.Log("[Spawner] Done (no loop).");
        _runner = null;
    }

    // --- Debug helpers ---
    [ContextMenu("Spawn One (Debug Random)")]
    public GameObject SpawnOneDebugRandom() => SpawnInternal(PickPureRandomKind());

    [ContextMenu("Spawn One (Debug Light)")]
    public GameObject SpawnOneDebugLight() => SpawnInternal(EnemyKind.Light);

    [ContextMenu("Spawn One (Debug Medium)")]
    public GameObject SpawnOneDebugMedium() => SpawnInternal(EnemyKind.Medium);

    [ContextMenu("Spawn One (Debug Heavy)")]
    public GameObject SpawnOneDebugHeavy() => SpawnInternal(EnemyKind.Heavy);

    // Pure random among assigned prefabs (uniform).
    EnemyKind PickPureRandomKind()
    {
        var options = new List<EnemyKind>(3);
        if (lightEnemyPrefab)  options.Add(EnemyKind.Light);
        if (mediumEnemyPrefab) options.Add(EnemyKind.Medium);
        if (heavyEnemyPrefab)  options.Add(EnemyKind.Heavy);

        if (options.Count == 0)
        {
            Debug.LogWarning("[Spawner] No enemy prefabs assigned.");
            return EnemyKind.Light; // harmless fallback; caller still guards
        }
        return options[Random.Range(0, options.Count)];
    }

    GameObject SpawnInternal(EnemyKind kind)
    {
        GameObject prefab = GetPrefab(kind);
        if (!prefab)
        {
            Debug.LogWarning($"[Spawner] No prefab set for {kind}. Falling back to Light.");
            prefab = lightEnemyPrefab;
            if (!prefab) return null;
        }

        Vector3 center = globeCenter ? globeCenter.position : Vector3.zero;
        float targetRadius = waterRadius + hoverOffset;

        Vector3 surfacePos;
        Quaternion rot;

        // Use explicit spawn points if provided
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            surfacePos = spawnPoint.position;
            Vector3 up = (surfacePos - center).normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(goalPoleDir, up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(up, Vector3.right).normalized;
            rot = Quaternion.LookRotation(fwd, up);
        }
        else
        {
            // Ring around pole fallback
            Vector3 poleDir = (-goalPoleDir).normalized;

            float ang = Random.Range(0f, spawnRingDegrees) * Mathf.Deg2Rad;
            Vector3 u = Vector3.Cross(poleDir, Vector3.up);
            if (u.sqrMagnitude < 1e-4f) u = Vector3.Cross(poleDir, Vector3.right);
            u.Normalize();
            Vector3 v = Vector3.Cross(poleDir, u);
            float theta = Random.value * Mathf.PI * 2f;
            Vector3 ringOffset = (Mathf.Cos(theta) * u + Mathf.Sin(theta) * v) * Mathf.Sin(ang);
            Vector3 spawnDir = (poleDir * Mathf.Cos(ang) + ringOffset).normalized;

            Vector2 jitter2 = Random.insideUnitCircle * spawnTangentialJitter;
            Vector3 jitter = (u * jitter2.x + v * jitter2.y);

            Vector3 posDir = (spawnDir * targetRadius + jitter - center).normalized;
            surfacePos = center + posDir * targetRadius;

            Vector3 up = (surfacePos - center).normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(goalPoleDir, up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(up, Vector3.right).normalized;
            rot = Quaternion.LookRotation(fwd, up);
        }

        var go = Instantiate(prefab, surfacePos, rot);
        go.SetActive(true);

        // Ensure navigator context is set (in case prefab doesn't already)
        var nav = go.GetComponent<WaterGlobeNavigator>() ?? go.AddComponent<WaterGlobeNavigator>();
        nav.planetCenter = globeCenter;
        nav.waterRadius = waterRadius;
        nav.hoverOffset = hoverOffset;
        nav.goalPoleDir = goalPoleDir;

        LayerMask resolvedMask = ResolveLandMask();
        nav.landMask = resolvedMask;

        nav.planet = planet;
        nav.useHeightFallback = true;
        nav.waterBias = 0f;

        go.AddComponent<SpawnedToken>().Init(this);
        _alive++;
        return go;
    }

    GameObject GetPrefab(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Light:  return lightEnemyPrefab;
            case EnemyKind.Medium: return mediumEnemyPrefab;
            case EnemyKind.Heavy:  return heavyEnemyPrefab;
            default: return lightEnemyPrefab;
        }
    }

    LayerMask ResolveLandMask()
    {
        if (landMask.value != 0) return landMask;
        int landLayer = LayerMask.NameToLayer("Land");
        if (landLayer >= 0) return (LayerMask)(1 << landLayer);
        return Physics.DefaultRaycastLayers;
    }

    void OnChildDestroyed() { _alive = Mathf.Max(0, _alive - 1); }

    sealed class SpawnedToken : MonoBehaviour
    {
        EnemyWaveSpawner owner;
        public void Init(EnemyWaveSpawner o) => owner = o;
        void OnDestroy()
        {
            if (owner)
                owner.SendMessage(nameof(EnemyWaveSpawner.OnChildDestroyed), SendMessageOptions.DontRequireReceiver);
        }
    }
}
