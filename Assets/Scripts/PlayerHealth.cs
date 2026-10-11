using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Movement), typeof(PlayerCombat))]
[DefaultExecutionOrder(20)]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maximumHealth = 100f;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float knockbackRecovery = 20f;
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.65f;
    [SerializeField] private SpriteRenderer bodyVisual;
    [SerializeField, Min(0f)] private float deathRespawnDelay = 0.75f;
    private Rigidbody2D body;
    private Movement movement;
    private PlayerCombat combat;
    private Color restingColor;
    private float flashUntil;
    private float knockbackSpeed;
    private float invulnerableUntil;
    public float CurrentHealth { get; private set; }
    public float MaximumHealth => maximumHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsInvulnerable => !IsDead && Time.time < invulnerableUntil;
    public event Action HealthChanged;

    private void Awake()
    {
        CurrentHealth = maximumHealth;
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        combat = GetComponent<PlayerCombat>();
        if (bodyVisual == null) bodyVisual = GetComponentInChildren<SpriteRenderer>();
        if (bodyVisual != null) restingColor = bodyVisual.color;
    }

    public void TakeDamage(float amount, Vector2 direction, float knockbackImpulse)
    {
        if (IsDead || IsInvulnerable || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        flashUntil = Time.time + hitFlashDuration;
        invulnerableUntil = Time.time + invulnerabilityDuration;
        knockbackSpeed += direction.normalized.x * knockbackImpulse / Mathf.Max(0.01f, body.mass);
        HealthChanged?.Invoke();
        if (!IsDead) return;
        movement.enabled = false;
        combat.enabled = false;
        knockbackSpeed = 0f;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        StartCoroutine(RespawnCurrentBiome());
    }

    private System.Collections.IEnumerator RespawnCurrentBiome()
    {
        // Keep the death reaction visible, then restart this biome at its authored spawn.
        var biome = gameObject.scene;
        yield return new WaitForSecondsRealtime(deathRespawnDelay);
        if (!biome.IsValid() || !biome.isLoaded) yield break;
        LevelRestartOnDeath.RestartScene(biome, this);
    }

    private void FixedUpdate()
    {
        if (IsDead || Mathf.Abs(knockbackSpeed) < 0.001f) return;
        // Movement sets the input velocity first; retain the hit impulse afterward.
        body.linearVelocity = new Vector2(body.linearVelocity.x + knockbackSpeed, body.linearVelocity.y);
        knockbackSpeed = Mathf.MoveTowards(knockbackSpeed, 0f, knockbackRecovery * Time.fixedDeltaTime);
    }

    private void Update()
    {
        if (bodyVisual != null)
            bodyVisual.color = IsDead ? restingColor * new Color(0.45f, 0.45f, 0.45f, 1f) :
                Time.time < flashUntil ? Color.red :
                IsInvulnerable && Mathf.FloorToInt(Time.time * 16f) % 2 == 0 ? Color.white : restingColor;
    }
}
