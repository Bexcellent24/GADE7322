using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(40)]
public class PolarCapDeformer : MonoBehaviour
{
    [Header("Targets")]
    public MeshFilter planetMeshFilter;
    public MeshCollider planetCollider;
    public Transform planetCenter;

    [Header("Polar Settings")]
    [Range(0f, 0.5f)] public float capSize = 0.15f;
    public float polarStrength = 1f;
    public float maxDisplacement = 4f;

    Mesh _mesh;
    Vector3[] _baseVerts;

    void Awake() { EnsureMesh(); }
    void OnEnable() { EnsureMesh(); }

    void EnsureMesh()
    {
        if (!planetMeshFilter) return;
        _mesh = planetMeshFilter.mesh;
        if (_mesh == null) return;
        if (_baseVerts == null || _baseVerts.Length != _mesh.vertexCount)
            _baseVerts = _mesh.vertices;
    }

    public IEnumerator ApplyPolarCaps()
    {
        EnsureMesh();
        if (_mesh == null || _baseVerts == null) yield break;

        var verts = new Vector3[_baseVerts.Length];
        Vector3 center = planetCenter ? planetCenter.position : Vector3.zero;

        
        int chunkSize = 500; 
        for (int i = 0; i < _baseVerts.Length; i++)
        {
            Vector3 worldPos = planetMeshFilter.transform.TransformPoint(_baseVerts[i]);
            Vector3 dir = (worldPos - center).normalized;
            float lat = Mathf.Abs(dir.y); // y = up axis

            if (lat > 1f - capSize) 
            {
                float t = (lat - (1f - capSize)) / capSize; 
                t = Mathf.SmoothStep(0f, 1f, t);

                Vector3 displacement = -dir * polarStrength * t;
                if (displacement.magnitude > maxDisplacement)
                    displacement = displacement.normalized * maxDisplacement;

                worldPos += displacement;
            }

            verts[i] = planetMeshFilter.transform.InverseTransformPoint(worldPos);

            
            if (i % chunkSize == 0)
                yield return null;
        }

        _mesh.vertices = verts;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (planetCollider)
        {
            planetCollider.sharedMesh = null;
            planetCollider.sharedMesh = _mesh;
        }
    }
}
