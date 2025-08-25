using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public int Current { get; private set; }
    public int Max { get; private set; }

    public bool IsAlive => Current > 0;
    public Transform Transform => transform;

    public event Action<IDamageable> OnDeath;

    public void Initialize(int max)
    {
        Max = max;
        Current = max;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        Current -= amount;
        if (Current <= 0) Die();
    }

    private void Die()
    {
        Current = 0;
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}