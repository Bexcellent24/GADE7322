using UnityEngine;
using UnityEngine.Serialization;

public class Actor : MonoBehaviour
{
    public ActorStats stats;
    public Faction faction;

    [HideInInspector] public Health health;
    [HideInInspector] public Attacker attacker;
    [HideInInspector] public AuraAttacker auraAttacker;
    [HideInInspector] public RangeIndicator indicator;

    void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();
        auraAttacker = GetComponent<AuraAttacker>();
        indicator = GetComponentInChildren<RangeIndicator>();

        if (stats != null && health != null)
            health.Initialize(stats.maxHealth, faction, stats.maxHealth, stats.triggerGameOver);
        
        if (stats != null && attacker != null)
            attacker.Initialize(stats.bulletPrefab, stats.range, stats.attackRate, stats.damage);
        
        if (stats != null && auraAttacker != null)
            auraAttacker.Initialize(stats.range, stats.damage);

        if (indicator != null)
         indicator.Initialize(stats.range);
    }
}

