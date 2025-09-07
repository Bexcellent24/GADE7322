using UnityEngine;

public class DefenderPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenderSpotPlacer spotGenerator;

    private GameObject ghostInstance;
    private TowerData currentTower;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    public void BeginDrag(TowerData towerData)
    {
        currentTower = towerData;

        if (ghostInstance != null) Destroy(ghostInstance);

        // spawn ghost - cause ux is important 
        ghostInstance = Instantiate(towerData.ghostPrefab);

        // highlight all free spots
        foreach (var spot in spotGenerator.spots)
        {
            if (spot.CanPlaceTower()) 
                spot.Show();
        }
    }

    public void UpdateDrag()
    {
        if (ghostInstance == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ghostInstance.transform.position = hit.point + Vector3.up * 0.1f;
            
            // Align tower so bottom points toward planet centre
            Vector3 planetCenter = spotGenerator.transform.position;
            Vector3 dirFromCenter = (ghostInstance.transform.position - planetCenter).normalized;

            ghostInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirFromCenter);
        }
    }

    public void EndDrag()
    {
        if (ghostInstance == null) return;

        DefenderSpot nearestSpot = null;
        float minDist = Mathf.Infinity;

        foreach (var spot in spotGenerator.spots)
        {
            if (!spot.CanPlaceTower()) continue;

            float dist = Vector3.Distance(ghostInstance.transform.position, spot.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestSpot = spot;
            }
        }

        // Try to place tower
        if (nearestSpot != null && minDist < 2f)
        {
            // double checking if we can even afford the tower
            if (CurrencyManager.Instance.SpendCurrency(currentTower.cost))
            {
                nearestSpot.PlaceTower(currentTower.towerPrefab);
                AudioManager.Instance.PlaySFX("Build");
            }
            else
            {
                Debug.Log("Could not afford tower at placement time.");
            }
        }

        // Cleanup
        Destroy(ghostInstance);
        ghostInstance = null;
        currentTower = null;

        // Hide all highlights
        foreach (var spot in spotGenerator.spots)
        {
            spot.Hide();
        }
    }
}
