using System.Collections.Generic;
using UnityEngine;

public class DefenderSpotGenerator : MonoBehaviour
{
    [Header("Tower Spots")]
    [SerializeField] private GameObject towerSpotPrefab;
    [SerializeField] private ProceduralTerrain terrain;
    [SerializeField] private int numberOfSpots = 8;
    [SerializeField] private float flattenRadius = 1.5f;

    public List<DefenderSpot> towerSpots;

    public void GenerateSpots()
    {
        towerSpots.Clear();

        int width = terrain.verticesGrid.GetLength(0);
        int depth = terrain.verticesGrid.GetLength(1);

        for (int i = 0; i < numberOfSpots; i++)
        {
            int x = Random.Range(1, width - 1);
            int z = Random.Range(1, depth - 1);

            Vector3 spot = terrain.verticesGrid[x, z];

            // Flatten surrounding vertices
            FlattenArea(x, z, flattenRadius);
            
            GameObject newSpot = Instantiate(towerSpotPrefab, spot, Quaternion.identity, transform);
            DefenderSpot spotComp = newSpot.GetComponent<DefenderSpot>();
            towerSpots.Add(spotComp);
        }
        
    }

    void FlattenArea(int centerX, int centerZ, float radius)
    {
        int width = terrain.verticesGrid.GetLength(0);
        int depth = terrain.verticesGrid.GetLength(1);

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 v = terrain.verticesGrid[x, z];
                if (Vector3.Distance(new Vector3(v.x, 0, v.z), new Vector3(centerX * terrain.scale, 0, centerZ * terrain.scale)) <= radius)
                {
                    // Flatten to center Y
                    v.y = terrain.verticesGrid[centerX, centerZ].y;
                    terrain.verticesGrid[x, z] = v;
                }
            }
        }

        // Update mesh
        Mesh mesh = terrain.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int index = 0;
        for (int z = 0; z < depth; z++)
        for (int x = 0; x < width; x++)
            vertices[index++] = terrain.verticesGrid[x, z];

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        terrain.GetComponent<MeshCollider>().sharedMesh = mesh;
        
        
    }
    
    
}

