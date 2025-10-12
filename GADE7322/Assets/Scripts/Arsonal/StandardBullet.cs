using UnityEngine;

public class StandardBullet : BaseBullet
{
    protected override void OnImpact()
    {
        // Damage only the target
        target.TakeDamage((int)damage);
        
        AudioManager.Instance?.PlaySFX("Shoot");
        
        Destroy(gameObject);
    }
}