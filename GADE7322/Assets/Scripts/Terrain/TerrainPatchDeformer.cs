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

                Vector3 to = vw - st.center;
                Vector3 tan = Vector3.ProjectOnPlane(to, st.up);
                float dist = tan.magnitude;
                if (dist > st.radius) continue;

                float inner = st.radius * (1f - st.feather);
                float t = (dist <= inner) ? 1f : 1f - Mathf.InverseLerp(inner, st.radius, dist);
                t = t * t * (3f - 2f * t); // smoothstep

                if (t > bestS)
                {
                    bestS = t;
                    bestTarget = st.center + tan; // point on the stamp's tangent plane
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
