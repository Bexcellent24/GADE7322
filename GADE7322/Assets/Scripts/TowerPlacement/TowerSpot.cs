using UnityEngine;

public class TowerSpot : MonoBehaviour
{
    private ParticleSystem ps;
    public bool IsOccupied { get; private set; } = false;

    void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();
    }

    public void Show() { if (ps != null && !ps.isPlaying) ps.Play(); }
    public void Hide() { if (ps != null && ps.isPlaying) ps.Stop(); }

    public bool CanPlaceTower()
    {
        return !IsOccupied;
    }

    public void PlaceTower(GameObject towerPrefab)
    {
        if (IsOccupied) return;

        Instantiate(towerPrefab, transform.position, Quaternion.identity);
        IsOccupied = true;
        Hide(); // hide effect once used
    }
}