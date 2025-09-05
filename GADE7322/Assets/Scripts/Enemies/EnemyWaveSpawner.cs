using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(50)]
public class EnemyWaveSpawner : MonoBehaviour
{
    [System.Serializable] public struct Wave { public int count; public float duration; }

    [Header("Refs")]
    public GameObject enemyPrefab;

    [Header("Water Globe")]
    public Transform globeCenter;
    public float waterRadius = 10f;
    public float hoverOffset = 0f;
    public Vector3 goalPoleDir = Vector3.down;
    public LayerMask landMask;

    [Header("Planet (optional height fallback)")]
    public MarchingCubesPlanet planet;

    [Header("Spawn Ring (around top pole)")]
    [Tooltip("Degrees away from the spawn pole (-goalPoleDir). 0 = exact pole.")]
    [Range(0f, 45f)] public float spawnRingDegrees = 8f;
    [Tooltip("Meters of tangent jitter on the surface.")]
    public float spawnTangentialJitter = 0.75f;

    [Header("Waves")]
    public List<Wave> waves = new() { new Wave { count = 10, duration = 5f } };
    public float timeBetweenWaves = 5f;
    public bool loopWaves = false;
    public int maxConcurrent = 60;

    [Header("Run")]
    public bool autoStart = true;
    public bool verboseLogs = true;

    int _alive;
    Coroutine _runner;

    void OnEnable()
    {
        if (autoStart) EnsureRunning();
    }

    void Start()
    {
        if (autoStart) EnsureRunning();
    }

    void EnsureRunning()
    {
        if (_runner == null) _runner = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (verboseLogs) Debug.Log("[Spawner] Run() start");

        while (enemyPrefab == null)
        {
            if (verboseLogs) Debug.Log("[Spawner] Waiting for enemyPrefab…");
            yield return null;
        }

        if (!planet) planet = FindObjectOfType<MarchingCubesPlanet>();

        if (waves == null || waves.Count == 0)
        {
            if (verboseLogs) Debug.LogWarning("[Spawner] No waves configured.");
            yield break;
        }

        do
        {
            for (int w = 0; w < waves.Count; w++)
            {
                var wave = waves[w];
                if (verboseLogs) Debug.Log($"[Spawner] Wave {w + 1}/{waves.Count}: {wave.count} in {wave.duration}s");

                float gap = (wave.count <= 1 || wave.duration <= 0f) ? 0f : wave.duration / wave.count;

                for (int i = 0; i < wave.count; i++)
                {
                    while (_alive >= maxConcurrent) yield return null;
                    SpawnInternal();

                    if (gap > 0f) yield return new WaitForSeconds(gap);
                    else yield return null;
                }

                if (timeBetweenWaves > 0f) yield return new WaitForSeconds(timeBetweenWaves);
            }
        } while (loopWaves);

        if (verboseLogs) Debug.Log("[Spawner] Done (no loop).");
        _runner = null;
    }

    [ContextMenu("Spawn One (Debug)")]
    public GameObject SpawnOneDebug() => SpawnInternal();

    GameObject SpawnInternal()
    {
        if (!enemyPrefab) return null;

        Vector3 center = globeCenter ? globeCenter.position : Vector3.zero;
        float targetRadius = waterRadius + hoverOffset;

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
        Vector3 surfacePos = center + posDir * targetRadius;

        Vector3 up = (surfacePos - center).normalized;
        Vector3 fwd = Vector3.ProjectOnPlane(goalPoleDir, up).normalized;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(up, Vector3.right).normalized;
        Quaternion rot = Quaternion.LookRotation(fwd, up);

        var go = Instantiate(enemyPrefab, surfacePos, rot);
        go.SetActive(true);
        if (!go.GetComponent<Collider>()) go.AddComponent<SphereCollider>();

        var nav = go.GetComponent<WaterGlobeNavigator>();
        if (!nav) nav = go.AddComponent<WaterGlobeNavigator>();

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
