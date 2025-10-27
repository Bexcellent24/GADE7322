using UnityEngine;

[DefaultExecutionOrder(-50)] // runs before Actor.Awake()
[RequireComponent(typeof(SwarmEnemy))]
[RequireComponent(typeof(WaterGlobeNavigator))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Actor))]
public class SwarmEnemyInstaller : MonoBehaviour
{
    [Header("Swarm Config")]
    public SwarmEnemyType config;

    [Header("Actor Stats ")]
    public ActorStats baseStats;

    private SwarmEnemy ai;
    private WaterGlobeNavigator nav;
    private Actor actor;

    void Reset()
    {
        actor     = GetComponent<Actor>();
    }

    void Awake()
    {
        if (!config)
        {
            Debug.LogError($"{name}: Missing SwarmEnemyType, installer aborted.");
            enabled = false;
            return;
        }

        ai    = GetComponent<SwarmEnemy>();
        nav   = GetComponent<WaterGlobeNavigator>();
        actor = GetComponent<Actor>();
        
        
        actor.faction = Faction.Enemy;

        if (!baseStats)
        {
            baseStats = ScriptableObject.CreateInstance<ActorStats>();
            baseStats.maxHealth      = config.maxHealth;
            baseStats.worth          = config.worth;
            baseStats.attackRate     = config.attackRate;
            baseStats.range          = config.attackRange;
            baseStats.damage         = config.attackDamage;
            baseStats.moveSpeed      = config.moveSpeed;
            baseStats.turnSpeedDeg   = config.turnSpeedDeg;
            baseStats.triggerGameOver = false;
        }

        actor.stats = baseStats;
        
        ai.attackRange   = config.attackRange;
        ai.attackRate    = config.attackRate;
        ai.attackDamage  = config.attackDamage;
        ai.towerMask     = config.towerMask;
        ai.towerTag      = config.towerTag;
        
    }

    void Start()
    {
        if (nav != null)
        {
            nav.HoverOffset += config.extraHoverOffset;
            nav.SurfaceSnap  = Mathf.Min(nav.SurfaceSnap, 0.08f);
        }
    }
}
