// Assets/Scripts/Enemies/Swarm/SwarmEnemyType.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Enemies/Swarm Enemy Type", fileName = "SwarmEnemyType")]
public class SwarmEnemyType : ScriptableObject 
{
    [Header("Identity / Visuals")]
    public string displayName = "Swarm";
    public GameObject visualPrefab; // optional body mesh

    [Header("Core Stats")]
    [Min(1)]  public int   maxHealth    = 80;
    [Min(0)]  public int   worth        = 1;
    [Min(0f)] public float moveSpeed    = 0.66f;
    [Min(0f)] public float turnSpeedDeg = 180f;

    [Header("Combat")]
    public bool  canAttack      = true;
    [Min(0f)] public float attackRange  = 1.75f;
    [Min(0f)] public float attackRate   = 0.6f;
    [Min(0f)] public float attackDamage = 6f;

    [Header("Targeting")]
    public LayerMask towerMask;
    public string towerTag = "Tower";

    [Header("Altitude")]
    public float extraHoverOffset = 0.5f;   // added to navigator.hoverOffset
    public float visualLift       = 0.6f;   // local Y offset for the SwarmVFX child

    [Header("Swarm VFX (Particle Controller)")]
    [Min(0)] public int particlesAt100 = 60;
    [Min(0)] public int particlesAt50  = 30;
    [Min(0)] public int particlesAt10  = 8;
    [Min(0f)] public float swarmRadius = 1.4f;
    public float orbitSpeedBoost = 0.3f;
}