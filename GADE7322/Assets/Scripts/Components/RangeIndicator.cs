using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    [SerializeField] private GameObject indicatorPrefab;
    private GameObject indicatorInstance;
    private bool isInitialized = false;

    public void Initialize(float range)
    {
        if (indicatorPrefab == null)
        {
            Debug.LogWarning("[RangeIndicator] No indicator prefab assigned!");
            return;
        }
        
        // If we already have an indicator, update its scale instead of creating a new one
        if (isInitialized && indicatorInstance != null)
        {
            UpdateScale(range);
            Debug.Log($"[RangeIndicator] Updated existing indicator to range {range}");
            return;
        }
        
        // First time initialization - create the indicator
        indicatorInstance = Instantiate(indicatorPrefab, transform);
        indicatorInstance.SetActive(false);
        
        // Set the scale
        UpdateScale(range);
        
        isInitialized = true;
        Debug.Log($"[RangeIndicator] Created new indicator with range {range}");
    }
    
    private void UpdateScale(float range)
    {
        if (indicatorInstance == null) return;
    
        float diameter = range * 2f;
    
        // Unparent to avoid unwanted scale inheritance
        Transform originalParent = indicatorInstance.transform.parent;
        indicatorInstance.transform.SetParent(null, true);
        
        indicatorInstance.transform.localScale = new Vector3(diameter, 3.5f, diameter);
    
        // Re-parent
        indicatorInstance.transform.SetParent(originalParent, true);
    
        Debug.Log($"[RangeIndicator] Set disk scale to XZ: {diameter}, Y: 0.05 (range: {range})");
    }


    public void Show()
    {
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(true);
        }
    }
    
    public void Hide()
    {
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up the indicator when this component is destroyed
        if (indicatorInstance != null)
        {
            Destroy(indicatorInstance);
        }
    }
}