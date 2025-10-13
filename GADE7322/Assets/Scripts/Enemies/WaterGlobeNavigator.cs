using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterGlobeNavigator : MonoBehaviour
{
    [Header("Enemy Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeedDeg = 180f;
    [SerializeField] private float surfaceSnap = 0.15f;
    [SerializeField] private float hoverOffset = 0.0f;

    [Header("Enemy Avoidance")]
    [SerializeField] private LayerMask landMask;
    [SerializeField] private float lookAhead = 3.0f;
    [SerializeField] private float probeRadius = 0.35f;
    [SerializeField] private float minClearance = 0.25f;

    [Header("Probe Origin")]
    [SerializeField] private Transform probeAnchor;
    [SerializeField] private float probeSkinUp = 0.025f;
    [SerializeField] private Vector3 probeLocalOffset = new Vector3(0f, -0.20f, 0.0f);

    [Header("Avoidance (height fallback)")]
    [SerializeField] private bool useHeightFallback = true;
    [SerializeField] private float waterBias = 0f;
    [SerializeField] private float heightProbeDistance = -1f;
    [Range(1, 6)] [SerializeField] private int heightSamples = 3;

    [Header("Steering Search")]
    [SerializeField] private float[] yawCandidates = new float[] { 0f, 30f, -30f, 60f, -60f };
    [SerializeField] private float goalWeight = 1.0f;
    [SerializeField] private float penaltyWeight = 1.0f;

    [Header("Water Lock")]
    [SerializeField] private bool enforceWaterLock = true;
    [Range(2f, 20f)] [SerializeField] private float waterLockStepDeg = 8f;
    [Range(1, 8)] [SerializeField] private int waterLockSteps = 5;
    [SerializeField] private bool pullBackToLastWater = true;

    [Header("Safety")]
    [SerializeField] private bool preventLandPenetration = true;
    [SerializeField] private float hardPushStrength = 4f;

    private WaterWorldManager _worldManager;
    private Vector3 _vel;
    private Vector3 _lastWaterUp;

    // Public properties for dynamic configuration
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float TurnSpeedDeg
    {
        get => turnSpeedDeg;
        set => turnSpeedDeg = value;
    }

    public float HoverOffset
    {
        get => hoverOffset;
        set => hoverOffset = value;
    }

    Vector3 Center => _worldManager?.PlanetCenter?.position ?? Vector3.zero;

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

        probeSkinUp = 0.025f;
        probeLocalOffset = new Vector3(0f, -0.20f, 0.0f);
    }

    void OnEnable()
    {
        _worldManager = WaterWorldManager.Instance;
        if (!_worldManager)
        {
            Debug.LogWarning("[WaterGlobeNavigator] No WaterWorldManager found!");
            enabled = false;
            return;
        }

        var center = Center;
        var up = (transform.position - center).normalized;
        float targetRadius = _worldManager.WaterRadius + hoverOffset;
        var p = center + up * targetRadius;
        transform.position = p;

        if (_worldManager.Planet && useHeightFallback)
            _lastWaterUp = _worldManager.Planet.IsWaterDirection(up, waterBias) ? up : Vector3.zero;
    }

    void LateUpdate()
    {
        if (!_worldManager) return;

        var pos = transform.position;
        var center = Center;

        Vector3 up = (pos - center).normalized;
        float targetRadius = _worldManager.WaterRadius + hoverOffset;
        Vector3 onSurface = center + up * targetRadius;
        pos = Vector3.MoveTowards(pos, onSurface, surfaceSnap);
        up = (pos - center).normalized;

        Vector3 goalPoint = center + _worldManager.GoalPoleDir.normalized * targetRadius;
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
            Vector3 origin = GetProbeOrigin(newPos, newUp, newFwd);
            if (Physics.SphereCast(origin, probeRadius, newUp, out var hit, 0.20f, landMask, QueryTriggerInteraction.Collide))
            {
                Vector3 tangentAway = Vector3.ProjectOnPlane(hit.normal, newUp).normalized;
                newPos += tangentAway * hardPushStrength * Time.deltaTime;

                newUp = (newPos - center).normalized;
                newPos = center + newUp * targetRadius;
            }
        }

        if (enforceWaterLock && _worldManager.Planet && useHeightFallback)
        {
            if (!_worldManager.Planet.IsWaterDirection(newUp, waterBias))
            {
                Vector3 fwdTangent = newFwd;
                if (fwdTangent.sqrMagnitude < 1e-6f)
                    fwdTangent = Vector3.ProjectOnPlane(_worldManager.GoalPoleDir, newUp).normalized;
                if (fwdTangent.sqrMagnitude < 1e-6f)
                    fwdTangent = Vector3.Cross(newUp, Vector3.right).normalized;

                Vector3 right = Vector3.Cross(newUp, fwdTangent).normalized;
                bool fixedIt = false;

                for (int k = 1; k <= waterLockSteps && !fixedIt; k++)
                {
                    float ang = waterLockStepDeg * k;
                    Vector3 upL = Quaternion.AngleAxis(+ang, right) * newUp;
                    Vector3 upR = Quaternion.AngleAxis(-ang, right) * newUp;

                    if (_worldManager.Planet.IsWaterDirection(upL, waterBias))
                    {
                        newUp = upL.normalized;
                        fixedIt = true;
                        break;
                    }
                    if (_worldManager.Planet.IsWaterDirection(upR, waterBias))
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
                    newFwd = Vector3.ProjectOnPlane(_worldManager.GoalPoleDir, newUp).normalized;
                if (newFwd.sqrMagnitude < 1e-6f)
                    newFwd = Vector3.Cross(newUp, Vector3.right).normalized;
            }

            if (_worldManager.Planet.IsWaterDirection(newUp, waterBias))
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

        if (useHeightFallback && _worldManager?.Planet)
        {
            float L = heightProbeDistance > 0f ? heightProbeDistance : lookAhead;
            int N = Mathf.Max(1, heightSamples);
            float localPenalty = 0f;

            for (int i = 1; i <= N; i++)
            {
                float t = (float)i / (N + 1);
                Vector3 samplePos = origin + dir * (L * t);
                Vector3 radialDir = (samplePos - Center).normalized;

                bool water = _worldManager.Planet.IsWaterDirection(radialDir, waterBias);
                if (!water)
                {
                    localPenalty += Mathf.Lerp(1.0f, 0.25f, t);
#if UNITY_EDITOR
                    Debug.DrawLine(origin, samplePos, new Color(1f, 0f, 1f, 1f), 0f, false);
#endif
                }
            }
            p += localPenalty / N;

            Vector3 endUp = (end - Center).normalized;
            if (!_worldManager.Planet.IsWaterDirection(endUp, waterBias))
                p += 0.5f;
        }

        return p;
    }

    Vector3 GetProbeOrigin(Vector3 pos, Vector3 up, Vector3 fwd)
    {
        if (probeAnchor) return probeAnchor.position;

        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.ProjectOnPlane(_worldManager.GoalPoleDir, up).normalized;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.Cross(up, Vector3.right).normalized;

        Vector3 right = Vector3.Cross(up, fwd).normalized;

        Vector3 origin = pos + up * probeSkinUp;
        origin += right * probeLocalOffset.x;
        origin += up    * probeLocalOffset.y;
        origin += fwd   * probeLocalOffset.z;

        return origin;
    }

    static Vector3 SlerpOnPlane(Vector3 from, Vector3 to, Vector3 planeNormal, float maxAngle)
    {
        from = Vector3.ProjectOnPlane(from, planeNormal).normalized;
        to   = Vector3.ProjectOnPlane(to,   planeNormal).normalized;

        float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f));
        if (ang < 1e-4f) return to;

        float t = Mathf.Min(1f, maxAngle / ang);
        Vector3 axis = Vector3.Cross(from, to);
        if (axis.sqrMagnitude < 1e-6f) axis = planeNormal;
        return Quaternion.AngleAxis(ang * t * Mathf.Rad2Deg, axis.normalized) * from;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_worldManager) _worldManager = WaterWorldManager.Instance;
        if (!_worldManager) return;

        Gizmos.color = Color.cyan;
        Vector3 center = Center;
        Vector3 up = (transform.position - center).normalized;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;

        Vector3 origin = Application.isPlaying
            ? GetProbeOrigin(transform.position, up, fwd)
            : transform.position + up * probeSkinUp;

        Gizmos.DrawLine(origin, origin + fwd * lookAhead);

        Vector3 end = origin + fwd * lookAhead;
        UnityEditor.Handles.color = new Color(0, 1, 1, 0.2f);
        UnityEditor.Handles.DrawWireDisc(end, up, probeRadius);
        UnityEditor.Handles.color = new Color(1, 1, 0, 0.2f);
        UnityEditor.Handles.DrawWireDisc(origin, up, probeRadius);
    }
#endif
}