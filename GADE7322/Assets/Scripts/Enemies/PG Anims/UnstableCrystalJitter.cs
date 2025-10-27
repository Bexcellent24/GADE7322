using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class UnstableCrystalJitter : MonoBehaviour
{
    [Range(0f, 1f)] public float masterIntensity = 0.4f; 
    public bool playOnAwake = true;
    public int seed = 12345;

    [Header("Space")]
    public bool useLocalSpace = true;
    public bool keepUpright  = false;

    [Header("Tremble Settings")]
    [Tooltip("Continuous wiggle.")]
    public float tremblePosAmplitude = 0.03f;
    [Tooltip("Total wobble in degrees.")]
    public float trembleRotDegrees   = 6f;
    [Tooltip("Tremble amplifier")]
    public float trembleScaleAmplitude = 0.05f;
    [Tooltip("Shared frequency for position, rotation and scale tremble")]
    public float trembleFrequency = 8f;

    [Header("Jerks")]
    [Tooltip("Average pops per second ")]
    public float jerkRate = 0.6f;
    public float jerkPosDistance = 0.12f;
    public float jerkRotDegrees = 18f;
    public float jerkDuration = 0.15f;
    public float jerkSettle = 0.22f; 
    
    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;
    Vector3 _baseLocalScale;
    
    // deterministic RNG from seed number
    System.Random _rng;
    // Timer for trembles
    float _t;
    // Enables master at runtime
    bool _running;

    // Jerk state settings
    Vector3 _jerkOffset;
    float _jerkRot;
    float _jerkTime;
    float _jerkTotal;
    Vector3 _jerkDir;   
    Vector3 _jerkAxis;  

    void Awake()
    {
        CacheBase();
        _rng = new System.Random(seed);
    }

    void OnEnable()
    {
        if (playOnAwake) _running = true;
    }

    void OnDisable()
    {
        RestoreTransform();
    }

    //Stores initial local transforms
    void CacheBase()
    {
        _baseLocalPos   = transform.localPosition;
        _baseLocalRot   = transform.localRotation;
        _baseLocalScale = transform.localScale;
    }

    void Update()
    {
        if (!_running) return;
        _t += Time.deltaTime;

        // Continuous tremble 
        Vector3 tremblePos = TremblePosition(_t);
        float wobbleDeg = TrembleRotationDegrees(_t);

        // Occasional jerks
        UpdateJerk(Time.deltaTime);

        // Base + tremble + jerk = final position
        Vector3 finalLocalPos = _baseLocalPos + (tremblePos + _jerkOffset) * masterIntensity;

        // Base * tremble * wobble * jerk = final rotation
        Quaternion finalLocalRot = _baseLocalRot * Quaternion.AngleAxis(wobbleDeg * masterIntensity, JitterAxis(_t));
        if (_jerkTotal > 0f)
            finalLocalRot = finalLocalRot * Quaternion.AngleAxis(_jerkRot * masterIntensity, _jerkAxis);

        // Write to transform in local or world space 
        if (useLocalSpace)
        {
            transform.localPosition = finalLocalPos;
            transform.localRotation = keepUpright ? Uprightize(finalLocalRot) : finalLocalRot;
        }
        else
        {
            if (transform.parent)
            {
                transform.position = transform.parent.TransformPoint(finalLocalPos);
                var worldRot = transform.parent.rotation * finalLocalRot;
                transform.rotation = keepUpright ? Uprightize(worldRot) : worldRot;
            }
            else
            {
                transform.position = finalLocalPos;
                transform.rotation = keepUpright ? Uprightize(finalLocalRot) : finalLocalRot;
            }
        }

        // Scale flicker
        ApplyScaleFlicker();

        // Randomly start a jerk
        if (TryPoisson(jerkRate * masterIntensity, Time.deltaTime))
            StartJerk();
    }

    Vector3 TremblePosition(float t)
    {
        // smooth noise tremble using one shared frequency
        float f = Mathf.Max(0.01f, trembleFrequency) * 0.7f;
        Vector3 n = new Vector3(
            Perlin(t * f, 12.34f),
            Perlin(t * (f * 1.23f), 56.78f),
            Perlin(t * (f * 0.91f), 90.12f)
        );
        return n * tremblePosAmplitude;
    }

    // Perlin-based wiggle
    float TrembleRotationDegrees(float t)
    {
        float f = Mathf.Max(0.01f, trembleFrequency);
        float wob = (Mathf.Sin(t * Mathf.PI * 2f * f) * 0.5f + 0.5f) * trembleRotDegrees;
        return wob;
    }
    
    // Wobble amount over time for continuous rotation tremble
    Vector3 JitterAxis(float t)
    {
        if (keepUpright) return useLocalSpace ? Vector3.up : transform.up;
        Vector3 a = new Vector3(Perlin(t, 1.11f), Perlin(t, 2.22f), Perlin(t, 3.33f));
        if (a.sqrMagnitude < 1e-3f) a = Vector3.up;
        return a.normalized;
    }

    // Keeps crystals read upright.
    Quaternion Uprightize(Quaternion q)
    {
        Vector3 up = (useLocalSpace && transform.parent) ? (transform.parent.rotation * Vector3.up) : Vector3.up;
        Vector3 fwd = q * Vector3.forward;
        Vector3 right = Vector3.Cross(up, fwd).normalized;
        if (right.sqrMagnitude < 1e-4f) return q;
        Vector3 correctedFwd = Vector3.Cross(right, up).normalized;
        return Quaternion.LookRotation(correctedFwd, up);
    }

    // Begin a new jerk
    void StartJerk()
    {
        _jerkTime  = 0f;
        _jerkTotal = Mathf.Max(0.01f, jerkDuration + jerkSettle);
        _jerkDir   = RandomOnUnitSphere();
        _jerkAxis  = keepUpright ? Vector3.up : RandomOnUnitSphere();
    }
    
    // Animate jerk
    void UpdateJerk(float dt)
    {
        if (_jerkTotal <= 0f) { _jerkOffset = Vector3.zero; _jerkRot = 0f; return; }

        _jerkTime += dt;

        float x;
        if (_jerkTime <= jerkDuration)
        {
            // Ease out to peak
            float u = Mathf.Clamp01(_jerkTime / jerkDuration);
            x = 1f - Mathf.Pow(1f - u, 3f);
        }
        else
        {
            // Smoothstep back to zero
            float u = Mathf.InverseLerp(jerkDuration, _jerkTotal, _jerkTime);
            x = 1f - (u * u * (3f - 2f * u));
            x *= 0.6f; 
        }

        _jerkOffset = _jerkDir * (jerkPosDistance * x);
        _jerkRot    = jerkRotDegrees * x;

        // End of jerk cleanup
        if (_jerkTime >= _jerkTotal)
        {
            _jerkTotal  = 0f;
            _jerkOffset = Vector3.zero;
            _jerkRot    = 0f;
        }
    }

    // Local scale oscillation around the base scale
    void ApplyScaleFlicker()
    {
        float f = Mathf.Max(0.01f, trembleFrequency);
        float s = 1f + trembleScaleAmplitude * masterIntensity *
                       Mathf.Sin(_t * Mathf.PI * 2f * f + 1.2345f);
        transform.localScale = _baseLocalScale * s;
    }
    
    // Restores position/rotation/scale to base state
    void RestoreTransform()
    {
        if (useLocalSpace)
        {
            transform.localPosition = _baseLocalPos;
            transform.localRotation = _baseLocalRot;
        }
        else
        {
            transform.position = transform.parent ? transform.parent.TransformPoint(_baseLocalPos) : _baseLocalPos;
            transform.rotation = transform.parent ? transform.parent.rotation * _baseLocalRot : _baseLocalRot;
        }
        transform.localScale = _baseLocalScale;
    }

    // Bernoulli trial for a Poisson rate
    bool TryPoisson(float ratePerSec, float dt)
    {
        float p = 1f - Mathf.Exp(-Mathf.Max(0f, ratePerSec) * dt);
        return RandomValue() < p;
    }

    // Perlin for time t with a constant offset
    float Perlin(float t, float off) => Mathf.PerlinNoise(t, off) * 2f - 1f;

    Vector3 RandomOnUnitSphere()
    {
        float u = (float)_rng.NextDouble();
        float v = (float)_rng.NextDouble();
        float theta = 2f * Mathf.PI * u;
        float z = v * 2f - 1f;
        float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
        return new Vector3(r * Mathf.Cos(theta), z, r * Mathf.Sin(theta)).normalized;
    }

    // Random value from the seeded RNG
    float RandomValue() => (float)_rng.NextDouble();
}
