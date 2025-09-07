using UnityEngine;

public class Bullet : MonoBehaviour
{
    private IDamageable target;
    [SerializeField] private int damage = 10;
    [SerializeField] private float speed = 5f;

    public void Init(IDamageable target)
    {
        this.target = target;
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
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

}