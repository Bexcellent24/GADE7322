using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoidEnemy : MonoBehaviour, IDamageable
{
    public enum State { Roam, Hunt, Attack }

    [Header("Targeting")]
    [SerializeField] LayerMask towerMask;
    [SerializeField] string towerTag = "Tower";
    [SerializeField] float detectionRadius = 40f;
    [SerializeField] float attackRange = 3.0f;

    [Header("Attack")]
    [SerializeField] float dps = 6f;                 
    [SerializeField] float attackTickRate = 0.2f;    
    float attackTimer;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float maxForce = 30f;
    [SerializeField] float wanderArcDegrees = 25f;
    [SerializeField] float wanderRetargetTime = 2.5f;

    [Header("Health")]
    [SerializeField] float maxHealth = 30f;
    float health;
    
    float stickRadius;
    
    Rigidbody rb;
    BoidFlock flock;
    Transform planetCenter;
    State state;
    Vector3 wanderDir;
    float wanderTimer;
    readonly List<BoidEnemy> neighbors = new();

    public bool IsAlive => health > 0f;
    UnityEngine.Transform IDamageable.Transform => this.transform;
    void IDamageable.TakeDamage(int amount) => TakeDamage((float)amount);

    void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                
        flock = BoidFlock.Instance;
        flock?.Register(this);
        planetCenter = flock?.PlanetCenter;

        health = maxHealth;
        PickNewWanderDir();

        if (planetCenter != null)
            stickRadius = (transform.position - planetCenter.position).magnitude;
    }

    void OnDisable() => flock?.Unregister(this);

    void FixedUpdate()
    {
        // State selection
        var target = FindClosestTower();
        if (target)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            state = dist <= attackRange ? State.Attack : State.Hunt;
        }
        else state = State.Roam;

        // Steering
        Vector3 steering = Vector3.zero;
        flock.GetNeighbors(this, neighbors);
        steering += flock.SeparationW * Separation();
        steering += flock.AlignmentW  * Alignment();
        steering += flock.CohesionW   * Cohesion();

        Vector3 goal = state switch
        {
            State.Hunt   => Seek(target.position),
            State.Attack => Orbit(target),
            State.Roam   => WanderOnSphere(),
            _            => Vector3.zero
        };
        steering += flock.GoalW * goal;
        steering += flock.SurfaceW * StickToSphereTangent();

        steering = Vector3.ClampMagnitude(steering, maxForce);
        rb.AddForce(steering, ForceMode.Acceleration);
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity, SurfaceUp());

        if (state == State.Attack && target.TryGetComponent<IDamageable>(out var dmgTarget))
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer >= attackTickRate)
            {
                int dmg = Mathf.Max(1, Mathf.RoundToInt(dps * attackTickRate));
                dmgTarget.TakeDamage(dmg);
                attackTimer = 0f;
            }
        }
        else
        {
            attackTimer = 0f;
        }
        if (planetCenter != null)
        {
            Vector3 up = SurfaceUp();

            rb.linearVelocity -= Vector3.Project(rb.linearVelocity, up);

            Vector3 fromCenter = transform.position - planetCenter.position;
            float currentR = fromCenter.magnitude;
            float desiredR = (stickRadius > 0f) ? stickRadius : currentR;
            float correction = Mathf.Clamp01(0.5f); 
            float newR = Mathf.Lerp(currentR, desiredR, correction);
            transform.position = planetCenter.position + up * newR;
        }
    }

    #region Steering
    Vector3 Separation()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        float r2 = flock.SeparationRadius * flock.SeparationRadius;
        foreach (var n in neighbors)
        {
            Vector3 to = transform.position - n.transform.position;
            float d2 = to.sqrMagnitude;
            if (d2 < 0.0001f || d2 > r2) continue;
            sum += to.normalized / Mathf.Max(0.001f, Mathf.Sqrt(d2));
        }
        return ToAccel(sum);
    }

    Vector3 Alignment()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        Vector3 avg = Vector3.zero;
        foreach (var n in neighbors) avg += n.rb.linearVelocity;
        avg /= neighbors.Count;
        return ToAccel((avg.normalized * maxSpeed) - rb.linearVelocity);
    }

    Vector3 Cohesion()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        Vector3 center = Vector3.zero;
        foreach (var n in neighbors) center += n.transform.position;
        center /= neighbors.Count;
        return Seek(center);
    }

    Vector3 Seek(Vector3 worldTarget)
    {
        Vector3 desired = (worldTarget - transform.position).normalized * maxSpeed;
        return ToAccel(desired - rb.linearVelocity);
    }

    Vector3 Orbit(Transform target)
    {
        Vector3 toTarget = (target.position - transform.position);
        Vector3 tangent = Vector3.Cross(SurfaceUp(), toTarget).normalized;
        Vector3 desired = tangent * maxSpeed;
        return ToAccel(desired - rb.linearVelocity);
    }

    Vector3 WanderOnSphere()
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f) PickNewWanderDir();
        Vector3 desired = wanderDir * maxSpeed * 0.75f;
        return ToAccel(desired - rb.linearVelocity);
    }

    void PickNewWanderDir()
    {
        wanderTimer = wanderRetargetTime;
        Vector3 up = SurfaceUp();
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.Cross(up, Random.onUnitSphere).normalized;
        Quaternion rot = Quaternion.AngleAxis(Random.Range(-wanderArcDegrees, wanderArcDegrees), up);
        wanderDir = (rot * fwd).normalized;
    }

    Vector3 StickToSphereTangent()
    {
        Vector3 up = SurfaceUp();
        Vector3 radialVel = Vector3.Project(rb.linearVelocity, up);
        return -radialVel * 5f;
    }

    Vector3 SurfaceUp()
    {
        if (planetCenter == null) return Vector3.up;
        return (transform.position - planetCenter.position).normalized;
    }

    Vector3 ToAccel(Vector3 dv) => Vector3.ClampMagnitude(dv, maxForce);
    #endregion

    #region Health
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        health -= amount;
        if (health <= 0f) Die();
        // TODO: VFX/SFX
    }

    void Die()
    {
        // TODO: drops, death VFX
        Destroy(gameObject);
    }
    #endregion

    #region Targeting
    Transform FindClosestTower()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, towerMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Transform bestT = null;
        foreach (var c in hits)
        {
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; bestT = c.transform; }
        }

        if (!bestT && !string.IsNullOrEmpty(towerTag))
        {
            var tagged = GameObject.FindGameObjectsWithTag(towerTag);
            foreach (var go in tagged)
            {
                float d = (go.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; bestT = go.transform; }
            }
        }
        return bestT;
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
