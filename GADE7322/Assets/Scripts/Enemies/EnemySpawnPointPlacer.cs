using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class EnemySpawnPointPlacer : MonoBehaviour
{
    [Header("Planet / Water")]
    public Transform planetCenter;
    public float waterRadius = 10f;
    public LayerMask terrainMask;

    [Header("Spawn Settings")]
    public int maxSpawnPoints = 3;
    public GameObject spawnPrefab;
    public float lift = 0.05f;
    [Range(0f, 1f)] public float southBias = 0.7f; 
    public float minSpacing = 2f; // minimum spacing between spawn points
    public int sampleCount = 800; // more means more accurate but slooower

    [HideInInspector] public List<Transform> spawnPoints = new();

    Vector3 Center => planetCenter ? planetCenter.position : Vector3.zero;

    [ContextMenu("Generate Spawn Points")]
    public void GenerateSpawnPoints()
    {
        // Clear old points
        foreach (var s in spawnPoints)
            if (s) DestroyImmediate(s.gameObject);
        spawnPoints.Clear();

        List<(Vector3 pos, float depth, Vector3 dir)> candidates = new();

        // Sample random directions near the south pole
        for (int i = 0; i < sampleCount; i++)
        {
            Vector3 dir = (Vector3.down + Random.insideUnitSphere * 0.3f).normalized;

            // bias toward south pole
            if (dir.y > -southBias) 
                continue;

            Vector3 rayStart = Center + dir * (waterRadius + 2f); // start a bit above water
            if (Physics.Raycast(rayStart, -dir, out RaycastHit hit, waterRadius * 3f, terrainMask))
            {
                float seabedDist = (hit.point - Center).magnitude;
                float depth = waterRadius - seabedDist;

                if (depth > 0.1f) // underwater only
                {
                    // store candidate with direction
                    candidates.Add((hit.point, depth, dir));
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[EnemySpawnPointPlacer] No valid spawn points found!");
            return;
        }

        // sort deepest first
        candidates.Sort((a, b) => b.depth.CompareTo(a.depth));

        // pick spawn points with spacing
        foreach (var candidate in candidates)
        {
            if (spawnPoints.Count >= maxSpawnPoints)
                break;

            Vector3 posOnSurface = Center + candidate.dir * waterRadius;

            bool tooClose = false;
            foreach (var existing in spawnPoints)
            {
                if (Vector3.Distance(existing.position, posOnSurface) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            Vector3 up = (posOnSurface - Center).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.Cross(up, Vector3.right);

            Quaternion rot = Quaternion.LookRotation(forward, up);

            GameObject go;
            if (spawnPrefab)
                go = Instantiate(spawnPrefab, posOnSurface + up * lift, rot, transform);
            else
            {
                go = new GameObject("EnemySpawnPoint");
                go.transform.SetParent(transform);
                go.transform.SetPositionAndRotation(posOnSurface + up * lift, rot);
            }

            spawnPoints.Add(go.transform);
        }
    }

    public IEnumerator GenerateCoroutine()
    {
        GenerateSpawnPoints();
        yield return null;
    }
}
