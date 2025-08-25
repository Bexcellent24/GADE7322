using UnityEngine;


[CreateAssetMenu(fileName = "TowerDefense", menuName = "Actors/Actor Stats")]
public class ActorStats : ScriptableObject
{
    [Header("Setup")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    
    [Header("Stats")]
    public int maxHealth = 100;
    public float attackRate = 1f;
    public float range = 3f;
    public float moveSpeed = 2f;
    
}

