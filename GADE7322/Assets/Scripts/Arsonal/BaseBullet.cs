using UnityEngine;

public abstract class BaseBullet : MonoBehaviour
{
    [SerializeField] protected float speed = 5f;
    
    protected IDamageable target;
    protected float damage;
    protected Faction attackerFaction;
    
    public virtual void Init(IDamageable target, float damage, Faction faction)
    {
        this.target = target;
        this.damage = damage;
        this.attackerFaction = faction;
    }

    protected virtual void Update()
    {
        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.Transform.position,
            speed * Time.deltaTime
        );

        // Rotate to face target
        Vector3 direction = (target.Transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // Check if hit
        if (Vector3.Distance(transform.position, target.Transform.position) < 0.1f)
        {
            OnImpact();
        }
    }

    protected abstract void OnImpact();
}