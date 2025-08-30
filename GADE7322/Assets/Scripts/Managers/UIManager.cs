using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text CurrencyText;


    private void Awake()
    {
        CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyHandler;
    }
    
    
    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateCurrencyHandler;
    }

    private void UpdateCurrencyHandler(int amount)
    {
        CurrencyText.text = amount.ToString();
    }
}
