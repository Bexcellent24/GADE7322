using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "TD/Enemy Type")]
public class EnemyType : ScriptableObject
{
    [Header("Identity / Visuals")]
    public string displayName = "Light";
    public GameObject visualPrefab; // ignored by the safe installer

    [Header("Stats")]
    [Min(1)]  public int   maxHealth    = 30;
    [Min(0)]  public int   worth        = 1;
    [Min(0f)] public float moveSpeed    = 0.66f;
    [Min(0f)] public float turnSpeedDeg = 180f;

    [Header("Combat (optional)")]
    public bool  canAttack    = true;
    [Min(0f)] public float attackRange  = 1.5f;
    [Min(0f)] public float attackRate   = 1.0f;
    [Min(0f)] public float attackDamage = 2.5f;
}