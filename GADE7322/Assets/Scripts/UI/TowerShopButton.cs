using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class TowerButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Tower Info")]
    [SerializeField] private TowerData towerData;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button button;

    [Header("Placement")]
    [SerializeField] private TowerPlacer towerPlacer;

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
            towerPlacer.BeginDrag(towerData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        towerPlacer.UpdateDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        towerPlacer.EndDrag();
    }
}
