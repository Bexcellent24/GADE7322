using UnityEngine;

public class DefenderSpot : MonoBehaviour
{
    private ParticleSystem ps;
    public bool IsOccupied { get; private set; } = false;

    void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();
    }

    public void Show()
    {
        if (ps != null)
        {
            ps.Clear();
            ps.Play();
        }
    }
    
    public void Hide()
    {
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public bool CanPlaceTower()
    {
        return !IsOccupied;
    }

    public void PlaceTower(GameObject towerPrefab)
    {
        if (IsOccupied) return;

        Vector3 planetCenter = Vector3.zero; // Or your planet's actual transform.position
        Vector3 dirFromCenter = (transform.position - planetCenter).normalized;

        // Rotate so that the tower's local "up" points away from the planet center
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, dirFromCenter);

        // Spawn the base tower
        Instantiate(towerPrefab, transform.position, rotation);

        IsOccupied = true;
        Hide();
    }

}