using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FrontalShield : MonoBehaviour
{
    public Transform ringRoot;
    public Transform center; 

    [Header("Planet / Globe")]
    public Transform planetCenter;
    public string planetCenterTag = "PlanetCenter";

    [Header("Idle Orbit")]
    public float idleSpinDegPerSec = 90f;

    [Header("Alert and Aim")]
    [Tooltip("How quickly the ring rotates toward a threat")]
    public float alertLerpSpeed = 8f;
    [Tooltip("Min time to keep aiming toward last seen threat")]
    public float alertHold = 0.6f;

    [Header("Projectile Filtering")]
    public string bulletLayerName = "Bullet";
    [HideInInspector] public int bulletLayer = -2; 

    [Header("Blocking")]
    public bool destroyProjectileOnHit = true;

    [Header("VFX")]
    public GameObject breakVfxPrefab;
    public float vfxLifetime = 1.5f;

    [Header("Sensor")]
    public float sensorRadius = 0.5f;              

    // All segments on enemy prefab 
    readonly List<ShieldSegment> _segments = new List<ShieldSegment>(); 
    // Trigger used to aim at incoming bullets
    SphereCollider _sensor;
    // Kinematic RB to support triggers reliably
    Rigidbody _rb; 
    // Counts down while aiming at a threat
    float _alertTimer;      
    // Last direction the ring must face
    Vector3 _desiredForwardWorld;
    // True once all segments are depleted
    bool _broken;                                   

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (!_rb)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        if (!center) center = transform;
        if (!ringRoot)
        {
            if (transform.childCount > 0) ringRoot = transform.GetChild(0);
            if (!ringRoot) ringRoot = transform;
        }

        // Get planet center by tag once if not assigned
        if (!planetCenter && !string.IsNullOrEmpty(planetCenterTag))
        {
            var tagged = GameObject.FindGameObjectWithTag(planetCenterTag);
            if (tagged) planetCenter = tagged.transform;
        }

        // Resolve bullet layer and warn if missing
        bulletLayer = LayerMask.NameToLayer(bulletLayerName);
        if (bulletLayer == -1)
        {
            bulletLayer = -2; 
            Debug.LogError($"[Shield] Bullet layer '{bulletLayerName}' not found.");
        }

        // Collect and bind all ShieldSegment children
        _segments.Clear();
        ringRoot.GetComponentsInChildren(true, _segments);
        foreach (var s in _segments)
        {
            // Give segment a back-reference to the shield
            s.Bind(this);  
            // Ensure each segment starts enabled
            s.SetActive(true); 
        }
        // If no segments = the shield starts broken
        _broken = _segments.Count == 0; 

        // Spherical trigger used for aim
        _sensor = GetComponent<SphereCollider>();
        if (!_sensor) _sensor = gameObject.AddComponent<SphereCollider>();
        _sensor.isTrigger = true;
        _sensor.radius = Mathf.Max(0.1f, sensorRadius);

        // Align ring to planet radial up
        _desiredForwardWorld = ringRoot.forward;
        var up = GetRadialUp();
        var fwd = ProjectOnPlaneNormalized(_desiredForwardWorld, up);
        ringRoot.rotation = Quaternion.LookRotation(fwd, up);
    }

    void Update()
    {
        if (_broken) return;
        
        // Orbit axis, radial from planet center
        Vector3 up = GetRadialUp(); 

        if (_alertTimer <= 0f)
        {
            // Spin around radial up at a constant rate
            ringRoot.Rotate(up, idleSpinDegPerSec * Time.deltaTime, Space.World);

            // Keep forward constrained to the tangent plane
            var currentFwd = ProjectOnPlaneNormalized(ringRoot.forward, up);
            ringRoot.rotation = Quaternion.LookRotation(currentFwd, up);
        }
        else
        {
            // Smoothly rotate to face the last seen projectile direction
            Vector3 targetFwd = ProjectOnPlaneNormalized(_desiredForwardWorld, up);
            if (targetFwd.sqrMagnitude > 1e-6f)
            {
                Quaternion target = Quaternion.LookRotation(targetFwd, up);
                ringRoot.rotation = Quaternion.Slerp(ringRoot.rotation, target, alertLerpSpeed * Time.deltaTime);
            }
            // Count down alert hold.
            _alertTimer -= Time.deltaTime; 
        }
    }

    // If a projectile enters/stays in range, set aim toward it.
    void OnTriggerEnter(Collider other) { if (IsProjectile(other)) FaceToward(other.bounds.center); }
    void OnTriggerStay (Collider other) { if (IsProjectile(other)) FaceToward(other.bounds.center); }
    
    // Called by a ShieldSegment when it is hit by a collider.
    internal void HandleSegmentHit(ShieldSegment segment, Collider other)
    {
        if (_broken) return;
        if (!IsProjectile(other)) return;

        // Destroy the projectile on contact
        if (destroyProjectileOnHit)
            Destroy(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);

        var hitPos = other.bounds.center;

        // Chooses the segment whose forward is closest to impact direction
        Vector3 flatDir = ProjectOnPlaneNormalized(hitPos - center.position, GetRadialUp());
        int removeIndex = GetClosestSegmentIndexFromDirection(flatDir);
        
        // Tells segment to visually deplete.
        if (removeIndex >= 0) _segments[removeIndex].Deplete(hitPos); 

        // If no segments remain = shields are depleted, otherwise keep aiming at the threat.
        if (CountActiveSegments() == 0) BreakShield();
        else FaceToward(hitPos);
    }

    // Layer check 
    bool IsProjectile(Collider other) => other.gameObject.layer == bulletLayer;

  
    // Aim ring towards a projectile and refresh the alert timer.
    void FaceToward(Vector3 worldPos)
    {
        _desiredForwardWorld = (worldPos - center.position);
        _alertTimer = Mathf.Max(_alertTimer, alertHold);
    }

    // Returns up relative to the globe
    
    public Vector3 GetRadialUp()
    {
        if (planetCenter)
        {
            Vector3 up = (center.position - planetCenter.position);
            if (up.sqrMagnitude > 1e-6f) return up.normalized;
        }
        return Vector3.up;
    }

    // Returns a safe Vector3.forward if v is nearly parallel to planeNormal.
    static Vector3 ProjectOnPlaneNormalized(Vector3 v, Vector3 planeNormal)
    {
        Vector3 r = v - Vector3.Dot(v, planeNormal) * planeNormal;
        float m2 = r.sqrMagnitude;
        return m2 > 1e-10f ? r / Mathf.Sqrt(m2) : Vector3.forward;
    }

    
    // Finds the closest active segment that matches flatDir.
    int GetClosestSegmentIndexFromDirection(Vector3 flatDir)
    {
        if (_segments.Count == 0) return -1;

        int best = -1;
        float bestDot = -1f;
        Vector3 up = GetRadialUp();

        for (int i = 0; i < _segments.Count; i++)
        {
            if (!_segments[i].IsActive) continue;
            Vector3 fwd = _segments[i].Forward;
            fwd = ProjectOnPlaneNormalized(fwd, up);
            float d = Vector3.Dot(fwd, flatDir); 
            if (d > bestDot) { bestDot = d; best = i; }
        }
        return best;
    }

    // Counts how many segments are still active
    int CountActiveSegments()
    {
        int c = 0; foreach (var s in _segments) if (s.IsActive) c++; return c;
    }
    
    // Disables all segments, plays VFX once, disables sensor and script.
    void BreakShield()
    {
        if (_broken) return;
        _broken = true;

        foreach (var s in _segments) s.SetActive(false);

        Vector3 fxPos = center ? center.position : transform.position;

        if (breakVfxPrefab)
        {
            var fx = Instantiate(breakVfxPrefab, fxPos, Quaternion.identity);
            Destroy(fx, vfxLifetime);
        }

        if (_sensor) _sensor.enabled = false; 
        // stop Update() once broken.
        enabled = false; 
    }
}
