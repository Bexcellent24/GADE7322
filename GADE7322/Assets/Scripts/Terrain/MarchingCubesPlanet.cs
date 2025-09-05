using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesPlanet : MonoBehaviour
{
    [Header("Planet Settings")]
    [Tooltip("Planet's radius")]
    [SerializeField] float radius = 10f;
    [Tooltip("Please don't change")]
    [SerializeField] float isoLevel = 0f; 
    [Tooltip("Adjust Low = smooth, High = Spikey")]
    [SerializeField] float surfaceJitter = 2.0f; 

    [Header("Grid Settings")]
    [Tooltip("Please don't change - higher res = smoother cubes")]
    [SerializeField, Range(8, 128)] int resolution = 48; 
    [Tooltip("Please don't change - Allows for the noise to go beyond the planet's radius bounds")]
    [SerializeField] float boundsPadding = 1.25f;

    [Header("Seeds")]
    [Tooltip("Use the same seed - for testing purposes")]
    [SerializeField] bool useFixedSeed = false;
    [SerializeField] int seed = 12345;
    [Tooltip("Keep on for RNG")]
    [SerializeField] bool autoRerollOnPlay = true;

    [Header("Planet Noise")]
    [Tooltip("Low = Smooth noise, Craggy noise")]
    [SerializeField, Range(1, 8)] int octaves = 4;
    [Tooltip("1 or Lower = Wider Stretched Hills, 1 or above = Pointy islands")]
    [SerializeField] float baseFreq = 0.35f;
    [Tooltip("Do not touch please - Acts as a multiplier for the octaves x base frequency")]
    [SerializeField] float lacunarity = 2.05f;
    [Tooltip("Keep at 0.5 please, otherwise planet won't be whole")]
    [SerializeField] float gain = 0.5f;

    [Header("Continents")]
    [Tooltip("0.5 and lower = few large lands, 0.5 and higher = many small islands")]
    [SerializeField] float continentFreq = 0.06f;
    [Tooltip("Controls the continent noise strength, higher = higher peaks.")]
    [SerializeField] float continentAmp  = 2.2f; 

    [Header("Domain Warp")]
    [Tooltip("Secondary noise used to distort the main noise.")]
    [SerializeField] float warpFreq = 0.18f;
    [Tooltip("Adjust at your own risk")]
    [SerializeField] float warpStrength = 1.75f;

    [Header("Elevation Shaping")]
    [Tooltip("Graph that equalises terrain heights")]
    [SerializeField] AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Clamps the ocean floor level")]
    [SerializeField] float seaLevel = 0.0f;
 
    
    [Header("Nav (slope)")]
    [SerializeField] Transform planetCenterOverride; 
    
    //Gets the globeCenter
    public Vector3 PlanetCenter => planetCenterOverride ? planetCenterOverride.position : transform.position;

    //Render Mesh
    MeshFilter mf;
    
    //Mesh instance built at runtime
    Mesh mesh;
    
    //3D offsets, de-correlates noise layers so they don't line up
    Vector3 offMain, offWarpA, offWarpB, offContinents;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        
        //If changes are made in the editor while running makes sure the planet does auto regenerate
        mesh = mf.sharedMesh ? mf.sharedMesh : (mf.sharedMesh = new Mesh { name = "Planet" });
        
        InitSeed();
        Regenerate();
    }
    void InitSeed()
    {
        if (!useFixedSeed && autoRerollOnPlay)
            //Use a min and max value to avoid reusing the same planet
            seed = Random.Range(int.MinValue / 2, int.MaxValue / 2);

        //Deterministic RNG from the seed value provided above ^^^
        var r = new System.Random(seed);
        
        //The four 3D offsets fed to the noise sampler [DO NOT CHANGE THE ORDER]
        offMain       = RandVec(r) * 1000f;
        offWarpA      = RandVec(r) * 1000f;
        offWarpB      = RandVec(r) * 1000f;
        offContinents = RandVec(r) * 1000f;
    }
    //Makes a 3D vector (between 0-1) out of the seed generated
    static Vector3 RandVec(System.Random r)
        => new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

    
    void Regenerate()
    {
        Build();
        mf.sharedMesh = mesh;
        var mc = GetComponent<MeshCollider>();
        if (mc) { mc.sharedMesh = null; mc.sharedMesh = mesh; }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh == null) mf.sharedMesh = new Mesh { name = "Planet (MarchingCubes)" };
        if (Application.isPlaying == false) { InitSeed(); Regenerate(); }
    }
