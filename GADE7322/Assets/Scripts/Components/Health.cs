using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public int Current { get; private set; } 
    public int Max { get; private set; }
    public Transform Transform => transform; 
    public bool IsAlive => Current > 0;
    public event Action<IDamageable> OnDeath;
    public static event Action OnEnemyKilled;
    public static event Action OnGameOverTriggered;
    public event Action OnHealthChanged;

    private Faction faction;
    private int worth;
    private bool triggerGameOver;

    public void Initialize(int max, Faction faction, int worth, bool triggerGameOver)
    {
        Max = max;
        Current = max;
        this.faction = faction;
        this.worth = worth;
        this.triggerGameOver = triggerGameOver;
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        Current -= amount;
        OnHealthChanged?.Invoke();

        if (Current <= 0) Die();
    }

    private void Die()
    {
        Current = 0;
        OnHealthChanged?.Invoke();
        OnDeath?.Invoke(this);

        if (faction == Faction.Enemy)
        {
            CurrencyManager.Instance.AddCurrency(worth);
            OnEnemyKilled?.Invoke();
        }
        if (triggerGameOver)
        {
            OnGameOverTriggered?.Invoke();
        }
        
        Destroy(gameObject);
    }
}
