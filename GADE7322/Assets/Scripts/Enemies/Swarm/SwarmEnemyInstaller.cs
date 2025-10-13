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

    [Header("Actor Stats (optional override)")]
    [Tooltip("If left empty, stats will be auto-generated from SwarmEnemyType.")]
    public ActorStats baseStats;

    [Header("Optional Scene Hooks")]
    public SwarmParticlesController particles;

    private SwarmEnemy ai;
    private WaterGlobeNavigator nav;
    private Actor actor;

    void Reset()
    {
        particles = GetComponentInChildren<SwarmParticlesController>(true);
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

        if (!particles)
            particles = GetComponentInChildren<SwarmParticlesController>(true);
        
        // 1. Prepare Actor + Stats
        actor.faction = Faction.Enemy;

        // If no base stats SO assigned, generate one at runtime
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
        
        // 2. Assign Swarm-specific logic
        ai.attackRange   = config.attackRange;
        ai.attackRate    = config.attackRate;
        ai.attackDamage  = config.attackDamage;
        ai.towerMask     = config.towerMask;
        ai.towerTag      = config.towerTag;
        
        // 3. Visual / Particle setup
        if (particles)
        {
            particles.maxHealth       = config.maxHealth;
            particles.particlesAt100  = config.particlesAt100;
            particles.particlesAt50   = config.particlesAt50;
            particles.particlesAt10   = config.particlesAt10;
            particles.swarmRadius     = config.swarmRadius;
            particles.orbitSpeedBoost = config.orbitSpeedBoost;

            // Lift visual offset
            var pTr = particles.transform;
            pTr.localPosition = new Vector3(
                pTr.localPosition.x,
                config.visualLift,
                pTr.localPosition.z
            );
        }
    }

    void Start()
    {
        // 4. Altitude adjustments
        if (nav != null)
        {
            nav.HoverOffset += config.extraHoverOffset;
            nav.SurfaceSnap  = Mathf.Min(nav.SurfaceSnap, 0.08f);
        }
    }
}
