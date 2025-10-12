using UnityEngine;

// Tracks individual enemy performance.
public class EnemyTracker : MonoBehaviour
{
    private AdaptiveEnemyWaveSpawner spawner;
    private EnemyKind kind;
    private float spawnTime;
    private float damageDealt;

    public void Init(AdaptiveEnemyWaveSpawner s, EnemyKind k, float time)
    {
        spawner = s;
        kind = k;
        spawnTime = time;
    }


    /// Call this when enemy deals damage
    public void RecordDamage(float amount)
    {
        damageDealt += amount;
    }

    void OnDestroy()
    {
        if (spawner)
        {
            float survival = Time.time - spawnTime;
            spawner.OnEnemyDied(kind, survival, damageDealt, transform.position);
        }
    }
}
