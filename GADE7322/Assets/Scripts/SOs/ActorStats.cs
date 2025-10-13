using UnityEngine;


[CreateAssetMenu(fileName = "TowerDefense", menuName = "Actors/Actor Stats")]
public class ActorStats : ScriptableObject
{
    [Header("Setup")]
    public GameObject bulletPrefab;

    [Header("Combat Stats")]
    [Min(1)] public int maxHealth = 100;
    [Min(0f)] public float attackRate = 1f;
    [Min(0f)] public float range = 3f;
    [Min(0f)] public float damage = 15f;
    public bool triggerGameOver = false;

    [Header("Optional Enemy Fields")]
    [Min(0)] public int worth = 0;
    [Min(0f)] public float moveSpeed = 0f;
    [Min(0f)] public float turnSpeedDeg = 0f;
    
}

