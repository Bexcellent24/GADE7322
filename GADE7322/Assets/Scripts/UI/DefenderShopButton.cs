using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class DefenderShopButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Defender Info")]
    [SerializeField] private TowerData towerData;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;
    
    [Header("Placement")]
    [SerializeField] private DefenderPlacer defenderPlacer;

    private void Awake()
    {
        // Populate UI from towerData
        if (towerData != null)
        {
            iconImage.sprite = towerData.icon;
            nameText.text = towerData.towerName;
            priceText.text = towerData.cost.ToString();
        }
        
        // Subscribe to currency changes
        CurrencyManager.Instance.OnCurrencyChanged += UpdateButtonState;
        UpdateButtonState(CurrencyManager.Instance.CurrentCurrency);
    }
    
    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateButtonState;
    }
    
    private void UpdateButtonState(int currentCurrency)
    {
        button.interactable = currentCurrency >= towerData.cost;
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
}
