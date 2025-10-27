using UnityEngine;
using System.Collections.Generic;


public class AuraAttackerMulti : MonoBehaviour
{
    [Header("Damage / Targeting")]
    [SerializeField] private float tickRate = 0.2f; 
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool popShieldOnBlock = true;

    [Header("Beam Visuals")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private Transform[] beamSockets = new Transform[0];

    private float _range;
    private float _damagePerSecond;

    private float _tickTimer;
    private readonly List<IDamageable> _targetsInRange = new();
    private readonly List<GameObject> _activeBeams = new();

    private Actor _selfActor;
    private int _shieldLayer;
    private static readonly Collider[] HitsBuffer = new Collider[128];

    public void Initialize(float range, float damagePerSecond) { _range = range; _damagePerSecond = damagePerSecond; }

    void Awake()
    {
        _selfActor = GetComponent<Actor>(); 
        _shieldLayer = LayerMask.NameToLayer("Shield"); 
    }

    void OnDisable() => CleanupBeams();
    void OnDestroy() => CleanupBeams();

    void Update()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= tickRate)
        {
            FindTargetsInRange();
            ApplyDamage();
            _tickTimer = 0f;
        }

        UpdateBeamVisuals();
    }

    private void FindTargetsInRange()
    {
        _targetsInRange.Clear();

        int count = (targetLayers.value == 0)
            ? Physics.OverlapSphereNonAlloc(transform.position, _range, HitsBuffer)
            : Physics.OverlapSphereNonAlloc(transform.position, _range, HitsBuffer, targetLayers);

        for (int i = 0; i < count; i++)
        {
            var col = HitsBuffer[i];
            if (!col) continue;

            if (!col.TryGetComponent<IDamageable>(out var dmg) || !dmg.IsAlive) continue;

            if (_selfActor && col.TryGetComponent<Actor>(out var other))
            {
                if (other.faction == _selfActor.faction) continue;
            }

            _targetsInRange.Add(dmg);
        }
    }

    private void ApplyDamage()
    {
        if (_targetsInRange.Count == 0) return;

        int damageThisTick = Mathf.Max(1, Mathf.RoundToInt(_damagePerSecond * tickRate));
        Vector3 fallbackStart = transform.position;

        foreach (var target in _targetsInRange)
        {
            if (target == null || !target.IsAlive) continue;

            var t = target.Transform;
            if (!t) continue;

            Vector3 start = GetBestSocketPositionOrFallback(t.position, fallbackStart);

            if (IsBlockedByShield(start, t.position, out var shield))
            {
                if (popShieldOnBlock && shield) { }
                continue;
            }

            target.TakeDamage(damageThisTick);
        }
    }

    private void UpdateBeamVisuals()
    {
        if (laserBeamPrefab == null || beamSockets == null || beamSockets.Length == 0)
        {
            SyncBeamInstances( Mathf.Min(_targetsInRange.Count, 1) );
            for (int i = 0; i < _activeBeams.Count; i++)
                UpdateBeamLine(i, transform.position, PickTargetPosForIndex(i));
            return;
        }

        int beamCount = Mathf.Min(4, beamSockets.Length); 
        // If there is at least 1 target = render 4 beams. If 0 targets = render 0 beams
        int desiredBeams = (_targetsInRange.Count > 0) ? beamCount : 0;

        SyncBeamInstances(desiredBeams);

        for (int i = 0; i < _activeBeams.Count; i++)
        {
            Transform socket = beamSockets[i] ? beamSockets[i] : transform;
            Vector3 start = socket.position;

            // Distribute beams across available targets
            Vector3 end = PickTargetPosForIndex(i);

            if (IsBlockedByShield(start, end, out _, out var hitPoint))
                end = hitPoint;

            UpdateBeamLine(i, start, end);
        }
    }

    private Vector3 PickTargetPosForIndex(int i)
    {
        if (_targetsInRange.Count == 0) return transform.position;
        var target = _targetsInRange[i % _targetsInRange.Count];
        return (target != null && target.IsAlive && target.Transform) ? target.Transform.position : transform.position;
    }

    private void SyncBeamInstances(int desired)
    {
        for (int i = _activeBeams.Count - 1; i >= desired; i--)
        {
            if (_activeBeams[i]) Destroy(_activeBeams[i]);
            _activeBeams.RemoveAt(i);
        }
        while (_activeBeams.Count < desired)
        {
            var src = beamSockets != null && beamSockets.Length > 0 && beamSockets[_activeBeams.Count]
                ? beamSockets[_activeBeams.Count].position
                : transform.position;
            var beam = Instantiate(laserBeamPrefab, src, Quaternion.identity, transform);
            _activeBeams.Add(beam);
        }
    }

    private void UpdateBeamLine(int beamIndex, Vector3 start, Vector3 end)
    {
        var go = _activeBeams[beamIndex];
        if (!go) return;
        var lr = go.GetComponent<LineRenderer>();
        if (!lr) return;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    private Vector3 GetBestSocketPositionOrFallback(Vector3 targetPos, Vector3 fallback)
    {
        if (beamSockets == null || beamSockets.Length == 0) return fallback;
        float best = float.NegativeInfinity;
        Vector3 bestPos = fallback;
        foreach (var s in beamSockets)
        {
            if (!s) continue;
            float d = Vector3.Dot((targetPos - s.position).normalized, s.forward);
            if (d > best) { best = d; bestPos = s.position; }
        }
        return bestPos;
    }

    private bool IsBlockedByShield(Vector3 from, Vector3 to, out FrontalShield shield) =>
        IsBlockedByShield(from, to, out shield, out _);

    private bool IsBlockedByShield(Vector3 from, Vector3 to, out FrontalShield shield, out Vector3 hitPoint)
    {
        shield = null; hitPoint = default;
        if (_shieldLayer == -1) return false;

        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 1e-4f) return false;

        if (Physics.Raycast(from, dir / dist, out RaycastHit hit, dist, 1 << _shieldLayer, QueryTriggerInteraction.Collide))
        {
            shield = hit.collider ? hit.collider.GetComponentInParent<FrontalShield>() : null;
            if (shield) { hitPoint = hit.point; return true; }
        }
        return false;
    }

    private void CleanupBeams()
    {
        for (int i = _activeBeams.Count - 1; i >= 0; i--)
        {
            if (_activeBeams[i]) Destroy(_activeBeams[i]);
        }
        _activeBeams.Clear();
    }
}
