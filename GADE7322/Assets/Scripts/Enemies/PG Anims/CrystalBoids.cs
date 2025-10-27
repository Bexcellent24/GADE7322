using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CrystalBoids : MonoBehaviour
{
    [Header("Render")]
    [Tooltip("Mesh used for each shard")]
    public Mesh shardMesh;
    [Tooltip(" Material used for each shard")]
    public Material shardMaterial;

    [Header("Flock")]
    [Tooltip("Number of shards")]
    [Range(1, 500)] public int count = 40;
    [Tooltip("Uniform scale applied to each shard")]
    [Min(0.05f)] public float shardScale = 0.12f;

    [Header("Orbit Motion")]
    [Tooltip("Average distance from the crystal center")]
    public float radius = 0.9f;
    [Tooltip("Random variation added to each shard’s radius")]
    public float radiusJitter = 0.25f;

    [Tooltip("Base angular speed of shards")]
    public float angularSpeedDeg = 90f;
    [Tooltip("Per-shard random speed multiplier range")]
    public Vector2 speedRange = new Vector2(0.6f, 1.4f);

    [Tooltip("Up/down bob amplitude")]
    public float bobAmplitude = 0.10f;
    [Tooltip("How fast the bobbing happens")]
    public float bobSpeed = 1.5f;
    [Tooltip("Small random wobble applied to the orbit axis over time")]
    public float axisWobble = 0.15f;
    
    public bool hideShardGameObjects = true;

    struct Shard
    {
        public Transform tr;
        public Vector3 orbitAxis;
        public float radius;
        public float angleDeg;
        public float speedMul;
        public float seed;
    }

    readonly List<Shard> _shards = new List<Shard>();
    Material _runtimeMat;
    System.Random _rng;
    bool _built;
    bool _valid;  

    void Awake()    => BuildIfNeeded();
    void OnEnable() => BuildIfNeeded();

#if UNITY_EDITOR
    void OnValidate()
    {
        count = Mathf.Max(1, count);
        speedRange.x = Mathf.Min(speedRange.x, speedRange.y);

        // If anything changed in edit mode, mark unbuilt and try to rebuild
        if (!Application.isPlaying) _built = false;
        BuildIfNeeded();
        ResizeIfNeeded();
        ApplyHideFlagsToChildren();
    }
#endif

    void OnDisable()
    {
        ClearChildren();
        _shards.Clear();
        _built = false;
    }

    void OnDestroy()
    {
        ClearChildren();
        _shards.Clear();
        _built = false;
    }

    // requires shard Mesh and shard Material to be assigned
    void BuildIfNeeded()
    {
        if (_built) return;

        // Validate required assets
        _valid = (shardMesh != null && shardMaterial != null);
        if (!_valid)
        {
            if (shardMesh == null)
                Debug.LogError($"[{nameof(CrystalBoids)}] Missing required Mesh on '{name}'. Please assign 'shardMesh' in the Inspector.");
            if (shardMaterial == null)
                Debug.LogError($"[{nameof(CrystalBoids)}] Missing required Material on '{name}'. Please assign 'shardMaterial' in the Inspector.");
            enabled = false; 
            return;
        }

        _rng = new System.Random(12345);

        // Make a runtime instance of the provided material
        if (_runtimeMat == null)
        {
            _runtimeMat = new Material(shardMaterial);
            _runtimeMat.enableInstancing = true;
        }

        ClearChildren();
        _shards.Clear();

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"shard_{i:0000}");
            go.transform.SetParent(transform, false);
            go.isStatic = false;

            if (hideShardGameObjects)
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var mf = go.AddComponent<MeshFilter>();    mf.sharedMesh = shardMesh;
            var mr = go.AddComponent<MeshRenderer>();  mr.sharedMaterial = _runtimeMat;

            var sh = new Shard
            {
                tr        = go.transform,
                orbitAxis = UnityEngine.Random.onUnitSphere.normalized,
                angleDeg  = UnityEngine.Random.Range(0f, 360f),
                radius    = Mathf.Max(0.05f, radius + UnityEngine.Random.Range(-radiusJitter, radiusJitter)),
                speedMul  = UnityEngine.Random.Range(speedRange.x, speedRange.y),
                seed      = (float)_rng.NextDouble() * 1000f
            };

            sh.tr.localScale = Vector3.one * shardScale;
            _shards.Add(sh);
        }

        _built = true;
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
        #if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
        #else
            Destroy(child);
        #endif
        }
    }

    void ResizeIfNeeded()
    {
        if (!_built) return;

        // Add
        while (_shards.Count < count)
        {
            _built = false; BuildIfNeeded(); return;
        }

        // Remove
        while (_shards.Count > count)
        {
            var last = _shards[_shards.Count - 1];
        #if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(last.tr.gameObject);
            else Destroy(last.tr.gameObject);
        #else
            Destroy(last.tr.gameObject);
        #endif
            _shards.RemoveAt(_shards.Count - 1);
        }
    }

    void ApplyHideFlagsToChildren()
    {
        var flags = hideShardGameObjects
            ? (HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild)
            : HideFlags.None;

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.hideFlags = flags;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.RepaintHierarchyWindow();
#endif
    }

    void Update()
    {
        if (!_built) BuildIfNeeded();
        if (!_valid) return;

        ResizeIfNeeded();

        float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
        if (dt <= 1e-6f) dt = Time.unscaledDeltaTime;

        float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

        for (int i = 0; i < _shards.Count; i++)
        {
            var sh = _shards[i];

            // Perlin wobble makes the orbit axis drift slightly
            Vector3 wobble = new Vector3(
                (Mathf.PerlinNoise(t * 0.4f, sh.seed) - 0.5f) * 2f,
                (Mathf.PerlinNoise(t * 0.5f, sh.seed + 33.3f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(t * 0.6f, sh.seed + 77.7f) - 0.5f) * 2f
            ) * axisWobble;

            Vector3 axis = (sh.orbitAxis + wobble).normalized;
            float w = angularSpeedDeg * sh.speedMul;

            sh.angleDeg += w * dt;

            // Basis perpendicular to axis
            Vector3 basis = Vector3.Cross(axis, Vector3.up);
            if (basis.sqrMagnitude < 1e-6f) basis = Vector3.Cross(axis, Vector3.right);
            basis.Normalize();

            // Position on ring + bob along axis
            Quaternion q = Quaternion.AngleAxis(sh.angleDeg, axis);
            Vector3 onRing = q * basis * sh.radius;
            float bob = Mathf.Sin((t + sh.seed) * bobSpeed) * bobAmplitude;
            Vector3 pos = onRing + axis * bob;

            sh.tr.localPosition = pos;
            sh.tr.localRotation = Quaternion.LookRotation(Vector3.Slerp(basis, axis, 0.3f), axis);

            _shards[i] = sh;
        }
    }
}
