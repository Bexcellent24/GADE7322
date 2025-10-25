using System;

[Serializable]
public struct UpgradeStats
{
    public float damage;
    public float range;
    public float attackRate;
    public int health;
    
    public static UpgradeStats operator *(UpgradeStats stats, float multiplier)
    {
        return new UpgradeStats
        {
            damage = stats.damage * multiplier,
            range = stats.range * multiplier,
            attackRate = stats.attackRate * multiplier,
            health = UnityEngine.Mathf.RoundToInt(stats.health * multiplier)
        };
    }
    
    public override string ToString()
    {
        return $"DMG: {damage:F1}, RNG: {range:F1}, ATK: {attackRate:F1}, HP: {health}";
    }
}