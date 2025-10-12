using UnityEngine;

[RequireComponent(typeof(Health))]
public class SplitOnDeath : MonoBehaviour
{
    [Header("Split Settings")]
    [Tooltip("Prefab to spawn when this unit dies (e.g., your Light enemy prefab).")]
    public GameObject splitChildPrefab;

    [Min(2)] public int count = 2;

    [Header("Size of children")]
    [Tooltip("Child size relative to THIS unit's current size. 0.5 = half of this unit.")]
    [Range(0.05f, 2f)] public float relativeScale = 0.5f;

    [Header("Spawn spread")]
    [Range(0.0f, 2.0f)] public float burstRadius = 0.6f;

    [Header("Navigation Inheritance")]
    [Tooltip("Copy WaterGlobeNavigator settings from this unit to the children.")]
    public bool inheritNavigator = true;

    Health _hp;

    void OnEnable()
    {
        _hp = GetComponent<Health>();
        if (_hp != null) _hp.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (_hp != null) _hp.OnDeath -= HandleDeath;
    }

    void HandleDeath(IDamageable _)
    {
        if (splitChildPrefab == null)
        {
            Debug.LogWarning("[SplitOnDeath] No splitChildPrefab assigned. Skipping split.");
            return;
        }

        // Cache before destruction
        Vector3 center = transform.position;
        Vector3 up = transform.up;
        Transform parent = transform.parent;

        // Optional: source nav
        WaterGlobeNavigator srcNav = inheritNavigator ? GetComponent<WaterGlobeNavigator>() : null;

        for (int i = 0; i < count; i++)
        {
            // Offset on the local tangent so they don't overlap
            Vector3 tangent = Vector3.ProjectOnPlane(Random.onUnitSphere, up).normalized;
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;
            Vector3 spawnPos = center + tangent * burstRadius;

            // Surface-aligned rotation
            Quaternion rot = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, up).normalized,
                up
            );

            // Spawn
            GameObject child = Instantiate(splitChildPrefab, spawnPos, rot);

            // Parent to the SAME parent as the splitter so hierarchy scale matches,
            // but keep world position/rotation.
            if (parent != null)
                child.transform.SetParent(parent, true);

            // Apply scale relative to this unit's current size
            ApplyRelativeScale(child.transform, transform, parent, relativeScale);

            // Copy navigator context (after parenting/scale so snapping works nicely)
            if (srcNav != null)
            {
                var dstNav = child.GetComponent<WaterGlobeNavigator>() ?? child.AddComponent<WaterGlobeNavigator>();
                CopyNav(srcNav, dstNav);
            }
        }
    }

    static void ApplyRelativeScale(Transform child, Transform source, Transform commonParent, float factor)
    {
        // We want: child.worldScale = source.worldScale * factor (component-wise).
        Vector3 sourceWorld = source.lossyScale;
        Vector3 targetWorld = sourceWorld * factor;

        if (commonParent == null)
        {
            // No parent → local == world
            child.localScale = targetWorld;
            return;
        }

        // Convert desired world scale into local scale under the parent:
        Vector3 parentWorld = commonParent.lossyScale;
        child.localScale = new Vector3(
            SafeDiv(targetWorld.x, parentWorld.x),
            SafeDiv(targetWorld.y, parentWorld.y),
            SafeDiv(targetWorld.z, parentWorld.z)
        );
    }

    static float SafeDiv(float a, float b) => Mathf.Approximately(b, 0f) ? a : a / b;

    static void CopyNav(WaterGlobeNavigator src, WaterGlobeNavigator dst)
    {
        if (src == null || dst == null) return;

        dst.planetCenter        = src.planetCenter;
        dst.waterRadius         = src.waterRadius;
        dst.hoverOffset         = src.hoverOffset;
        dst.goalPoleDir         = src.goalPoleDir;
        dst.landMask            = src.landMask;

        dst.planet              = src.planet;
        dst.useHeightFallback   = src.useHeightFallback;
        dst.waterBias           = src.waterBias;
        dst.heightProbeDistance = src.heightProbeDistance;
        dst.heightSamples       = src.heightSamples;

        dst.yawCandidates       = (src.yawCandidates != null)
                                  ? (float[])src.yawCandidates.Clone()
                                  : new float[] { 0f, 30f, -30f, 60f, -60f };
        dst.goalWeight          = src.goalWeight;
        dst.penaltyWeight       = src.penaltyWeight;

        dst.enforceWaterLock    = src.enforceWaterLock;
        dst.waterLockStepDeg    = src.waterLockStepDeg;
        dst.waterLockSteps      = src.waterLockSteps;
        dst.pullBackToLastWater = src.pullBackToLastWater;

        dst.preventLandPenetration = src.preventLandPenetration;
        dst.hardPushStrength       = src.hardPushStrength;

        // Movement tuning is owned by the child prefab; leave its speeds alone.
        dst.surfaceSnap     = src.surfaceSnap;
        dst.probeRadius     = src.probeRadius;
        dst.minClearance    = src.minClearance;
        dst.lookAhead       = src.lookAhead;
        dst.probeSkinUp     = src.probeSkinUp;
        dst.probeLocalOffset= src.probeLocalOffset;
    }
}
