using UnityEngine;

public class Bullet : MonoBehaviour
{
    private IDamageable target;
    private int damage;
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

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.Transform.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.Transform.position) < 0.1f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}