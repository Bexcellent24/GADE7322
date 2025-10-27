using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UpgradeConfig", menuName = "Towers/Upgrade Configuration")]
public class UpgradeConfiguration : ScriptableObject
{
    
    [Header("Display Info")]
    [Tooltip("The display name for this unit (shown in upgrade UI)")]
    public string unitDisplayName = "Tower";
    
    [Header("Upgrade Type")]
    [Tooltip("How should this unit's visuals be upgraded?")]
    public UpgradeType upgradeType = UpgradeType.None;
    
    [Header("Upgrade Costs")]
    public int level1Cost = 100;
    public int level2Cost = 200;
    
    [Header("Level 1 Stat Multipliers")]
    public float level1DamageMultiplier = 1.5f;
    public float level1RangeMultiplier = 1.2f;
    public float level1AttackRateMultiplier = 1.3f;
    public float level1HealthMultiplier = 1.3f;
    
    [Header("Level 2 Stat Multipliers")]
    public float level2DamageMultiplier = 2.0f;
    public float level2RangeMultiplier = 1.5f;
    public float level2AttackRateMultiplier = 1.6f;
    public float level2HealthMultiplier = 1.6f;
    
    [Header("Visual Upgrade Rules for Defenders")]
    [Range(0f, 1f)] 
    [Tooltip("Percentage of tiles to swap at level 1")]
    public float level1TileSwapPercentage = 0.35f;
    
    [Range(0f, 1f)] 
    [Tooltip("Percentage of tiles to swap at level 2")]
    public float level2TileSwapPercentage = 0.65f;
    
    [Tooltip("Tile variant mappings for WFC defenders")]
    public List<TileUpgradeMapping> tileUpgradeMappings;
    
    [Header("Visual Upgrade Rules for Main Tower)")]
    [Tooltip("Mesh to swap to at level 1")]
    public GameObject level1MeshPrefab;
    
    [Tooltip("Mesh to swap to at level 2")]
    public GameObject level2MeshPrefab;
    
    // Helper methods
    public float GetDamageMultiplier(int level)
    {
        return level switch
        {
            1 => level1DamageMultiplier,
            2 => level2DamageMultiplier,
            _ => 1f
        };
    }
    
    public float GetRangeMultiplier(int level)
    {
        return level switch
        {
            1 => level1RangeMultiplier,
            2 => level2RangeMultiplier,
            _ => 1f
        };
    }
    
    public float GetAttackRateMultiplier(int level)
    {
        return level switch
        {
            1 => level1AttackRateMultiplier,
            2 => level2AttackRateMultiplier,
            _ => 1f
        };
    }
    
    public float GetHealthMultiplier(int level)
    {
        return level switch
        {
            1 => level1HealthMultiplier,
            2 => level2HealthMultiplier,
            _ => 1f
        };
    }
    
    public int GetUpgradeCost(int currentLevel)
    {
        return currentLevel switch
        {
            0 => level1Cost,
            1 => level2Cost,
            _ => 0
        };
    }
    
    public bool DoesDamageUpgrade(int level)
    {
        float multiplier = GetDamageMultiplier(level);
        return multiplier > 1f && !Mathf.Approximately(multiplier, 1f);
    }
    
    public bool DoesRangeUpgrade(int level)
    {
        float multiplier = GetRangeMultiplier(level);
        return multiplier > 1f && !Mathf.Approximately(multiplier, 1f);
    }
    
    public bool DoesAttackRateUpgrade(int level)
    {
        float multiplier = GetAttackRateMultiplier(level);
        return multiplier > 1f && !Mathf.Approximately(multiplier, 1f);
    }
    
    public bool DoesHealthUpgrade(int level)
    {
        float multiplier = GetHealthMultiplier(level);
        return multiplier > 1f && !Mathf.Approximately(multiplier, 1f);
    }
}

[System.Serializable]
public class TileUpgradeMapping
{
    [Tooltip("The base tile from the WFC generation")]
    public WFCTile baseTile;
    
    [Tooltip("The upgraded variant - used for both level 1 AND level 2")]
    public WFCTile upgradedVariant;
}