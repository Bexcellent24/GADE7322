using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CrystalRingOrbiter : MonoBehaviour
{
    public enum AxisMode { WorldAxis, CenterUp, PlanetRadial, ViewForward, CenterLocalAxis }

    [Header("Center")]
    public Transform center;

    [Header("Rings")]
    public Transform innerRingParent;
    public Transform outerRingParent;

    [Header("Motion - degrees per second")]
    public float innerSpeedDeg = 45f;
    public float outerSpeedDeg = -30f;

    [Header("Axis Selection")]
    public AxisMode axisMode = AxisMode.CenterLocalAxis;
    public Vector3 orbitAxisWorld = Vector3.up;
    [Tooltip("Auto-find planet center by tag")]
    public Transform planetCenter;
    public string planetCenterTag = "PlanetCenter";
    public Camera viewCamera;
    public Vector3 centerLocalAxis = Vector3.up;

    [Tooltip("Freeze the resolved axis the first time it becomes valid.")]
    public bool freezeAxisOnceValid = true;

    [Tooltip("Keep each shard at its initial ring radius from center.")]
    public bool lockRadius = true;

    [Header("Orientation")]
    public bool faceOutward = true;
    public bool keepUpright = true;

    [Header("Misc")]
    public bool rebuildOnEnable = true;

    struct Ring
    {
        public List<Transform> items;
        public List<float> radii;
        public Transform parent;
        public float speedDeg;
    }

    Ring _inner, _outer;
    Vector3 _axisFrozen;
    bool _triedAutoFindPlanet;

    void Reset() => center = transform;

    void OnEnable()
    {
        if (!center) center = transform;

        _inner = new Ring { items = new List<Transform>(32), radii = new List<float>(32), parent = innerRingParent, speedDeg = innerSpeedDeg };
        _outer = new Ring { items = new List<Transform>(32), radii = new List<float>(32), parent = outerRingParent, speedDeg = outerSpeedDeg };

        if (rebuildOnEnable) RebuildSets();
    }

    public void RebuildSets()
    {
        // Keep speeds in sync with inspector values
        _inner.speedDeg = innerSpeedDeg;
        _outer.speedDeg = outerSpeedDeg;

        _inner.items.Clear(); _inner.radii.Clear();
        _outer.items.Clear(); _outer.radii.Clear();

        if (_inner.parent || _outer.parent)
        {
            BuildRing(ref _inner, _inner.parent);
            BuildRing(ref _outer, _outer.parent);
        }
        else
        {
            foreach (Transform t in transform)
            {
                if (!t || t == center || !t.gameObject.activeInHierarchy) continue;
                _inner.items.Add(t);
            }
        }

        _axisFrozen = Vector3.zero;

        if (lockRadius)
        {
            Vector3 axis = ResolveAxis(liveOnly: true);
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.up;
            axis.Normalize();

            CacheRadii(ref _inner, axis);
            CacheRadii(ref _outer, axis);
        }
    }
    
    // Add active children from a given parent to the ring's list.
    void BuildRing(ref Ring ring, Transform parent)
    {
        if (!parent) return;
        foreach (Transform t in parent)
        {
            if (t && t.gameObject.activeInHierarchy)
                ring.items.Add(t);
        }
    }
    
    // Stores each item's distance from center  onto the orbit plane.
    void CacheRadii(ref Ring ring, Vector3 axis)
    {
        ring.radii.Capacity = Mathf.Max(ring.radii.Capacity, ring.items.Count);
        ring.radii.Clear();
        Vector3 cpos = GetCenterPos();
        for (int i = 0; i < ring.items.Count; i++)
        {
            Transform t = ring.items[i];
            Vector3 onPlane = Vector3.ProjectOnPlane(t.position - cpos, axis);
            float r = onPlane.magnitude;
            ring.radii.Add(r > 1e-6f ? r : 0.01f);
        }
    }

    void Update()
    {
        if (!center) center = transform;

        // Compute current axis once per frame
        Vector3 axis = _axisFrozen.sqrMagnitude > 1e-12f ? _axisFrozen : ResolveAxis(liveOnly: false);
        if (axis.sqrMagnitude < 1e-8f) axis = Vector3.up;
        axis.Normalize();

        if (freezeAxisOnceValid && _axisFrozen.sqrMagnitude < 1e-12f)
            _axisFrozen = axis;

        float dt = Time.deltaTime;
        Vector3 cpos = GetCenterPos();

        // Advance both rings by their angular step
        OrbitRing(ref _inner, cpos, axis, _inner.speedDeg * dt);
        OrbitRing(ref _outer, cpos, axis, _outer.speedDeg * dt);
    }

    // Rotates all items in the ring around the axis via world space.
    void OrbitRing(ref Ring ring, Vector3 cpos, Vector3 axis, float deltaDeg)
    {
        if (ring.items.Count == 0 || Mathf.Approximately(deltaDeg, 0f)) return;

        Quaternion q = Quaternion.AngleAxis(deltaDeg, axis);

        for (int i = 0; i < ring.items.Count; i++)
        {
            Transform t = ring.items[i];
            if (!t) continue;
            
            // Project onto orbit plane to avoid axial movement
            Vector3 to = t.position - cpos;
            Vector3 onPlane = Vector3.ProjectOnPlane(to, axis);
            if (onPlane.sqrMagnitude < 1e-10f) onPlane = AnyPerpendicular(axis) * 0.01f;

            // Rotate around axis
            Vector3 rotated = q * onPlane;
            if (lockRadius && i < ring.radii.Count && ring.radii[i] > 1e-6f)
                rotated = rotated.normalized * ring.radii[i];

            t.position = cpos + rotated;

            if (faceOutward)
                OrientShard(t, rotated, axis);
        }
    }
    
    // Sets the shard's rotation to face its motion direction,
    void OrientShard(Transform t, Vector3 dir, Vector3 axis)
    {
        if (dir.sqrMagnitude < 1e-6f) return;

        if (keepUpright)
        {
            Vector3 up = axis; 
            Vector3 fwd = Vector3.ProjectOnPlane(dir, up);
            if (fwd.sqrMagnitude > 1e-6f)
                t.rotation = Quaternion.LookRotation(fwd.normalized, up);
        }
        else
        {
            t.rotation = Quaternion.LookRotation(dir.normalized, t.up);
        }
    }
    
    // Returns the orbit axis based on AxisMode.
    Vector3 ResolveAxis(bool liveOnly)
    {
        switch (axisMode)
        {
            case AxisMode.WorldAxis:
                return orbitAxisWorld;

            case AxisMode.CenterUp:
                return center ? center.up : Vector3.up;

            case AxisMode.CenterLocalAxis:
                return center ? center.TransformDirection(centerLocalAxis) : centerLocalAxis;

            case AxisMode.ViewForward:
            {
                var cam = viewCamera ? viewCamera : Camera.main;
                #if UNITY_EDITOR
                    if (!Application.isPlaying && !cam && SceneView.lastActiveSceneView != null)
                        cam = SceneView.lastActiveSceneView.camera;
                #endif
                return cam ? cam.transform.forward : Vector3.forward;
            }

            case AxisMode.PlanetRadial:
            {
                if (!planetCenter && !_triedAutoFindPlanet && !liveOnly)
                {
                    _triedAutoFindPlanet = true;
                    if (!string.IsNullOrEmpty(planetCenterTag))
                    {
                        var tagged = GameObject.FindWithTag(planetCenterTag);
                        if (tagged) planetCenter = tagged.transform;
                    }
                }

                if (center && planetCenter)
                    return (center.position - planetCenter.position).normalized;

                return center ? center.up : Vector3.up;
            }
        }
        return Vector3.up;
    }

    static Vector3 AnyPerpendicular(Vector3 n)
    {
        n = n.normalized;
        Vector3 a = Mathf.Abs(Vector3.Dot(n, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
        return Vector3.Cross(n, a).normalized;
    }

    Vector3 GetCenterPos() => center ? center.position : transform.position;
}
