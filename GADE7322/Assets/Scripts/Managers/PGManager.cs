using System.Collections;
using UnityEngine;

public class PGManager : MonoBehaviour
{
    [Header("Generators")]
    [SerializeField] private GameObject crystalTowerPrefab;
    [SerializeField] private DefenderSpotGenerator spotGenerator;

    [Header("Planet")]
    public Transform planet;
    
    [Header("North Pole Position")]
    public Vector3 northPoleOffset = new Vector3(0, 5, 0);

    private void Start()
    {
        StartCoroutine(RunGenerationSequence());
    }

    private IEnumerator RunGenerationSequence()
    {
        Debug.Log("Starting procedural generation sequence...");
        
        // 3. Terrain
        

        // 2. Spawn Crystal Tower at North Pole
        Vector3 northPole = planet.transform.position + northPoleOffset;
        GameObject crystalObj = Instantiate(crystalTowerPrefab, northPole, Quaternion.identity);
        CrystalTowerGenerator generator = crystalObj.GetComponent<CrystalTowerGenerator>();
        yield return StartCoroutine(generator.GenerateCoroutine());


        // 3. Tower spots
        if (spotGenerator != null)
        {
            yield return StartCoroutine(spotGenerator.GenerateSpotsCoroutine());
        }

        Debug.Log("Procedural generation complete!");
    }
}