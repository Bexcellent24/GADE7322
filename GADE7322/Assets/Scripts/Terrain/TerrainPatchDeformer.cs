using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class TerrainPatchDeformer : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Test")]
    public MeshFilter landMeshFilter; 
    [Tooltip("Test")]
    public MeshCollider landCollider;  
    [Tooltip("Test")]
    public Transform planetCenter;      

    struct Stamp { public Vector3 center; public Vector3 up; public float radius; public float feather; }
    readonly List<Stamp> _stamps = new();

    Mesh _mesh; 
    Vector3[] _baseVerts; 

    void Awake()  { EnsureMeshAndBase(); }
    void OnEnable(){ EnsureMeshAndBase(); }

    void EnsureMeshAndBase()
    {
        if (!landMeshFilter) return;
        _mesh = landMeshFilter.mesh; 
        if (_mesh == null) return;
        if (_baseVerts == null || _baseVerts.Length != _mesh.vertexCount)
            _baseVerts = _mesh.vertices;
    }

    public void ClearStampsAndRebuild()
    {
        _stamps.Clear();
        Rebuild();
    }

    public void AddFlattenStampWorld(Vector3 centerW, Vector3 up, float radius = 0.6f, float feather = 0.4f)
    {
        EnsureMeshAndBase();
        if (_mesh == null || _baseVerts == null) return;

        _stamps.Add(new Stamp {
            center = centerW,
            up = up.normalized,
            radius = Mathf.Max(0.001f, radius),
            feather = Mathf.Clamp01(feather)
        });

        Rebuild();
    }

    void Rebuild()
    {
        if (_mesh == null || _baseVerts == null) return;

        var l2w = landMeshFilter.transform.localToWorldMatrix;
        var w2l = landMeshFilter.transform.worldToLocalMatrix;

        var verts = new Vector3[_baseVerts.Length];

        for (int i = 0; i < _baseVerts.Length; i++)
        {
            Vector3 vw = l2w.MultiplyPoint3x4(_baseVerts[i]); 

            float bestS = 0f;
            Vector3 bestTarget = vw;

            for (int s = 0; s < _stamps.Count; s++)
            {
                var st = _stamps[s];

                // Direction from planet centre to vertex
                Vector3 vertDir = (vw - planetCenter.position).normalized;
                // Direction from planet centre to stamp centre
                Vector3 stampDir = (st.center - planetCenter.position).normalized;

                // Angle between vertex and stamp in radians
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(vertDir, stampDir), -1f, 1f));

                // Convert to "arc length" distance on sphere (angle * radius of stamp)
                float sphereDist = angle * (vw - planetCenter.position).magnitude;

                if (sphereDist > st.radius) continue;

                // Feathering as before, but based on spherical distance
                float inner = st.radius * (1f - st.feather);
                float t = (sphereDist <= inner) ? 1f : 1f - Mathf.InverseLerp(inner, st.radius, sphereDist);
                t = t * t * (3f - 2f * t); // smoothstep

                if (t > bestS)
                {
                    bestS = t;

                    // Flatten toward sphere surface defined by stamp
                    float height = Vector3.Dot(vw - st.center, st.up);
                    bestTarget = vw - height * st.up;
                }
            }

            if (bestS > 0f)
                vw = Vector3.Lerp(vw, bestTarget, bestS);

            verts[i] = w2l.MultiplyPoint3x4(vw);
        }

        _mesh.vertices = verts;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (landCollider)
        {
            landCollider.sharedMesh = null;
            landCollider.sharedMesh = _mesh;
        }
    }
}
