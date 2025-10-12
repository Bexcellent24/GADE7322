using UnityEngine;

public class FrontalShield : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Optional: assign your visual root (mesh or arc visual). If null, will try first child MeshRenderer.")]
    public GameObject visualRoot;

    [Tooltip("Destroy the projectile on hit?")]
    public bool destroyProjectileOnHit = true;

    [Tooltip("Layer used by bullets (must match your Bullet layer).")]
    public string bulletLayerName = "Bullet";

    [Header("FX (optional)")]
    public GameObject breakVfxPrefab;
    public AudioClip breakSfx;
    public float vfxLifetime = 1.5f;

    [HideInInspector] public int bulletLayer;
    BoxCollider[] _segments;
    bool _broken;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        _segments = GetComponentsInChildren<BoxCollider>(includeInactive: true);

        bulletLayer = LayerMask.NameToLayer(bulletLayerName);
        if (bulletLayer == -1)
        {
            Debug.LogWarning($"[Shield] Bullet layer '{bulletLayerName}' not found. Falling back to any layer.");
        }

        if (!visualRoot)
        {
            var mr = GetComponentInChildren<MeshRenderer>(true);
            if (mr) visualRoot = mr.gameObject;
        }

        SetSegmentsEnabled(true);
        SetVisualEnabled(true);
        _broken = false;
    }

    /*public void BreakShield(GameObject hitter = null)
    {
        if (_broken) return;
        _broken = true;

        Debug.Log("[Shield] Broken!");

        // FX
        if (breakVfxPrefab)
        {
            var fx = Instantiate(breakVfxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, vfxLifetime);
        }
        if (breakSfx) AudioSource.PlayClipAtPoint(breakSfx, transform.position);

        // Disable hit volumes + visual
        SetSegmentsEnabled(false);
        SetVisualEnabled(false);
    }

*/
    
    public void HandleHit(Collider other)
    {
        if (_broken) return;

        // Optional layer check
        if (bulletLayer != -1 && other.gameObject.layer != bulletLayer) return;

        Debug.Log($"[Shield] Hit by {other.name} → BREAK");

        if (destroyProjectileOnHit)
            Destroy(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);

       // BreakShield(other.gameObject);
    }

    void SetSegmentsEnabled(bool enabled)
    {
        if (_segments == null) return;
        foreach (var c in _segments)
        {
            if (!c) continue;
            c.enabled = enabled;
            c.isTrigger = true; // ensure trigger
        }
    }

    void SetVisualEnabled(bool enabled)
    {
        if (!visualRoot) return;
        visualRoot.SetActive(enabled);
    }
}
