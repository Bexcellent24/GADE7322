using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    [SerializeField] private GameObject indicatorPrefab;
    private GameObject indicatorInstance;

    public void Initialize(float range)
    {
        if (indicatorPrefab == null) return;
        indicatorInstance = Instantiate(indicatorPrefab, transform);
        indicatorInstance.SetActive(false);
        
        //all of this cause i was having ussies with local scaling 
        float diameter = range * 2f;
        Transform originalParent = indicatorInstance.transform.parent;
        indicatorInstance.transform.localScale = Vector3.one * diameter;
        indicatorInstance.transform.SetParent(null, true); // temporarily unparent
        indicatorInstance.transform.localScale = new Vector3(diameter, diameter, diameter);
        indicatorInstance.transform.SetParent(originalParent, true); // reparent unparent
    }

    public void Show() => indicatorInstance?.SetActive(true);
    public void Hide() => indicatorInstance?.SetActive(false);
}

