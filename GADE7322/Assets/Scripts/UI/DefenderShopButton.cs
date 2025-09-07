using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class DefenderShopButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Defender Info")]
    [SerializeField] private TowerData towerData;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Placement")]
    [SerializeField] private DefenderPlacer defenderPlacer;
    
    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;


    private void Awake()
    {
        // Populate UI from towerData
        if (towerData != null)
        {
            iconImage.sprite = towerData.icon;
            nameText.text = towerData.towerName;
            priceText.text = towerData.cost.ToString();
        }
        
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateButtonState;
    }

    private void OnDestroy()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateButtonState;
    }
    
    private void UpdateButtonState(int currentCurrency)
    {
        bool canAfford = currentCurrency >= towerData.cost;
        
        button.interactable = canAfford;

        if (canvasGroup != null)
        {
            // Fade out if unaffordable
            canvasGroup.alpha = canAfford ? 1f : 0.3f;
            canvasGroup.interactable = canAfford;
            canvasGroup.blocksRaycasts = canAfford;
        }
    }
    
    
    // --- Drag Events ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (towerData != null)
            defenderPlacer.BeginDrag(towerData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        defenderPlacer.UpdateDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        defenderPlacer.EndDrag();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && towerData != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = $"{towerData.towerName}\n \n" +
                               $"Damage: {towerData.stats.damage}\n" +
                               $"Range: {towerData.stats.range}\n" +
                               $"Attack Rate: {towerData.stats.attackRate}\n \n" +
                               $"{towerData.description}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
