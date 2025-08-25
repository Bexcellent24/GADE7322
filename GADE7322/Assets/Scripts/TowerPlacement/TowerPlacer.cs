using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerSpotGenerator spotGenerator;

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

        // Spawn ghost
        ghostInstance = Instantiate(towerData.ghostPrefab);

        // Highlight all free spots
        foreach (var spot in spotGenerator.towerSpots)
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
        }
    }

    public void EndDrag()
    {
        if (ghostInstance == null) return;

        TowerSpot nearestSpot = null;
        float minDist = Mathf.Infinity;

        foreach (var spot in spotGenerator.towerSpots)
        {
            if (!spot.CanPlaceTower()) continue;

            float dist = Vector3.Distance(ghostInstance.transform.position, spot.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestSpot = spot;
            }
        }

        if (nearestSpot != null && minDist < 2f) // within snap range
        {
            nearestSpot.PlaceTower(currentTower.towerPrefab);
        }

        // Cleanup
        Destroy(ghostInstance);
        ghostInstance = null;
        currentTower = null;

        // Hide all highlights
        foreach (var spot in spotGenerator.towerSpots)
        {
            spot.Hide();
        }
    }
}
