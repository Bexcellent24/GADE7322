using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)] // ensure we run after most components
public class EnemyTypeInstaller : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private EnemyType enemyType;

    [Header("Health Init")]
    [SerializeField] private Faction faction = Faction.Enemy;
    [SerializeField] private bool triggerGameOver = false;

    [Header("Apply Options")]
    [SerializeField] private bool applyNavigatorSpeeds = true; // <- key: write into WaterGlobeNavigator
    [SerializeField] private bool applyNavigatorHover   = false; // optional, if you want SO to drive hoverOffset too

    bool _applied;

    void Start()
    {
        if (_applied || !Application.isPlaying || enemyType == null) return;
        Apply(enemyType);
        _applied = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply EnemyType Now (Play Mode Only)")]
    void ApplyNowInEditor()
    {
        if (Application.isPlaying && enemyType != null && !_applied)
        {
            Apply(enemyType);
            _applied = true;
        }
    }
#endif

    public void SetEnemyType(EnemyType type, bool applyImmediately = true)
    {
        enemyType = type;
        if (Application.isPlaying && applyImmediately && !_applied && type != null)
        {
            Apply(type);
            _applied = true;
        }
    }

    void Apply(EnemyType data)
    {
        // Health via your Initialize(max, faction, worth, triggerGameOver)
        var health = GetComponentInChildren<Health>();
        if (health != null)
            health.Initialize(data.maxHealth, faction, data.worth, triggerGameOver);

        // Write speeds directly into WaterGlobeNavigator so it doesn’t override them
        if (applyNavigatorSpeeds)
        {
            var nav = GetComponentInChildren<WaterGlobeNavigator>();
            if (nav)
            {
                nav.moveSpeed    = data.moveSpeed;
                nav.turnSpeedDeg = data.turnSpeedDeg;
            }
        }

        // NOTE: No visual spawning, no native allocations.
    }
}
