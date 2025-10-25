using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public int Current { get; set; } 
    public int Max { get; set; }  // Changed to have setter for upgrades
    public Transform Transform => transform; 
    public bool IsAlive => Current > 0;
    public event Action<IDamageable> OnDeath;
    public event Action OnHealthChanged;
    public static event Action OnDefenderDied;

    private Faction faction;
    private int worth;
    private bool triggerGameOver;
    private bool isDying = false;  // Prevent multiple death triggers

    public void Initialize(int max, Faction faction, int worth, bool triggerGameOver)
    {
        Max = max;
        Current = max;
        this.faction = faction;
        this.worth = worth;
        this.triggerGameOver = triggerGameOver;
        OnHealthChanged?.Invoke();
    }

    public void UpdateHealth()
    {
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || isDying) return;

        Current -= amount;
        Current = Mathf.Max(0, Current);  // Clamp to 0
        OnHealthChanged?.Invoke();

        if (Current <= 0) Die();
    }

    private void Die()
    {
        if (isDying) return;  // Prevent multiple death calls
        isDying = true;
        
        Current = 0;
        OnHealthChanged?.Invoke();
        OnDeath?.Invoke(this);

        if (faction == Faction.Enemy)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency(worth);
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IncrementEnemiesKilled();
            }
        }
        else
        {
            OnDefenderDied?.Invoke();
            AudioManager.Instance?.PlaySFX("Break");
        }
        
        if (triggerGameOver && GameManager.Instance != null)
        {
            GameManager.Instance.LoseGame();
        }
        
        Destroy(gameObject);
    }
}