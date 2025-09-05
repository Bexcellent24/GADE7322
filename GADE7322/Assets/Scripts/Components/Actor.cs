using UnityEngine;
using UnityEngine.Serialization;

public class Actor : MonoBehaviour
{
    public ActorStats stats;
    public Faction faction;

    [HideInInspector] public Health health;
    [HideInInspector] public Attacker attacker;

    void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();

        if (stats != null && health != null)
            health.Initialize(stats.maxHealth);
        
        if (stats != null && health != null)
            attacker.Initialize(stats.bulletPrefab, stats.range, stats.attackRate);
    }
}

