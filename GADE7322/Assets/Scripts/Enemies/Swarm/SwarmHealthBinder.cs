// SwarmHealthBinder.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class SwarmHealthBinder : MonoBehaviour
{
    public SwarmParticlesController particles;
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (!particles) particles = GetComponentInChildren<SwarmParticlesController>(true);
        if (!health || !particles) return;

        // initial sync AFTER Health.Initialize
        Sync();

        health.OnHealthChanged += Sync;
        health.OnDeath += OnDeathHandler;
    }

    void OnDestroy()
    {
        if (!health) return;
        health.OnHealthChanged -= Sync;
        health.OnDeath -= OnDeathHandler;
    }

    private void OnDeathHandler(IDamageable _) => Sync();

    private void Sync()
    {
        if (!particles || !health) return;
        particles.SetMaxHealth(health.Max);
        particles.SetCurrentHealth(health.Current);
    }
}