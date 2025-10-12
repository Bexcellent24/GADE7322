using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "TD/Enemy Type")]
public class EnemyType : ScriptableObject
{
    [Header("Identity / Visuals")]
    public string displayName = "Light";
    public GameObject visualPrefab;

    [Header("Stats")]
    public int maxHealth = 30;
    public int worth = 1;
    public float moveSpeed = 3.5f;
    public float turnSpeedDeg = 180f;

    [Header("Combat (optional)")]
    public bool canAttack = true; 
    public float attackRange = 1.5f;
    public float attackRate = 1.0f;
    public float attackDamage = 5f;

    [Header("Behaviour Flags")]
    public bool splitsOnDeath = false;   
    public EnemyType splitChildType; 
    public int splitCount = 2;
    [Range(0.2f, 1.0f)] public float splitScale = 0.6f;
    public float splitBurstRadius = 0.6f;

    public bool hasFrontalShield = false; 
    [Range(0f, 180f)] public float shieldHalfAngle = 90f; 
    public float shieldRadius = 1.0f;
}