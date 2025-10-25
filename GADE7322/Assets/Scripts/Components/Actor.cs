using UnityEngine;
using UnityEngine.Serialization;

public class Actor : MonoBehaviour, ISelectable
{
    public ActorStats stats;
    public Faction faction;

    [HideInInspector] public Health health;
    [HideInInspector] public Attacker attacker;
    [HideInInspector] public AuraAttacker auraAttacker;
    [HideInInspector] public RangeIndicator indicator;
    [HideInInspector] public WaterGlobeNavigator navigator;
    
    [Header("Upgrade System")]
    [Tooltip("Assign an UpgradeConfiguration to make this Actor upgradeable")]
    public UpgradeConfiguration upgradeConfig;
    
    [HideInInspector] public UpgradeController upgradeController;
    
    public bool IsSelectable => faction == Faction.Ally || stats.triggerGameOver; // Defenders + Main Tower

    void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();
        auraAttacker = GetComponent<AuraAttacker>();
        indicator = GetComponentInChildren<RangeIndicator>();
        navigator = GetComponent<WaterGlobeNavigator>();
        upgradeController = GetComponent<UpgradeController>();

        if (stats != null && health != null)
            health.Initialize(stats.maxHealth, faction, stats.worth, stats.triggerGameOver);
        
        if (stats != null && attacker != null)
            attacker.Initialize(stats.bulletPrefab, stats.range, stats.attackRate, stats.damage);
        
        if (stats != null && auraAttacker != null)
            auraAttacker.Initialize(stats.range, stats.damage);

        if (indicator != null)
            indicator.Initialize(stats.range);
        
        if (navigator != null)
        {
            navigator.MoveSpeed = stats.moveSpeed;
            navigator.TurnSpeedDeg = stats.turnSpeedDeg;
        }
        
        // Initialize upgrade system if config is assigned
        if (upgradeConfig != null && upgradeController != null)
        {
            upgradeController.Initialize(this, upgradeConfig);
        }
        else if (upgradeConfig != null && upgradeController == null)
        {
            Debug.LogWarning($"[Actor] {gameObject.name} has UpgradeConfig but no UpgradeController component!");
        }
    }
    
    // ISelectable implementation
    public void OnSelected()
    {
        // Always show range indicator
        if (indicator != null)
            indicator.Show();
        
        // Show upgrade panel if this actor is upgradeable
        if (upgradeController != null && upgradeConfig != null && upgradeConfig.upgradeType != UpgradeType.None)
        {
            UpgradeUIManager.Instance?.ShowUpgradePanel(this);
        }
    }
    
    public void OnDeselected()
    {
        // Hide range indicator
        if (indicator != null)
            indicator.Hide();
        
        // Hide upgrade panel
        UpgradeUIManager.Instance?.HideUpgradePanel();
    }
}