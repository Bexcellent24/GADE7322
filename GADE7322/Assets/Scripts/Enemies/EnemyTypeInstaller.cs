using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class EnemyTypeInstaller : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private EnemyType enemyType;

    [Header("Health Init")]
    [SerializeField] private Faction faction = Faction.Enemy;
    [SerializeField] private bool triggerGameOver = false;

    [Header("Apply Options")]
    [SerializeField] private bool applyNavigatorSpeeds = true;

    private bool _applied;

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
        var health = GetComponentInChildren<Health>();
        if (health != null)
            health.Initialize(data.maxHealth, faction, data.worth, triggerGameOver);

        // Use public properties instead of direct field access
        if (applyNavigatorSpeeds)
        {
            var nav = GetComponentInChildren<WaterGlobeNavigator>();
            if (nav)
            {
                nav.MoveSpeed = data.moveSpeed;
                nav.TurnSpeedDeg = data.turnSpeedDeg;
            }
        }
    }
}