using UnityEngine;

public class TowerHPToShader : MonoBehaviour
{
    [Header("Lookup")]
    [Tooltip("Tag on Main Tower")]
    public string mainTowerTag = "MainTower";

    [Header("Shader")]
    public string shaderHPParam = "_TowerHealth01";

    Health _health;

    void Awake()
    {
        // Try find by tag 
        var tower = GameObject.FindGameObjectWithTag(mainTowerTag);
        if (!tower)
        {
            // Fallback: scan all Healths and pick one that has the tag
            var all = FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var h in all)
            {
                if (h && h.CompareTag(mainTowerTag)) { tower = h.gameObject; break; }
            }
        }

        if (!tower)
        {
            Debug.LogError($"[TowerHPToShader] Could not find an object with tag '{mainTowerTag}'.");
            enabled = false; return;
        }

        _health = tower.GetComponent<Health>();
        if (!_health)
        {
            Debug.LogError($"[TowerHPToShader] Object '{tower.name}' with tag '{mainTowerTag}' is missing a Health component.");
            enabled = false; return;
        }
    }

    void OnEnable()
    {
        if (_health != null)
        {
            _health.OnHealthChanged += HandleHealthChanged;
            // Push initial value immediately
            HandleHealthChanged();
        }
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= HandleHealthChanged;
    }

    void HandleHealthChanged()
    {
        // Compute 0..1 ratio 
        float max = Mathf.Max(1, _health.Max);
        float t = Mathf.Clamp01((float)_health.Current / max);

        // Send to all shaders
        Shader.SetGlobalFloat(shaderHPParam, t);
    }
}
