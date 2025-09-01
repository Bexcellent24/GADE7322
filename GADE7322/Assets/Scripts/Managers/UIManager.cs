using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text CurrencyText;

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyHandler;
    }
    
    private void OnDestroy()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyHandler;
    }

    private void UpdateCurrencyHandler(int amount)
    {
        Debug.Log($"Updating currency: {amount}");
        CurrencyText.text = amount.ToString();
    }
}