#endif
   void Build()
{
    if (mesh == null) mesh = new Mesh { name = "Planet (MarchingCubes)" };
    mesh.Clear();

    float half = radius * boundsPadding;
    int N = Mathf.Max(2, resolution);
    float cell = (half * 2f) / N;

    var verts = new List<Vector3>(N * N * N * 15);
    var tris  = new List<int>   (N * N * N * 15);

    for (int z = 0; z < N; z++)
    for (int y = 0; y < N; y++)
    for (int x = 0; x < N; x++)
    {
        Vector3[] p = new Vector3[8];
        p[0] = new Vector3(x,     y,     z    );
        p[1] = new Vector3(x + 1, y,     z    );
        p[2] = new Vector3(x + 1, y,     z + 1);
        p[3] = new Vector3(x,     y,     z + 1);
        p[4] = new Vector3(x,     y + 1, z    );
        p[5] = new Vector3(x + 1, y + 1, z    );
        p[6] = new Vector3(x + 1, y + 1, z + 1);
        p[7] = new Vector3(x,     y + 1, z + 1);

        for (int i = 0; i < 8; i++)
        {
            p[i] = (p[i] / N * (half * 2f) - new Vector3(half, half, half));
        }

        Vector3[] wp = new Vector3[8];
        for (int i = 0; i < 8; i++) wp[i] = transform.TransformPoint(p[i]);

        float[] d = new float[8];
        for (int i = 0; i < 8; i++) d[i] = SampleDensity(wp[i]);

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            float localIso = isoLevel + TinyHash(wp[i], cell);
            if (d[i] < localIso) cubeIndex |= (1 << i);
        }

        int edgeFlags = Tables.EdgeTable[cubeIndex];
        if (edgeFlags == 0) continue;

        Vector3[] ev = new Vector3[12];
        for (int e = 0; e < 12; e++)
        {
            if ((edgeFlags & (1 << e)) == 0) continue;

            int a = Tables.EdgeIndexTable[e, 0];
            int b = Tables.EdgeIndexTable[e, 1];

            Vector3 v = InterpVertex(wp[a], wp[b], d[a], d[b], isoLevel);
            ev[e] = transform.InverseTransformPoint(v);
        }

        for (int t = 0; t < 16; t += 3)
        {
            int ei0 = Tables.TriTable[cubeIndex, t];
            if (ei0 == -1) break;
            int ei1 = Tables.TriTable[cubeIndex, t + 1];
            int ei2 = Tables.TriTable[cubeIndex, t + 2];

            int i0 = verts.Count; verts.Add(ev[ei0]);
            int i1 = verts.Count; verts.Add(ev[ei1]);
            int i2 = verts.Count; verts.Add(ev[ei2]);

            tris.Add(i0); tris.Add(i1); tris.Add(i2);
        }
    }

    mesh.SetVertices(verts);
    mesh.SetTriangles(tris, 0, true);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
}

static Vector3 InterpVertex(Vector3 pA, Vector3 pB, float dA, float dB, float iso)
{
    float denom = (dB - dA);
    if (Mathf.Abs(denom) < 1e-6f) return (pA + pB) * 0.5f;

    float t = Mathf.Clamp01((iso - dA) / denom);
    return pA + (pB - pA) * t;
}

    float SampleDensity(Vector3 worldPos)
    {
        float sdfSphere = worldPos.magnitude - radius;

        float cont = Perlin3D(worldPos * continentFreq + offContinents);
        cont = cont * 0.5f + 0.5f;
        cont = heightCurve.Evaluate(cont);
        float continentalLift = (cont - 0.5f) * continentAmp;

        Vector3 w = worldPos * warpFreq + offWarpA;
        float wx = Perlin3D(w + new Vector3(37, 0, 0));
        float wy = Perlin3D(w + new Vector3(0, 73, 0));
        float wz = Perlin3D(w + new Vector3(0, 0, 131));
        Vector3 warpedPos = worldPos + new Vector3(wx, wy, wz) * warpStrength;

        float hills = fBm(warpedPos * baseFreq + offMain, octaves, lacunarity, gain);

        float ridged = 1f - Mathf.Abs(hills * 2f - 1f);
        float detail = Mathf.Lerp(hills, ridged, 0.35f);

        float displacement = surfaceJitter * detail + continentalLift - seaLevel;

        return sdfSphere + displacement;
    }

    float TinyHash(Vector3 pos, float scale)
    {
        float h = Mathf.Sin(Vector3.Dot(pos, new Vector3(12.9898f, 78.233f, 37.719f)) * 43758.5453f);
        return (h * 0.5f + 0.5f) * (scale * 0.0025f);
    }
    
    float fBm(Vector3 p, int octs, float lac, float g)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int i = 0; i < octs; i++)
        {
            sum  += amp * Perlin3D(p * freq);
            norm += amp;
            amp  *= g;
            freq *= lac;
        }
        return (sum / Mathf.Max(0.0001f, norm));
    }

    float Perlin3D(Vector3 p)
    {
        float xy = Mathf.PerlinNoise(p.x + offMain.x, p.y + offMain.y);
        float yz = Mathf.PerlinNoise(p.y + 37.1f + offMain.y, p.z + 53.2f + offMain.z);
        float zx = Mathf.PerlinNoise(p.z + 91.7f + offMain.z, p.x + 11.8f + offMain.x);
        return (xy + yz + zx) / 3f; // ~[0,1]
    }

    float DisplacementAt(Vector3 worldPos)
    {
        float cont = Perlin3D(worldPos * continentFreq + offContinents);
        cont = cont * 0.5f + 0.5f;
        cont = heightCurve.Evaluate(cont);
        float continentalLift = (cont - 0.5f) * continentAmp;

        Vector3 w = worldPos * warpFreq + offWarpA;
        float wx = Perlin3D(w + new Vector3(37, 0, 0));
        float wy = Perlin3D(w + new Vector3(0, 73, 0));
        float wz = Perlin3D(w + new Vector3(0, 0, 131));
        Vector3 warpedPos = worldPos + new Vector3(wx, wy, wz) * warpStrength;

        float hills  = fBm(warpedPos * baseFreq + offMain, octaves, lacunarity, gain);
        float ridged = 1f - Mathf.Abs(hills * 2f - 1f);
        float detail = Mathf.Lerp(hills, ridged, 0.35f);

        return surfaceJitter * detail + continentalLift - seaLevel;
    }
    public bool IsWaterDirection(Vector3 dir, float waterBias = 0f)
    {
        dir = dir.normalized;
        float disp = DisplacementAt(dir * radius);
        return (disp + waterBias) < 0f; 
    }

    public Vector3 SurfacePointFromDirection(Vector3 dir)
    {
        dir = dir.normalized;
        float disp = DisplacementAt(dir * radius);
        float surfaceR = radius - disp;
        return transform.TransformPoint(dir * surfaceR);
    }
    
    public IEnumerator GenerateCoroutine()
    {
        InitSeed();
        Regenerate();
        yield return null;
    }
  
}
