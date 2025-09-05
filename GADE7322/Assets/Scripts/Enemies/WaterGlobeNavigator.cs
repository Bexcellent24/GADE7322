using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterGlobeNavigator : MonoBehaviour
{
    [Header("Planet")]
    public Transform planetCenter;
    public float waterRadius = 10f;
    public Vector3 goalPoleDir = Vector3.down;

    [Header("Enemy Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeedDeg = 180f;
    public float surfaceSnap = 0.15f;
    public float hoverOffset = 0.0f;

    [Header("Enemy Avoidance")]
    public LayerMask landMask;
    public float lookAhead = 3.0f;
    public float probeRadius = 0.35f;
    public float minClearance = 0.25f;

    [Header("Probe Origin")]
    [Tooltip("If set, the probe ray origin will be taken from this transform's position.")]
    public Transform probeAnchor;
    [Tooltip("Small outward skin along radial up to avoid starting inside geometry.")]
    public float probeSkinUp = 0.025f;
    [Tooltip("Local-tangent-space offset of the probe origin: (right, up=radial, forward). Use negative Y to start lower (toward planet center).")]
    public Vector3 probeLocalOffset = new Vector3(0f, -0.20f, 0.0f);

    [Header("Avoidance (height fallback)")]
    public MarchingCubesPlanet planet;
    public bool useHeightFallback = true;
    public float waterBias = 0f;
    public float heightProbeDistance = -1f;
    [Range(1, 6)] public int heightSamples = 3;

    [Header("Steering Search")]
    public float[] yawCandidates = new float[] { 0f, 30f, -30f, 60f, -60f };
    public float goalWeight = 1.0f;
    public float penaltyWeight = 1.0f;

    [Header("Water Lock")]
    public bool enforceWaterLock = true;
    [Range(2f, 20f)] public float waterLockStepDeg = 8f;
    [Range(1, 8)] public int waterLockSteps = 5;
    public bool pullBackToLastWater = true;

    [Header("Safety")]
    public bool preventLandPenetration = true;
    public float hardPushStrength = 4f;

    Vector3 _vel;
    Vector3 _lastWaterUp;

    Vector3 Center => planetCenter ? planetCenter.position : Vector3.zero;

    void Reset()
    {
        int land = LayerMask.NameToLayer("Land");
        landMask = land >= 0 ? (LayerMask)(1 << land) : Physics.DefaultRaycastLayers;

        lookAhead = 3.0f;
        probeRadius = 0.35f;
        minClearance = 0.25f;
        surfaceSnap = 0.15f;

        useHeightFallback = true;
        waterBias = 0f;
        heightSamples = 3;

        yawCandidates = new float[] { 0f, 30f, -30f, 60f, -60f };
        goalWeight = 1.0f;
        penaltyWeight = 1.0f;

        enforceWaterLock = true;
        waterLockStepDeg = 8f;
        waterLockSteps = 5;
        pullBackToLastWater = true;

        // NEW: sensible probe defaults
        probeSkinUp = 0.025f;
        probeLocalOffset = new Vector3(0f, -0.20f, 0.0f);
    }

    void Awake()
    {
        if (landMask.value == 0)
        {
            int land = LayerMask.NameToLayer("Land");
            landMask = land >= 0 ? (LayerMask)(1 << land) : Physics.DefaultRaycastLayers;
        }
        if (!planet) planet = FindObjectOfType<MarchingCubesPlanet>();
    }

    void OnEnable()
    {
        var center = Center;
        var up = (transform.position - center).normalized;
        float targetRadius = waterRadius + hoverOffset;
        var p = center + up * targetRadius;
        transform.position = p;

        if (planet && useHeightFallback)
            _lastWaterUp = planet.IsWaterDirection(up, waterBias) ? up : Vector3.zero;
    }

    void LateUpdate()
    {
        var pos = transform.position;
        var center = Center;

        Vector3 up = (pos - center).normalized;
        float targetRadius = waterRadius + hoverOffset;
        Vector3 onSurface = center + up * targetRadius;
        pos = Vector3.MoveTowards(pos, onSurface, surfaceSnap);
        up = (pos - center).normalized;

        Vector3 goalPoint = center + goalPoleDir.normalized * targetRadius;
        Vector3 toGoal = goalPoint - pos;
        Vector3 desired = Vector3.ProjectOnPlane(toGoal, up).normalized;

        Vector3 steering = ChooseSteeringWithCost(pos, up, desired);

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (fwd.sqrMagnitude < 0.0001f) fwd = steering;

        float maxStep = turnSpeedDeg * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 newFwd = SlerpOnPlane(fwd, steering, up, maxStep).normalized;

        _vel = newFwd * moveSpeed;
        Vector3 newPos = pos + _vel * Time.deltaTime;

        Vector3 newUp = (newPos - center).normalized;
        newPos = center + newUp * targetRadius;

        if (preventLandPenetration)
        {
            // Use adjusted origin near the "bottom" for penetration guard too
            Vector3 origin = GetProbeOrigin(newPos, newUp, newFwd);
            // Cast outward slightly along up to catch immediate terrain
            if (Physics.SphereCast(origin, probeRadius, newUp, out var hit, 0.20f, landMask, QueryTriggerInteraction.Collide))
            {
                Vector3 tangentAway = Vector3.ProjectOnPlane(hit.normal, newUp).normalized;
                newPos += tangentAway * hardPushStrength * Time.deltaTime;

                newUp = (newPos - center).normalized;
                newPos = center + newUp * targetRadius;
            }
        }

        if (enforceWaterLock && planet && useHeightFallback)
        {
            if (!planet.IsWaterDirection(newUp, waterBias))
            {
                Vector3 fwdTangent = newFwd;
                if (fwdTangent.sqrMagnitude < 1e-6f)
                    fwdTangent = Vector3.ProjectOnPlane(goalPoleDir, newUp).normalized;
                if (fwdTangent.sqrMagnitude < 1e-6f)
                    fwdTangent = Vector3.Cross(newUp, Vector3.right).normalized;

                Vector3 right = Vector3.Cross(newUp, fwdTangent).normalized;
                bool fixedIt = false;

                for (int k = 1; k <= waterLockSteps && !fixedIt; k++)
                {
                    float ang = waterLockStepDeg * k;
                    Vector3 upL = Quaternion.AngleAxis(+ang, right) * newUp;
                    Vector3 upR = Quaternion.AngleAxis(-ang, right) * newUp;

                    if (planet.IsWaterDirection(upL, waterBias))
                    {
                        newUp = upL.normalized;
                        fixedIt = true;
                        break;
                    }
                    if (planet.IsWaterDirection(upR, waterBias))
                    {
                        newUp = upR.normalized;
                        fixedIt = true;
                        break;
                    }
                }

                if (!fixedIt && pullBackToLastWater && _lastWaterUp.sqrMagnitude > 0.1f)
                {
                    newUp = Vector3.Slerp(newUp, _lastWaterUp, 0.75f).normalized;
                }

                newPos = center + newUp * targetRadius;
                newFwd = Vector3.ProjectOnPlane(newFwd, newUp).normalized;
                if (newFwd.sqrMagnitude < 1e-6f)
                    newFwd = Vector3.ProjectOnPlane(goalPoleDir, newUp).normalized;
                if (newFwd.sqrMagnitude < 1e-6f)
                    newFwd = Vector3.Cross(newUp, Vector3.right).normalized;
            }

            if (planet.IsWaterDirection(newUp, waterBias))
                _lastWaterUp = newUp;
        }

        transform.position = newPos;
        transform.rotation = Quaternion.LookRotation(newFwd, newUp);
    }

    Vector3 ChooseSteeringWithCost(Vector3 pos, Vector3 up, Vector3 desired)
    {
        if (yawCandidates == null || yawCandidates.Length == 0)
            return desired;

        float bestScore = float.NegativeInfinity;
        Vector3 bestDir = desired;

        for (int i = 0; i < yawCandidates.Length; i++)
        {
            float yaw = yawCandidates[i];
            Vector3 cand = Quaternion.AngleAxis(yaw, up) * desired;
            float penalty = ComputePenalty(pos, up, cand);
            float reward = Mathf.Clamp01(Vector3.Dot(cand, desired));
            float score = goalWeight * reward - penaltyWeight * penalty;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = cand;
            }
        }
        return bestDir.normalized;
    }

    float ComputePenalty(Vector3 pos, Vector3 up, Vector3 dir)
    {
        float p = 0f;

        // NEW: use adjustable origin (lowered / anchored)
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        Vector3 origin = GetProbeOrigin(pos, up, fwd);

        Vector3 end = origin + dir * lookAhead;
        Vector3 rayDir = (end - origin).normalized;

        if (Physics.SphereCast(origin, probeRadius, rayDir, out var hit, lookAhead, landMask, QueryTriggerInteraction.Collide))
        {
            float d = Mathf.Max(0.0001f, hit.distance);
            float t = 1f - Mathf.Clamp01((d - minClearance) / Mathf.Max(0.0001f, (lookAhead - minClearance)));
            p += Mathf.Lerp(0.25f, 1.0f, t);
#if UNITY_EDITOR
            Debug.DrawLine(origin, hit.point, Color.red, 0f, false);
#endif
        }

        if (useHeightFallback && planet)
        {
            float L = heightProbeDistance > 0f ? heightProbeDistance : lookAhead;
            int N = Mathf.Max(1, heightSamples);
            float localPenalty = 0f;

            for (int i = 1; i <= N; i++)
            {
                float t = (float)i / (N + 1);
                Vector3 samplePos = origin + dir * (L * t);
                Vector3 radialDir = (samplePos - Center).normalized;

                bool water = planet.IsWaterDirection(radialDir, waterBias);
                if (!water)
                {
                    localPenalty += Mathf.Lerp(1.0f, 0.25f, t);
#if UNITY_EDITOR
                    Debug.DrawLine(origin, samplePos, new Color(1f, 0f, 1f, 1f), 0f, false); // magenta
#endif
                }
            }
            p += localPenalty / N;

            Vector3 endUp = (end - Center).normalized;
            if (!planet.IsWaterDirection(endUp, waterBias))
                p += 0.5f;
        }

        return p;
    }

    // NEW: compute a stable probe origin that can be placed lower/forward or via anchor
    Vector3 GetProbeOrigin(Vector3 pos, Vector3 up, Vector3 fwd)
    {
        if (probeAnchor) return probeAnchor.position;

        // Build tangent basis
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.ProjectOnPlane(goalPoleDir, up).normalized;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.Cross(up, Vector3.right).normalized;

        Vector3 right = Vector3.Cross(up, fwd).normalized;

        // Start with a small outward skin, then apply local offsets
        Vector3 origin = pos + up * probeSkinUp;
        origin += right * probeLocalOffset.x;
        origin += up    * probeLocalOffset.y;   // negative pulls origin toward planet center
        origin += fwd   * probeLocalOffset.z;   // positive puts origin slightly ahead

        return origin;
    }

    // Great-circle-ish slerp constrained to the tangent plane (rotate around 'up').
    static Vector3 SlerpOnPlane(Vector3 from, Vector3 to, Vector3 planeNormal, float maxAngle)
    {
        from = Vector3.ProjectOnPlane(from, planeNormal).normalized;
        to   = Vector3.ProjectOnPlane(to,   planeNormal).normalized;

        float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f));
        if (ang < 1e-4f) return to;

        float t = Mathf.Min(1f, maxAngle / ang);
        Vector3 axis = Vector3.Cross(from, to);
        if (axis.sqrMagnitude < 1e-6f) axis = planeNormal; // degenerate
        return Quaternion.AngleAxis(ang * t * Mathf.Rad2Deg, axis.normalized) * from;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Center;
        Vector3 up = ((Application.isPlaying ? transform.position : transform.position) - center).normalized;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;

        Vector3 origin = Application.isPlaying
            ? GetProbeOrigin(transform.position, up, fwd)
            : transform.position + up * probeSkinUp; // editor preview

        // Visualize current forward probe with the adjusted origin
        Gizmos.DrawLine(origin, origin + fwd * lookAhead);

        Vector3 end = origin + fwd * lookAhead;
        UnityEditor.Handles.color = new Color(0, 1, 1, 0.2f);
        UnityEditor.Handles.DrawWireDisc(end, up, probeRadius);
        UnityEditor.Handles.color = new Color(1, 1, 0, 0.2f);
        UnityEditor.Handles.DrawWireDisc(origin, up, probeRadius);
    }
#endif
}
