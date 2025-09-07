using UnityEngine;
using UnityEngine.Serialization;

public class Actor : MonoBehaviour
{
    public ActorStats stats;
    public Faction faction;

    [HideInInspector] public Health health;
    [HideInInspector] public Attacker attacker;
    [HideInInspector] public RangeIndicator indicator;

    void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();
        indicator = GetComponentInChildren<RangeIndicator>();

        if (stats != null && health != null)
            health.Initialize(stats.maxHealth, faction, stats.maxHealth, stats.triggerGameOver);
        
        if (stats != null && health != null)
            attacker.Initialize(stats.bulletPrefab, stats.range, stats.attackRate, stats.damage);

        if (indicator != null)
         indicator.Initialize(stats.range);
    }
}

