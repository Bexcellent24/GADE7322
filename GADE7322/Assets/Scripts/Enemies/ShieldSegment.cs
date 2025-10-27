using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ShieldSegment : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Mesh root for this fragment ")]
    public GameObject visualRoot;

    [Header("Hit VFX")]
    [Tooltip("Spawned when this fragment is removed.")]
    public GameObject hitVfxPrefab;
    public float hitVfxLifetime = 1.0f;
    [Tooltip("Offset along this fragment's forward")]
    public float hitVfxForwardOffset = 0.05f;

    BoxCollider _col;
    FrontalShield _shield;
    bool _active = true;

    public Vector3 Forward => transform.forward;
    public bool IsActive => _active;

    void Reset()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;
    }

    void Awake()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;

        if (!visualRoot)
        {
            var mr = GetComponentInChildren<MeshRenderer>(true);
            if (mr) visualRoot = mr.gameObject;
        }
    }

    public void Bind(FrontalShield controller)
    {
        _shield = controller;
        SetActive(true);
    }

    public void SetActive(bool on)
    {
        _active = on;
        if (_col) _col.enabled = on;
        if (visualRoot) visualRoot.SetActive(on);
    }

    // Called by the controller when this fragment is chosen to be removed
    public void Deplete(Vector3 worldHitPoint)
    {
        if (!_active) return;

        Vector3 up = _shield ? _shield.GetRadialUp() : Vector3.up;
        Vector3 outward = Forward.sqrMagnitude > 1e-6f ? Forward : (transform.position - (_shield ? _shield.transform.position : Vector3.zero)).normalized;

        Vector3 spawnPos;
        if (hitVfxPrefab)
        {
            spawnPos = worldHitPoint;
            // Project hit to be near the segment plane
            if ((spawnPos - transform.position).sqrMagnitude > 4f) 
                spawnPos = transform.position + outward * hitVfxForwardOffset;
        }
        else
        {
            spawnPos = transform.position + outward * hitVfxForwardOffset;
        }

        Quaternion spawnRot = Quaternion.LookRotation(outward, up);

        if (hitVfxPrefab)
        {
            var fx = Object.Instantiate(hitVfxPrefab, spawnPos, spawnRot);
            Object.Destroy(fx, hitVfxLifetime);
        }

        // Disable this fragment 
        SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_active || _shield == null) return;
        // Controller validates the projectile layer and chooses which fragment to deplete
        _shield.HandleSegmentHit(this, other);
    }
}
