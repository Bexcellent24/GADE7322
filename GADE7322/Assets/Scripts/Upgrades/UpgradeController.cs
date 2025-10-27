using UnityEngine;

// Handles all upgrade logic for an Actor (stats, visuals, costs).
// Supports both tile-based (defenders) and mesh-based (main tower) upgrades.
[RequireComponent(typeof(Actor))]
public class UpgradeController : MonoBehaviour, IUpgradable
{
    private Actor actor;
    private UpgradeConfiguration config;
    private IVisualUpgradeStrategy visualUpgradeStrategy;
    private bool hasSetupStrategy = false;
    
    public int CurrentUpgradeLevel { get; private set; } = 0;
    public int MaxUpgradeLevel => 2;
    
    private ActorStats baseStats; // Store original stats
    
    public void Initialize(Actor actor, UpgradeConfiguration config)
    {
        this.actor = actor;
        this.config = config;
        
        // Store base stats
        if (actor.stats != null)
        {
            baseStats = actor.stats;
        }
        else
        {
            Debug.LogError($"[UpgradeController] Actor {actor.gameObject.name} has no stats assigned!");
        }
        
        hasSetupStrategy = false;
    }
    
    private void SetupVisualUpgradeStrategy()
    {
        if (hasSetupStrategy) return; // Only setup once
        
        if (config == null)
        {
            Debug.LogWarning($"[UpgradeController] No upgrade config assigned to {gameObject.name}");
            hasSetupStrategy = true;
            return;
        }
        
        switch (config.upgradeType)
        {
            case UpgradeType.TileSwap:
                var tileSwapper = GetComponent<TileSwapController>();
                if (tileSwapper == null)
                {
                    // Try to find it on children
                    tileSwapper = GetComponentInChildren<TileSwapController>();
                }
                
                if (tileSwapper == null)
                {
                    Debug.LogError($"[UpgradeController] {gameObject.name} needs TileSwap but has no TileSwapController!");
                }
                else if (!tileSwapper.IsValid())
                {
                    Debug.LogError($"[UpgradeController] TileSwapController on {gameObject.name} is not initialized. WFC may not have completed yet!");
                }
                visualUpgradeStrategy = tileSwapper;
                break;
                
            case UpgradeType.MeshSwap:
                var meshSwapper = GetComponent<MeshSwapController>();
                if (meshSwapper == null)
                {
                    Debug.LogError($"[UpgradeController] {gameObject.name} needs MeshSwap but has no MeshSwapController!");
                }
                else if (!meshSwapper.IsValid())
                {
                    Debug.LogWarning($"[UpgradeController] MeshSwapController on {gameObject.name} is not properly configured.");
                }
                visualUpgradeStrategy = meshSwapper;
                break;
                
            case UpgradeType.None:
                Debug.Log($"[UpgradeController] {gameObject.name} has upgrade type set to None - no visual upgrades will occur.");
                visualUpgradeStrategy = null;
                break;
                
            default:
                Debug.LogWarning($"[UpgradeController] Unknown upgrade type: {config.upgradeType}");
                visualUpgradeStrategy = null;
                break;
        }
        
        hasSetupStrategy = true;
    }
    
    public bool CanUpgrade()
    {
        if (CurrentUpgradeLevel >= MaxUpgradeLevel)
        {
            return false;
        }
        
        if (config == null)
        {
            Debug.LogWarning("[UpgradeController] Cannot upgrade - no configuration assigned!");
            return false;
        }
        
        int cost = GetUpgradeCost();
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[UpgradeController] CurrencyManager instance not found!");
            return false;
        }
        
