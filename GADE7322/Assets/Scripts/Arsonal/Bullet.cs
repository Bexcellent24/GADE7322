using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    
    private IDamageable target;
    private float damage = 10;
    
    public void Init(IDamageable target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }

    void Update()
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
            target.TakeDamage((int)damage);
            Destroy(gameObject);
        }
    }

}