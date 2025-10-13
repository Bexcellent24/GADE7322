using UnityEngine;

[RequireComponent(typeof(Health))]
public class SplitOnDeath : MonoBehaviour
{
    [Header("Split Settings")]
    [SerializeField] private GameObject splitChildPrefab;

    [Min(2)] [SerializeField] private int count = 2;

    [Header("Size of children")]
    [Range(0.05f, 2f)] [SerializeField] private float relativeScale = 0.5f;

    [Header("Spawn spread")]
    [Range(0.0f, 2.0f)] [SerializeField] private float burstRadius = 0.6f;

    [Header("Navigation Inheritance")]
    [SerializeField] private bool inheritNavigatorSpeeds = true;

    private Health _hp;
    private WaterGlobeNavigator _sourceNav;

    void OnEnable()
    {
        _hp = GetComponent<Health>();
        _sourceNav = GetComponent<WaterGlobeNavigator>();
        
        if (_hp != null) 
            _hp.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (_hp != null) 
            _hp.OnDeath -= HandleDeath;
    }

    void HandleDeath(IDamageable _)
    {
        if (splitChildPrefab == null)
        {
            Debug.LogWarning("[SplitOnDeath] No splitChildPrefab assigned.");
            return;
        }

        Vector3 center = transform.position;
        Vector3 up = transform.up;
        Transform parent = transform.parent;

        for (int i = 0; i < count; i++)
        {
            Vector3 tangent = Vector3.ProjectOnPlane(Random.onUnitSphere, up).normalized;
            if (tangent.sqrMagnitude < 1e-6f) 
                tangent = Vector3.right;
                
            Vector3 spawnPos = center + tangent * burstRadius;

            Quaternion rot = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, up).normalized,
                up
            );

            GameObject child = Instantiate(splitChildPrefab, spawnPos, rot);
            
            if (parent != null)
                child.transform.SetParent(parent, true);

            ApplyRelativeScale(child.transform, transform, parent, relativeScale);

            // Only copy speeds, let WaterWorldManager handle world data
            if (inheritNavigatorSpeeds && _sourceNav != null)
            {
                var dstNav = child.GetComponent<WaterGlobeNavigator>();
                if (dstNav != null)
                {
                    dstNav.MoveSpeed = _sourceNav.MoveSpeed;
                    dstNav.TurnSpeedDeg = _sourceNav.TurnSpeedDeg;
                    dstNav.HoverOffset = _sourceNav.HoverOffset;
                }
            }
        }
    }

    static void ApplyRelativeScale(Transform child, Transform source, Transform commonParent, float factor)
    {
        Vector3 sourceWorld = source.lossyScale;
        Vector3 targetWorld = sourceWorld * factor;

        if (commonParent == null)
        {
            child.localScale = targetWorld;
            return;
        }

        Vector3 parentWorld = commonParent.lossyScale;
        child.localScale = new Vector3(
            SafeDiv(targetWorld.x, parentWorld.x),
            SafeDiv(targetWorld.y, parentWorld.y),
            SafeDiv(targetWorld.z, parentWorld.z)
        );
    }

    static float SafeDiv(float a, float b) => Mathf.Approximately(b, 0f) ? a : a / b;
}