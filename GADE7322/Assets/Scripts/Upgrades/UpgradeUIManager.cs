using UnityEngine;
using TMPro;
using UnityEngine.UI;


// Manages the upgrade UI panel that appears when selecting an upgradeable unit.
// Singleton pattern for easy access from other scripts.

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI attackRateText;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Colors")]
    [SerializeField] private Color upgradedStatColor = Color.green;
    [SerializeField] private Color canAffordColor = Color.white;
    [SerializeField] private Color cannotAffordColor = Color.red;
    
    private Actor currentActor;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Hide panel in the beginning
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
        
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
           
    }
    
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    public void ShowUpgradePanel(Actor actor)
    {
        if (actor == null)
        {
            Debug.LogWarning("[UpgradeUIManager] Cannot show panel for null actor!");
            return;
        }
        
        currentActor = actor;
        
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
        
        RefreshUpgradePanel();
    }
    
    public void HideUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
        
        currentActor = null;
    }
    
    public void RefreshUpgradePanel()
    {
        if (currentActor == null)
        {
            Debug.LogWarning("[UpgradeUIManager] Cannot refresh - no actor selected!");
            return;
        }
        
        var upgradeController = currentActor.upgradeController;
        if (upgradeController == null)
        {
            Debug.LogWarning("[UpgradeUIManager] Actor has no UpgradeController!");
            HideUpgradePanel();
            return;
        }
        
        // Update unit name - use display name from config if available
        if (unitNameText != null)
        {
            string displayName = currentActor.gameObject.name; // fallback
            if (currentActor.upgradeConfig != null && !string.IsNullOrEmpty(currentActor.upgradeConfig.unitDisplayName))
            {
                displayName = currentActor.upgradeConfig.unitDisplayName;
            }
            unitNameText.text = displayName;
        }
        
        // Update level display
        if (levelText != null)
        {
            levelText.text = $"Level {upgradeController.CurrentUpgradeLevel}/{upgradeController.MaxUpgradeLevel}";
        }
        
        // Check if max level
        bool isMaxLevel = upgradeController.CurrentUpgradeLevel >= upgradeController.MaxUpgradeLevel;
        
        if (isMaxLevel)
        {
            // Show max level UI
            DisplayMaxLevel();
        }
        else
        {
            // Show upgrade UI
            DisplayUpgradeOption(upgradeController);
        }
    }
    
    private void DisplayUpgradeOption(UpgradeController upgradeController)
    {
        var currentStats = upgradeController.GetCurrentStats();
        var nextStats = upgradeController.GetNextUpgradeStats();
        int cost = upgradeController.GetUpgradeCost();
        bool canAfford = upgradeController.CanUpgrade();
        
        // Get the config to check what actually upgrades
        var config = currentActor.upgradeConfig;
        int nextLevel = upgradeController.CurrentUpgradeLevel + 1;
        
        // Update stats with upgrade preview 
        if (damageText != null)
        {
            if (config != null && config.DoesDamageUpgrade(nextLevel))
            {
                damageText.text = $"Damage: {currentStats.damage:F1} → <color=#{ColorUtility.ToHtmlStringRGB(upgradedStatColor)}>{nextStats.damage:F1}</color>";
            }
            else
            {
                damageText.text = $"Damage: {currentStats.damage:F1}";
            }
        }
        
        if (rangeText != null)
        {
            if (config != null && config.DoesRangeUpgrade(nextLevel))
            {
                rangeText.text = $"Range: {currentStats.range:F1} → <color=#{ColorUtility.ToHtmlStringRGB(upgradedStatColor)}>{nextStats.range:F1}</color>";
            }
            else
            {
                rangeText.text = $"Range: {currentStats.range:F1}";
            }
        }
        
        if (attackRateText != null)
        {
            if (config != null && config.DoesAttackRateUpgrade(nextLevel))
            {
                attackRateText.text = $"Attack Rate: {currentStats.attackRate:F1} → <color=#{ColorUtility.ToHtmlStringRGB(upgradedStatColor)}>{nextStats.attackRate:F1}</color>";
            }
            else
            {
                attackRateText.text = $"Attack Rate: {currentStats.attackRate:F1}";
            }
        }
        
        if (healthText != null)
        {
            if (config != null && config.DoesHealthUpgrade(nextLevel))
            {
                healthText.text = $"Health: {currentStats.health} → <color=#{ColorUtility.ToHtmlStringRGB(upgradedStatColor)}>{nextStats.health}</color>";
            }
            else
            {
                healthText.text = $"Health: {currentStats.health}";
            }
        }
        
        // Update cost display
        if (costText != null)
        {
            Color costColor = canAfford ? canAffordColor : cannotAffordColor;
            costText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(costColor)}>{cost}</color>";
        }
        
        // Update button
        if (upgradeButton != null)
        {
            upgradeButton.interactable = canAfford;
            
            if (buttonText != null)
            {
                buttonText.text = canAfford ? "UPGRADE" : "CANT AFFORD";
            }
        }
    }
    
    private void DisplayMaxLevel()
    {
        var currentStats = currentActor.upgradeController.GetCurrentStats();
        
        // Show current stats only (no arrows)
        if (damageText != null)
            damageText.text = $"Damage: {currentStats.damage:F1}";
        
        if (rangeText != null)
            rangeText.text = $"Range: {currentStats.range:F1}";
        
        if (attackRateText != null)
            attackRateText.text = $"Attack Rate: {currentStats.attackRate:F1}";
        
        if (healthText != null)
            healthText.text = $"Health: {currentStats.health}";
        
        // Hide or disable cost
        if (costText != null)
            costText.text = "";
        
        // Disable button and show max level message
        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
            
            if (buttonText != null)
                buttonText.text = "MAX LEVEL";
        }
    }
    
    public void OnUpgradeButtonClicked()
    {
        
        if (currentActor == null || currentActor.upgradeController == null)
        {
            Debug.LogWarning("[UpgradeUIManager] Cannot upgrade - no valid actor/controller!");
            return;
        }
        
        if (!currentActor.upgradeController.CanUpgrade())
        {
            Debug.LogWarning("[UpgradeUIManager] Cannot upgrade - check failed!");
            return;
        }
        
        // Apply the upgrade
        Debug.LogWarning("[UpgradeUIManager] Telling Controller to upgrade!");
        currentActor.upgradeController.ApplyUpgrade();
        
    }
    

    // Call this when currency changes to update the UI if panel is open
    public void OnCurrencyChanged()
    {
        if (currentActor != null && upgradePanel != null && upgradePanel.activeSelf)
        {
            RefreshUpgradePanel();
        }
    }
}