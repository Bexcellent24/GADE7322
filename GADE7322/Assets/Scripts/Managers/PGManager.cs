using System.Collections;
using UnityEngine;

public class PGManager : MonoBehaviour
{
    [Header("Generators")]
    [SerializeField] private DefenderSpotPlacer defenderSpotPlacer;
    [SerializeField] private MarchingCubesPlanet planetGenerator;
    
    [Header("References")]
    [SerializeField] private GameObject crystalTowerPrefab;

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
        
        // 1. Terrain
       // yield return StartCoroutine(planetGenerator.GenerateCoroutine());

        // 2. Tower spots
        yield return StartCoroutine(defenderSpotPlacer.GenerateCoroutine());
        
        // 3. Spawn Crystal Tower at North Pole
        Vector3 northPole = planet.transform.position + northPoleOffset;
        GameObject crystalObj = Instantiate(crystalTowerPrefab, northPole, Quaternion.identity);
        CrystalTowerGenerator generator = crystalObj.GetComponent<CrystalTowerGenerator>();
        yield return StartCoroutine(generator.GenerateCoroutine());
        
        //4. Enemy Spawner
        
    }
}