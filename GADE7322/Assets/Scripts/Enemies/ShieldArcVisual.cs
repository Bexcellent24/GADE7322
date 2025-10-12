// ShieldArcVisual.cs
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ShieldArcVisual : MonoBehaviour
{
    [Header("Shape")]
    [Range(10f, 360f)] public float arcDegrees = 180f;
    public float innerRadius = 0.1f;
    public float outerRadius = 6f;
    [Range(3, 128)] public int radialSegments = 48;

    [Header("Orientation")]
    public bool faceOwnerForward = true;
    public Transform owner; // optional; if set, forward defines arc forward

    Mesh _mesh;

    void OnValidate() { Build(); }
    void Awake() { Build(); }

    void LateUpdate()
    {
        if (faceOwnerForward && owner)
        {
            // Keep arc centered at owner and facing forward on XZ
            transform.position = owner.position;
            Vector3 fwd = Vector3.ProjectOnPlane(owner.forward, Vector3.up);
            if (fwd.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }

    public void Build()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "ShieldArcMesh" };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        int segs = Mathf.Max(3, radialSegments);
        int vertsPerRing = segs + 1;

        Vector3[] v = new Vector3[vertsPerRing * 2];
        Vector2[] uv = new Vector2[v.Length];
        int[] tris = new int[segs * 6];

        float start = -arcDegrees * 0.5f;
        float step = arcDegrees / segs;

        for (int i = 0; i <= segs; i++)
        {
            float a = start + i * step;
            Quaternion rot = Quaternion.AngleAxis(a, Vector3.up);
            Vector3 dir = rot * Vector3.forward;

            v[i] = dir * innerRadius;                   // inner ring
            v[i + vertsPerRing] = dir * outerRadius;    // outer ring

            uv[i] = new Vector2(i / (float)segs, 0f);
            uv[i + vertsPerRing] = new Vector2(i / (float)segs, 1f);
        }

        int t = 0;
        for (int i = 0; i < segs; i++)
        {
            int i0 = i;
            int i1 = i + 1;
            int o0 = i + vertsPerRing;
            int o1 = i + 1 + vertsPerRing;

            // Quad (two triangles): (i0,o0,o1) (i0,o1,i1)
            tris[t++] = i0; tris[t++] = o0; tris[t++] = o1;
            tris[t++] = i0; tris[t++] = o1; tris[t++] = i1;
        }

        _mesh.Clear();
        _mesh.vertices = v;
        _mesh.uv = uv;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}
