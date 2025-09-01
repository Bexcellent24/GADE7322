using System.Collections.Generic;
using UnityEngine;

public class DefenderSpotGenerator : MonoBehaviour
{
    [Header("Tower Spots")]
    [SerializeField] private GameObject towerSpotPrefab;
    [SerializeField] private Transform planet;       // Sphere or planet centre
    [SerializeField] private float planetRadius = 5f;
    [SerializeField] private int numberOfSpots = 8;
    [SerializeField] private float flattenRadius = 1.5f;

    private Mesh planetMesh;
    private Vector3[] vertices;

    public List<DefenderSpot> towerSpots = new List<DefenderSpot>();

    void Awake()
    {
        planetMesh = planet.GetComponent<MeshFilter>().mesh;
        vertices = planetMesh.vertices;
        
        Debug.Log(planetMesh.vertices.Length);
    }

    private void Start()
    {
        GenerateSpots();
    }

    public void GenerateSpots()
    {
        Debug.Log("Generating spots");

        towerSpots.Clear();

        // Always fetch latest mesh here
        MeshFilter mf = planet.GetComponent<MeshFilter>();
        if (mf == null || mf.mesh == null)
        {
            Debug.LogError("Planet has no mesh at runtime!");
            return;
        }

        planetMesh = mf.mesh;
        vertices = planetMesh.vertices;

        for (int i = 0; i < numberOfSpots; i++)
        {
            Vector3 randomDir = Random.onUnitSphere;
            Vector3 rayStart = planet.position + randomDir * (planetRadius + 2f); // start outside the planet
            Vector3 rayEnd = planet.position; // point at center

            if (Physics.Raycast(rayStart, (rayEnd - rayStart), out RaycastHit hit, planetRadius * 3f))
            {
                Vector3 spotPos = hit.point;

                // Push tower slightly above the surface
                Vector3 normal = (spotPos - planet.position).normalized;
                float towerOffset = 0.1f;
                spotPos += normal * towerOffset;

                FlattenArea(hit, flattenRadius);

                Quaternion spotRot = Quaternion.FromToRotation(Vector3.up, normal);

                GameObject newSpot = Instantiate(towerSpotPrefab, spotPos, spotRot);
                newSpot.transform.SetParent(transform, true); 
                DefenderSpot spotComp = newSpot.GetComponent<DefenderSpot>();
                towerSpots.Add(spotComp);
                
                
            }
        }

        // Commit mesh changes
        planetMesh.vertices = vertices;
        planetMesh.RecalculateNormals();
        planet.GetComponent<MeshCollider>().sharedMesh = planetMesh;
    }


    void FlattenArea(RaycastHit hit, float radius)
    {
        Vector3[] verts = planetMesh.vertices;
        Transform meshTransform = planet.transform;

        Vector3 hitLocal = meshTransform.InverseTransformPoint(hit.point);

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float dist = Vector3.Distance(v, hitLocal);
            if (dist <= radius)
            {
                // Flatten this vertex to be at the same distance from centre as hit point
                Vector3 dir = v.normalized;
                float targetDistance = hitLocal.magnitude;
                verts[i] = dir * targetDistance;
            }
        }

        vertices = verts;
    }
}
