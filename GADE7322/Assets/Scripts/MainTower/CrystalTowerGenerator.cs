using System.Collections;
using UnityEngine;

public class CrystalTowerGenerator : MonoBehaviour
{
    [Header("Crystal Settings")]
    public int heightLayers = 10;          // Number of layers
    public int crystalsPerLayerBase = 12;  // Max crystals at base
    public float baseRadius = 5f;          // Radius at base
    public float topRadius = 1f;           // Radius at top
    public float baseHeight = 3f;          // Height of bottom crystals
    public float topHeight = 1f;           // Height of top crystals
    public GameObject[] crystalPrefabs;    // Different crystal prefabs
    public Transform parent;

    [Header("Randomness")]
    [Range(0f, 0.3f)] public float jitter = 0.2f;      
    [Range(0f, 0.5f)] public float lean = 0.2f;        
    [Range(0f, 0.5f)] public float skipChance = 0.2f;  

    
    [Header("Firepoint")]
    public Transform firePoint;            
    public float firePointOffset = 1f;     
    
    public void GenerateCrystalTower()
    {
        if (parent == null) parent = transform;

        // Clear existing
        foreach (Transform child in parent)
        {
            if (child != firePoint) // Don't destroy firePoint
                DestroyImmediate(child.gameObject);
        }

        float currentY = 0f;
        float maxHeight = 0f;

        for (int layer = 0; layer < heightLayers; layer++)
        {
            float t = layer / (float)(heightLayers - 1);

            // Interpolate radius and crystal height
            float radius = Mathf.Lerp(baseRadius, topRadius, t);
            float crystalHeight = Mathf.Lerp(baseHeight, topHeight, t);

            // Fewer crystals at top
            int crystalsThisLayer = Mathf.Max(1, Mathf.RoundToInt(crystalsPerLayerBase * radius / baseRadius));

            for (int i = 0; i < crystalsThisLayer; i++)
            {
                if (Random.value < skipChance) continue; // Random gaps

                float angle = i * Mathf.PI * 2f / crystalsThisLayer;

                // Base position on ring
                Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                pos.y = currentY;

                // Add jitter
                pos += new Vector3(
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter, jitter)
                );

                // Spawn prefab
                GameObject prefab = crystalPrefabs[Random.Range(0, crystalPrefabs.Length)];
                GameObject crystal = Instantiate(prefab, parent.position + pos, Quaternion.identity, parent);

                // Scale based on layer height and randomness
                float randHeight = crystalHeight * Random.Range(0.8f, 1.2f);
                Vector3 baseScale = prefab.transform.localScale;
                crystal.transform.localScale = new Vector3(baseScale.x, baseScale.y * randHeight, baseScale.z);

                // Lean outward, decreasing toward the top
                float leanAmount = Mathf.Lerp(lean, 0f, t);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;
                Vector3 leanDir = (Vector3.up + dir * leanAmount).normalized;
                crystal.transform.rotation = Quaternion.FromToRotation(Vector3.up, leanDir);
                
                // Track tallest point
                float crystalTop = pos.y + (baseScale.y * randHeight);
                if (crystalTop > maxHeight)
                    maxHeight = crystalTop;
            }

            // Move up by .3 to overlap layers
            currentY += crystalHeight * 0.3f;
        }
        
        // Position firePoint above the tallest crystal
        if (firePoint != null)
        {
            firePoint.position = parent.position + Vector3.up * (maxHeight + firePointOffset);
        }
    }

    public IEnumerator GenerateCoroutine()
    {
        GenerateCrystalTower();
        yield return null;
    }
}
