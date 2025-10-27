using UnityEngine;


// Handles visual upgrades for the Main Tower that doesn't use WFC.
// Swaps the entire mesh/prefab for upgraded versions.
public class MeshSwapController : MonoBehaviour, IVisualUpgradeStrategy
{
    [Header("References")]
    [SerializeField] 
    [Tooltip("The visual root object that will be replaced")]
    private Transform visualRoot;
    
    private GameObject currentVisual;
    private bool isInitialized = false;
    
    void Awake()
    {
        // If no visual root specified, try to find it
        if (visualRoot == null)
        {
            if (visualRoot == null && transform.childCount > 0)
                visualRoot = transform.GetChild(0);
        }
        
        if (visualRoot != null)
        {
            currentVisual = visualRoot.gameObject;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("[MeshSwapController] No visual root found! Please assign one in the inspector.");
        }
    }
    
    public void ApplyVisualUpgrade(int upgradeLevel, UpgradeConfiguration config)
    {
        if (!IsValid())
        {
            Debug.LogError("[MeshSwapController] Cannot apply visual upgrade - no visual root assigned!");
            return;
        }
        
        if (config == null)
        {
            Debug.LogError("[MeshSwapController] UpgradeConfiguration is null!");
            return;
        }
        
        if (config.upgradeType != UpgradeType.MeshSwap)
        {
            Debug.LogWarning($"[MeshSwapController] Config upgrade type is {config.upgradeType}, expected MeshSwap. Aborting.");
            return;
        }
        
        GameObject newMeshPrefab = upgradeLevel == 1 
            ? config.level1MeshPrefab 
            : config.level2MeshPrefab;
        
        if (newMeshPrefab == null)
        {
            Debug.LogError($"[MeshSwapController] No mesh prefab configured for level {upgradeLevel}!");
            return;
        }
        
        SwapMesh(newMeshPrefab);
    }
    
    private void SwapMesh(GameObject newMeshPrefab)
    {
        if (currentVisual == null)
        {
            Debug.LogError("[MeshSwapController] Current visual is null!");
            return;
        }
        
        // Store the transform values
        Vector3 localPos = currentVisual.transform.localPosition;
        Quaternion localRot = currentVisual.transform.localRotation;
        Vector3 localScale = currentVisual.transform.localScale;
        
        // Destroy the old visual
        Destroy(currentVisual);
        
        // Instantiate the new visual
        currentVisual = Instantiate(newMeshPrefab, transform);
        currentVisual.transform.localPosition = localPos;
        currentVisual.transform.localRotation = localRot;
        currentVisual.transform.localScale = localScale;
        
        visualRoot = currentVisual.transform;
        
        Debug.Log($"[MeshSwapController] Successfully swapped mesh to {newMeshPrefab.name}");
    }
    
    public bool IsValid()
    {
        return isInitialized && visualRoot != null;
    }
    
    public void SetVisualRoot(Transform root)
    {
        visualRoot = root;
        if (visualRoot != null)
        {
            currentVisual = visualRoot.gameObject;
            isInitialized = true;
        }
    }
}