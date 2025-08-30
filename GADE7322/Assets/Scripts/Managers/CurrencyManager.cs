using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    [SerializeField] private int startingCurrency = 100;
    public static CurrencyManager Instance { get; private set; }
    public int CurrentCurrency { get; private set; }

    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        CurrentCurrency = startingCurrency;
    }

    public void AddCurrency(int amount)
    {
        CurrentCurrency += amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }

    public bool SpendCurrency(int amount)
    {
        if (CurrentCurrency < amount) return false;

        CurrentCurrency -= amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
        return true;
    }

    public void SetCurrency(int amount)
    {
        CurrentCurrency = amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }
}

