using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount, Vector2 direction, float knockbackImpulse);
}

public static class DamageUtility
{
    public static IDamageable Find(Component hit)
    {
        foreach (var component in hit.GetComponentsInParent<MonoBehaviour>())
            if (component is IDamageable receiver) return receiver;
        return null;
    }
}
