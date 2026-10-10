using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-50)]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maximumHealth = 60f;
    [SerializeField, Range(0f, 1f)] private float knockbackResistance;
    [SerializeField, Min(0f)] private float deathDelay = 0.4f;
    [SerializeField] private Transform visuals;
    private SpriteRenderer[] renderers;
    private Color[] colors;
    private Rigidbody2D body;
    private float flashUntil;
    private float deathAt;
    public float CurrentHealth { get; private set; }
    public float MaximumHealth => maximumHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public float LastHitTime { get; private set; } = float.NegativeInfinity;
    public event Action HealthChanged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maximumHealth;
        body = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;
    }

    public void TakeDamage(float amount, Vector2 direction, float knockbackImpulse)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        LastHitTime = Time.time;
        flashUntil = Time.time + 0.15f;
        HealthChanged?.Invoke();
        if (!IsDead)
        {
            body.AddForce(direction.normalized * knockbackImpulse * (1f - knockbackResistance), ForceMode2D.Impulse);
            return;
        }
        deathAt = Time.time;
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        Died?.Invoke();
        Destroy(gameObject, deathDelay);
    }

    private void Update()
    {
        float fade = IsDead ? 1f - Mathf.Clamp01((Time.time - deathAt) / Mathf.Max(0.01f, deathDelay)) : 1f;
        for (int i = 0; i < renderers.Length; i++)
        {
            var color = !IsDead && Time.time < flashUntil ? Color.white : colors[i];
            color.a *= fade;
            renderers[i].color = color;
        }
        if (IsDead && visuals != null) visuals.localScale *= Mathf.Max(0f, 1f - Time.deltaTime * 4f);
    }
}
