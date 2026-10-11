using UnityEngine;

// Keeps existing damage interfaces, health bars, knockback and crystal rewards.
public sealed class BlightBossHealth : EnemyHealth
{
    public override void TakeDamage(float amount, Vector2 direction, float knockbackImpulse)
    {
        var boss = GetComponent<BlightBoss>();
        float multiplier = boss != null ? boss.IncomingDamageMultiplier : 1f;
        base.TakeDamage(amount * multiplier, direction, knockbackImpulse);
    }
}