        return CurrencyManager.Instance.HasEnough(cost);
    }
    
    public int GetUpgradeCost()
    {
        if (config == null) return 0;
        return config.GetUpgradeCost(CurrentUpgradeLevel);
    }
    
    public UpgradeType GetUpgradeType()
    {
        return config != null ? config.upgradeType : UpgradeType.None;
    }
    
    public void ApplyUpgrade()
    {
        if (!CanUpgrade())
        {
            Debug.LogWarning("[UpgradeController] Cannot upgrade - check CanUpgrade() first!");
            return;
        }
        
        Debug.Log($"[UpgradeController] Starting upgrade for {gameObject.name}...");
        
        try
        {
            // Setup visual strategy on first upgrade
            if (!hasSetupStrategy)
            {
                SetupVisualUpgradeStrategy();
            }
            
            // Deduct cost
            int cost = GetUpgradeCost();
            if (!CurrencyManager.Instance.SpendCurrency(cost))
            {
                Debug.LogError("[UpgradeController] Failed to spend currency!");
                return;
            }
            
            // Increment level
            CurrentUpgradeLevel++;
            Debug.Log($"[UpgradeController] Upgraded {gameObject.name} to level {CurrentUpgradeLevel}");
            
            // Apply stat upgrades
            ApplyStatUpgrades();
            
            // Apply visual upgrades
            if (visualUpgradeStrategy != null && visualUpgradeStrategy.IsValid())
            {
                Debug.Log($"[UpgradeController] Applying visual upgrade...");
                visualUpgradeStrategy.ApplyVisualUpgrade(CurrentUpgradeLevel, config);
            }
            else if (config.upgradeType != UpgradeType.None)
            {
                Debug.LogWarning($"[UpgradeController] No valid visual upgrade strategy for {gameObject.name}. Visual upgrade skipped.");
            }
            
            // Play upgrade effects
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Upgrade");
            }
            
            // Notify UI
            if (UpgradeUIManager.Instance != null)
            {
                UpgradeUIManager.Instance.RefreshUpgradePanel();
            }
            
            Debug.Log($"[UpgradeController] Upgrade complete!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeController] Error during upgrade: {e.Message}\n{e.StackTrace}");
            // Rollback level if something went wrong
            CurrentUpgradeLevel--;
        }
    }
    
    private void ApplyStatUpgrades()
    {
        if (baseStats == null)
        {
            Debug.LogError("[UpgradeController] Base stats are null!");
            return;
        }
        
        if (actor == null)
        {
            Debug.LogError("[UpgradeController] Actor reference is null!");
            return;
        }
        
        float damageMulti = config.GetDamageMultiplier(CurrentUpgradeLevel);
        float rangeMulti = config.GetRangeMultiplier(CurrentUpgradeLevel);
        float attackMulti = config.GetAttackRateMultiplier(CurrentUpgradeLevel);
        float healthMulti = config.GetHealthMultiplier(CurrentUpgradeLevel);
        
        float newDamage = baseStats.damage * damageMulti;
        float newRange = baseStats.range * rangeMulti;
        float newAttackRate = baseStats.attackRate * attackMulti;
        int newMaxHealth = Mathf.RoundToInt(baseStats.maxHealth * healthMulti);
        
        Debug.Log($"[UpgradeController] New stats - DMG: {newDamage:F1}, RNG: {newRange:F1}, ATK: {newAttackRate:F1}, HP: {newMaxHealth}");
        
        // Re-initialize components with new stats
        if (actor.attacker != null)
        {
            actor.attacker.Initialize(
                baseStats.bulletPrefab, 
                newRange, 
                newAttackRate, 
                newDamage
            );
            Debug.Log("[UpgradeController] Updated Attacker stats");
        }
        
        if (actor.auraAttacker != null)
        {
            actor.auraAttacker.Initialize(newRange, newDamage);
            Debug.Log("[UpgradeController] Updated AuraAttacker stats");
        }
        
        if (actor.indicator != null)
        {
            actor.indicator.Initialize(newRange);
            Debug.Log("[UpgradeController] Updated RangeIndicator");
        }
        
        // Update health if it exists
        if (actor.health != null)
        {
            // Store current health ratio before any changes
            int oldCurrent = actor.health.Current;
            int oldMax = actor.health.Max;
            float healthRatio = oldMax > 0 ? (float)oldCurrent / oldMax : 1f;
    
            Debug.Log($"[UpgradeController] Health before: {oldCurrent}/{oldMax} (ratio: {healthRatio:F2})");
    
            // Calculate new current health based on ratio
            int newCurrentHealth = Mathf.RoundToInt(newMaxHealth * healthRatio);
    
            // Add 20% of new max health as a bonus
            int healthBonus = Mathf.RoundToInt(newMaxHealth * 0.2f);
            newCurrentHealth += healthBonus;
    
            // Clamp to new max (in case they were already near full)
            newCurrentHealth = Mathf.Clamp(newCurrentHealth, 1, newMaxHealth);
    
            // Update max first
            actor.health.Max = newMaxHealth;
    
            // Then set current
            actor.health.Current = newCurrentHealth;
    
            // Trigger health manually to trigger the health bar ui update. 
            actor.health.UpdateHealth();
    
            Debug.Log($"[UpgradeController] Health after: {newCurrentHealth}/{newMaxHealth} (bonus: +{healthBonus})");
        }

        Debug.Log($"[UpgradeController] Stat upgrades applied successfully");
    }
    
    public UpgradeStats GetCurrentStats()
    {
        if (baseStats == null)
        {
            Debug.LogWarning("[UpgradeController] Base stats are null!");
            return default;
        }
        
        float damageMulti = CurrentUpgradeLevel == 0 ? 1f : config.GetDamageMultiplier(CurrentUpgradeLevel);
        float rangeMulti = CurrentUpgradeLevel == 0 ? 1f : config.GetRangeMultiplier(CurrentUpgradeLevel);
        float attackMulti = CurrentUpgradeLevel == 0 ? 1f : config.GetAttackRateMultiplier(CurrentUpgradeLevel);
        float healthMulti = CurrentUpgradeLevel == 0 ? 1f : config.GetHealthMultiplier(CurrentUpgradeLevel);
        
        return new UpgradeStats
        {
            damage = baseStats.damage * damageMulti,
            range = baseStats.range * rangeMulti,
            attackRate = baseStats.attackRate * attackMulti,
            health = Mathf.RoundToInt(baseStats.maxHealth * healthMulti)
        };
    }
    
    public UpgradeStats GetNextUpgradeStats()
    {
        if (CurrentUpgradeLevel >= MaxUpgradeLevel || baseStats == null)
        {
            return default;
        }
        
        int nextLevel = CurrentUpgradeLevel + 1;
        float damageMulti = config.GetDamageMultiplier(nextLevel);
        float rangeMulti = config.GetRangeMultiplier(nextLevel);
        float attackMulti = config.GetAttackRateMultiplier(nextLevel);
        float healthMulti = config.GetHealthMultiplier(nextLevel);
        
        return new UpgradeStats
        {
            damage = baseStats.damage * damageMulti,
            range = baseStats.range * rangeMulti,
            attackRate = baseStats.attackRate * attackMulti,
            health = Mathf.RoundToInt(baseStats.maxHealth * healthMulti)
        };
    }
}