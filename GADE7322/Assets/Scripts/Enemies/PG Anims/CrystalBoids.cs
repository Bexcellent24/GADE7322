using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CrystalBoidsFlocking : MonoBehaviour
{
    [Header("Render")]
    public Mesh shardMesh;
    public Material shardMaterial;
    [Range(1, 1000)] public int count = 120;
    [Min(0.02f)] public float shardScale = 0.12f;
    public bool hideShardGameObjects = true;

    [Header("Flocking (Reynolds)")]
    [Tooltip("Neighbour radius used for alignment/cohesion.")]
    [Min(0.05f)] public float neighborRadius = 1.1f;
    [Tooltip("Personal space radius used for separation.")]
    [Min(0.02f)] public float separationRadius = 0.8f;

    [Tooltip("Weight of separation force.")]
    public float separationWeight = 2.8f;
    [Tooltip("Weight of alignment force.")]
    public float alignmentWeight = 0.6f;
    [Tooltip("Weight of cohesion force.")]
    public float cohesionWeight = 0.5f;

    [Header("Speed Limits")]
    [Tooltip("Maximum speed ")]
    public float maxSpeed = 2.2f;
    [Tooltip("Maximum steering acceleration")]
    public float maxAccel = 8f;

    [Header("Anchor")]
    [Tooltip("Preferred distance from center")]
    public float anchorRadius = 1.1f;
    [Tooltip("How strongly boids are nudged toward/away from the anchor radius")]
    public float anchorStrength = 1.2f;

    [Header("Stability / Flow")]
    [Tooltip("Random drift to reduce lock-ups")]
    [Range(0f, 1f)] public float jitter = 0.3f;
    [Tooltip("Small damping to stabilise jitter")]
    [Range(0f, 1f)] public float velocityDamping = 0.005f;
    [Tooltip("Bias toward tangential motion around sphere (0..1)")]
    [Range(0f, 1f)] public float tangentialBias = 0.25f;

    [Header("Soft Bounds")]
    [Tooltip("0 disables soft bounds.")]
    public float softBoundsRadius = 0f;
    public float softBoundsStrength = 1.5f;

    struct Shard { public Transform tr; public Vector3 pos, vel; public float seed; }

    readonly List<Shard> _shards = new();
    Material _runtimeMat;
    bool _built;

#if UNITY_EDITOR
    bool _needsRebuild;
    void OnValidate()
    {
        neighborRadius    = Mathf.Max(0.05f, neighborRadius);
        separationRadius  = Mathf.Clamp(separationRadius, 0.02f, neighborRadius);
        maxSpeed          = Mathf.Max(0.01f, maxSpeed);
        maxAccel          = Mathf.Max(0.01f, maxAccel);
        count             = Mathf.Max(1, count);

        if (!Application.isPlaying)
        {
            _needsRebuild = true;
            UnityEditor.EditorApplication.delayCall += DeferredRebuild;
        }
    }
    void DeferredRebuild()
    {
        UnityEditor.EditorApplication.delayCall -= DeferredRebuild;
        if (!this) return;
        if (_needsRebuild) { _needsRebuild = false; Rebuild(); }
    }
#endif

    void Awake()    => Rebuild();
    void OnEnable() => Rebuild();
    void OnDisable() { ClearChildren(); _shards.Clear(); _built = false; }
    void OnDestroy() { ClearChildren(); _shards.Clear(); _built = false; }

    void Rebuild()
    {
        if (shardMesh == null || shardMaterial == null)
        {
            if (shardMesh == null) Debug.LogError("[CrystalBoidsFlocking] Assign shardMesh.");
            if (shardMaterial == null) Debug.LogError("[CrystalBoidsFlocking] Assign shardMaterial.");
            enabled = false; return;
        }

        if (_runtimeMat == null)
        {
            _runtimeMat = new Material(shardMaterial);
            _runtimeMat.enableInstancing = true;
        }

        ClearChildren();
        _shards.Clear();

        var rand = new System.Random(12345);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"boid_{i:0000}");
            go.transform.SetParent(transform, false);
            go.isStatic = false;

            if (hideShardGameObjects)
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var mf = go.AddComponent<MeshFilter>();   mf.sharedMesh = shardMesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = _runtimeMat;

            Vector3 dir = Random.onUnitSphere;
            float r = anchorRadius * Mathf.Lerp(0.7f, 1.3f, (float)rand.NextDouble());
            Vector3 pos = dir * r;
            Vector3 vel = Vector3.Cross(dir, Vector3.up);
            if (vel.sqrMagnitude < 1e-4f) vel = Vector3.Cross(dir, Vector3.right);
            vel = vel.normalized * (maxSpeed * Mathf.Lerp(0.2f, 0.8f, (float)rand.NextDouble()));

            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
            go.transform.localScale = Vector3.one * shardScale;

            _shards.Add(new Shard { tr = go.transform, pos = pos, vel = vel, seed = (float)rand.NextDouble() * 1000f });
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

    void Update()
    {
        if (!_built) return;

        float dt = Mathf.Max(0.0005f, Time.deltaTime);
        float neighR2 = neighborRadius * neighborRadius;
        float sepR2   = separationRadius * separationRadius;

        // O(n^2) 
        for (int i = 0; i < _shards.Count; i++)
        {
            var a = _shards[i];

            Vector3 sumPos = Vector3.zero;
            Vector3 sumVel = Vector3.zero;
            Vector3 separation = Vector3.zero;

            int neighborCount = 0;
            int sepCount = 0;

            for (int j = 0; j < _shards.Count; j++)
            {
                if (i == j) continue;

                Vector3 delta = _shards[j].pos - a.pos;
                float d2 = delta.sqrMagnitude;

                if (d2 < neighR2)
                {
                    neighborCount++;
                    sumPos += _shards[j].pos;
                    sumVel += _shards[j].vel;

                    if (d2 < sepR2 && d2 > 0f)
                    {
                        // Inverse-square push away, clamped to avoid numeric spikes
                        float inv = 1f / Mathf.Max(0.0002f, d2);
                        Vector3 dir = delta * (-inv);
                        separation += Vector3.ClampMagnitude(dir, 5f);
                        sepCount++;
                    }
                }
            }

            Vector3 accel = Vector3.zero;

            // Separation 
            if (sepCount > 0)
            {
                float crowd = Mathf.InverseLerp(2f, 10f, neighborCount);
                float sepBoost = Mathf.Lerp(1f, 1.8f, crowd);
                Vector3 sepDir = separation.normalized;
                accel += sepDir * (separationWeight * sepBoost * maxAccel);
            }

            // Alignment
            if (neighborCount > 0)
            {
                Vector3 avgVel = sumVel / neighborCount;
                Vector3 align = (avgVel.normalized * maxSpeed) - a.vel;
                align = Vector3.ClampMagnitude(align, maxAccel * 0.5f);
                accel += align * alignmentWeight;
            }

            // Cohesion 
            if (neighborCount > 0)
            {
                Vector3 center = sumPos / neighborCount;
                Vector3 toCenter = center - a.pos;
                toCenter = Vector3.ClampMagnitude(toCenter, maxAccel * 0.4f);
                accel += toCenter * cohesionWeight;
            }

            float r = a.pos.magnitude;
            if (r > 0.0001f)
            {
                float radialErr = (anchorRadius - r); 
                Vector3 radialDir = a.pos.normalized;
                accel += radialDir * (radialErr * anchorStrength);
            }

            if (softBoundsRadius > 0f)
            {
                float rr = a.pos.magnitude;
                if (rr > softBoundsRadius)
                {
                    Vector3 back = (-a.pos).normalized * softBoundsStrength;
                    accel += back;
                }
            }

            if (jitter > 0f)
            {
                float t = Time.time;
                Vector3 n = new Vector3(
                    Mathf.PerlinNoise(a.seed, t * 0.7f) - 0.5f,
                    Mathf.PerlinNoise(a.seed + 13.1f, t * 0.8f) - 0.5f,
                    Mathf.PerlinNoise(a.seed + 37.7f, t * 0.6f) - 0.5f
                );
                if (n.sqrMagnitude > 1e-6f)
                {
                    n.Normalize();
                    accel += n * (jitter * maxAccel * 0.5f);
                }
            }
            
            if (accel.sqrMagnitude > maxAccel * maxAccel)
                accel = accel.normalized * maxAccel;

            a.vel += accel * dt;
            
            if (tangentialBias > 0f && a.pos.sqrMagnitude > 1e-6f)
            {
                Vector3 radial = a.pos.normalized;
                Vector3 radialComp = Vector3.Project(a.vel, radial);
                a.vel -= radialComp * tangentialBias;
            }

            // Damping + clamp speed
            a.vel *= (1f - velocityDamping);
            float speed = a.vel.magnitude;
            if (speed > maxSpeed) a.vel = a.vel * (maxSpeed / Mathf.Max(0.0001f, speed));

            a.pos += a.vel * dt;

            // Write back + apply to transform
            _shards[i] = a;
            a.tr.localPosition = a.pos;
            if (a.vel.sqrMagnitude > 1e-6f)
                a.tr.localRotation = Quaternion.LookRotation(a.vel.normalized, Vector3.up);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, anchorRadius);
        if (softBoundsRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, softBoundsRadius);
        }
    }
#endif
}
