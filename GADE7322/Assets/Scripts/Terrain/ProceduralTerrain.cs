using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private int width = 20;       // number of vertices along X
    [SerializeField] private int depth = 20;       // number of vertices along Z
    [SerializeField] public float scale = 1f;     // spacing between vertices
    [SerializeField] private float heightMultiplier = 5f;  // max terrain height

    [Header("Perlin Noise")]
    [Range(0, 1)]
    [SerializeField] private float noiseScale1 = 0.2f;
    [Range(0, 1)]
    [SerializeField] private float noiseScale2 = 0.5f;
    [Range(0, 1)]
    [SerializeField] private float noiseWeight2 = 0.3f;    // weight of second noise layer

    [Header("Ground Material")]
    [SerializeField] private Material groundMaterial;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    public Vector3[,] verticesGrid; // store vertices for tower placement

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[width * depth];
        Vector2[] uv = new Vector2[width * depth];
        int[] triangles = new int[(width - 1) * (depth - 1) * 6];

        verticesGrid = new Vector3[width, depth];

        // Generate vertices with Perlin noise
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float y = Mathf.PerlinNoise(x * noiseScale1, z * noiseScale1);
                y += Mathf.PerlinNoise(x * noiseScale2, z * noiseScale2) * noiseWeight2;
                y *= heightMultiplier;

                vertices[z * width + x] = new Vector3(x * scale, y, z * scale);
                uv[z * width + x] = new Vector2((float)x / width, (float)z / depth);
                verticesGrid[x, z] = vertices[z * width + x];
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int z = 0; z < depth - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int topLeft = z * width + x;
                int bottomLeft = (z + 1) * width + x;
                int topRight = topLeft + 1;
                int bottomRight = bottomLeft + 1;

                // First triangle
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;

                // Second triangle
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.material = groundMaterial;
        
        GetComponent<TowerSpotGenerator>()?.GenerateSpots();
    }
}
