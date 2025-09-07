using UnityEngine;

public class DefenderSpotLink : MonoBehaviour
{
    private DefenderSpot spot;
    
    public void AssignSpot(DefenderSpot s)
    {
        spot = s;
        
        var health = GetComponent<Health>();
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void HandleDeath(IDamageable dead)
    {
        if (spot != null)
        {
            spot.ClearSpot();
            spot = null;
        }
    }

    private void OnDestroy()
    {
        var health = GetComponent<Health>();
        if (health != null)
            health.OnDeath -= HandleDeath;
    }
}
