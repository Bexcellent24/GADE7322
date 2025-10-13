// Assets/Scripts/Enemies/Swarm/SwarmParticlesController.cs
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SwarmParticlesController : MonoBehaviour
{
    [Header("HP Binding")]
    public int maxHealth = 100;

    // Particle targets at key HP landmarks (we'll blend between these)
    public int particlesAt100 = 60;  // when HP = 100%
    public int particlesAt50  = 30;  // when HP = 50%
    public int particlesAt10  = 8;   // when HP = 10%
    // at 0% HP we force 0 particles

    [Header("Swarm Tuning")]
    public float swarmRadius = 1.4f;
    public float orbitSpeedBoost = 0.3f;

    [Header("Dynamics")]
    [Tooltip("Average lifetime (seconds). Lower = faster visual response to damage.")]
    public float cloudLifetime = 2.0f;
    [Tooltip("How fast the emission rate moves toward the target.")]
    public float rateSmoothing = 35f; // higher = snappier

    // --- runtime ---
    ParticleSystem ps;
    int currentHealth;
    bool inited;
    float _currentRate;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        // Main
        var main = ps.main;
        main.maxParticles    = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop            = true;
        main.startLifetime   = cloudLifetime;

        // Shape
        var shape = ps.shape;
        shape.shapeType             = ParticleSystemShapeType.Sphere;
        shape.radius                = swarmRadius;
        shape.randomDirectionAmount = 0.6f;

        // Velocity over Lifetime
        var vel = ps.velocityOverLifetime;
        vel.enabled  = true;
        vel.orbitalX = 0.4f + orbitSpeedBoost;
        vel.orbitalY = 0.6f + orbitSpeedBoost;
        vel.orbitalZ = 0.5f + orbitSpeedBoost;

        // Gentle forces
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        force.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        force.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        // Emission: rate-only (no bursts)
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());

        // Initial HP and immediate rate set (no smoothing in Awake)
        currentHealth = maxHealth;
        float targetRate = CountToRate(TargetCountFromHP(currentHealth, maxHealth));
        _currentRate = targetRate;
        emission.rateOverTime = _currentRate;
        if (main.maxParticles < targetRate * cloudLifetime)
            main.maxParticles = Mathf.CeilToInt(targetRate * cloudLifetime);

        ps.Clear();
        ps.Play();

        inited = true;
    }

    void OnValidate()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!ps) return;

        var main = ps.main;
        main.startLifetime = cloudLifetime;

        var shape = ps.shape;
        shape.radius = swarmRadius;

        if (Application.isPlaying && inited)
            ApplyRate(immediate: true);
    }

    // --- Public API used by your binder ---
    public void SetMaxHealth(int hp)
    {
        maxHealth = Mathf.Max(1, hp);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        if (inited) ApplyRate(immediate: false);
    }

    public void SetCurrentHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHealth));
        if (inited) ApplyRate(immediate: false);
    }

    // --- Core logic ---

    // Continuous mapping:
    // 0%  -> 0
    // 10% -> particlesAt10
    // 50% -> particlesAt50
    // 100%-> particlesAt100
    // (piecewise linear between those anchors)
    int TargetCountFromHP(int hp, int max)
    {
        float p = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;

        if (p <= 0f) return 0;
        if (p <= 0.10f)
        {
            // 0%..10% : 0 -> particlesAt10
            float t = p / 0.10f;
            return Mathf.RoundToInt(Mathf.Lerp(0, particlesAt10, t));
        }
        if (p <= 0.50f)
        {
            // 10%..50% : particlesAt10 -> particlesAt50
            float t = (p - 0.10f) / 0.40f;
            return Mathf.RoundToInt(Mathf.Lerp(particlesAt10, particlesAt50, t));
        }
        else
        {
            // 50%..100% : particlesAt50 -> particlesAt100
            float t = (p - 0.50f) / 0.50f;
            return Mathf.RoundToInt(Mathf.Lerp(particlesAt50, particlesAt100, t));
        }
    }

    float CountToRate(int count)
    {
        float lifetime = Mathf.Max(0.25f, cloudLifetime);
        return count / lifetime; // steady-state: count ≈ rate * lifetime
    }

    void ApplyRate(bool immediate)
    {
        int targetCount = TargetCountFromHP(currentHealth, maxHealth);
        float targetRate = CountToRate(targetCount);

        var emission = ps.emission;
        var main     = ps.main;

        if (immediate)
        {
            _currentRate = targetRate;
        }
        else
        {
            // smooth towards target
            float step = (rateSmoothing + targetRate) * Time.deltaTime;
            _currentRate = Mathf.MoveTowards(_currentRate, targetRate, step);
        }

        emission.rateOverTime = _currentRate;

        // ensure capacity so we can reach/maintain the target
        int needed = Mathf.CeilToInt(_currentRate * Mathf.Max(0.25f, cloudLifetime));
        if (main.maxParticles < needed) main.maxParticles = needed;
    }

    /// <summary>Optional: snap instantly to current HP visual state.</summary>
    public void SnapNow()
    {
        ApplyRate(immediate: true);
        ps.Clear();
    }
}
