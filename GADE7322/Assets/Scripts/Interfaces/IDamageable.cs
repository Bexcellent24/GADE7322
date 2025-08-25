using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
    Transform Transform { get; } 
    bool IsAlive { get; }
}
