using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class TowerButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Tower Info")]
    [SerializeField] private TowerData towerData;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;

    [FormerlySerializedAs("towerPlacer")]
    [Header("Placement")]
    [SerializeField] private DefenderPlacer defenderPlacer;

    private void Awake()
    {
        // Populate UI from towerData
        if (towerData != null)
        {
            iconImage.sprite = towerData.icon;
            nameText.text = towerData.towerName;
            priceText.text = "R " + towerData.cost;
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
}
