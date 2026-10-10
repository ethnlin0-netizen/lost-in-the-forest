using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Movement))]
[DefaultExecutionOrder(10)]
public class PlayerCombat : MonoBehaviour
{
    [Header("Melee")]
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float swingDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float meleeReach = 0.9f;
    [SerializeField, Min(0.01f)] private float tailSweepReach = 1.8f;
    [SerializeField, Min(0.01f)] private float hitHeight = 1.2f;
    [SerializeField, Min(0f)] private float tailSweepKnockback = 8f;
    [SerializeField, Min(0f)] private float meleeDamage = 15f;
    [SerializeField, Min(0f)] private float tailSweepDamage = 30f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private Transform meleeVisual;
    [SerializeField] private SpriteRenderer chargeIndicator;

    [Header("Ranged")]
    [SerializeField] private PlaceholderProjectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.01f)] private float projectileRange = 14f;
    [SerializeField, Min(0f)] private float projectileDamage = 15f;

    private readonly Collider2D[] hitColliders = new Collider2D[32];
    private readonly HashSet<PracticeTarget> hitTargets = new HashSet<PracticeTarget>();
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private Movement movement;
    private Collider2D bodyCollider;
    private ContactFilter2D targetFilter;
    private bool isCharging;
    private float chargeStartedAt;
    private float swingTimeRemaining;
    private int swingDirection;
    private bool strongSwing;

    public bool IsCharging => isCharging;
    public float ChargeProgress => isCharging
        ? Mathf.Clamp01((Time.time - chargeStartedAt) / chargeDuration) : 0f;
    public bool LastMeleeWasCharged { get; private set; }

    private void Awake()
    {
        movement = GetComponent<Movement>();
        bodyCollider = GetComponent<Collider2D>();
        targetFilter = new ContactFilter2D { useTriggers = false };
        targetFilter.SetLayerMask(targetLayers);
        ResetVisuals();
    }

    private void Update()
    {
        UpdateSwingVisual();
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            CancelCharge();
            return;
        }

        if (keyboard.xKey.wasPressedThisFrame)
        {
            isCharging = true;
            chargeStartedAt = Time.time;
        }

        // Resolve on release so a charged press cannot also fire a basic hit.
        if (keyboard.xKey.wasReleasedThisFrame && isCharging)
        {
            bool charged = Time.time - chargeStartedAt >= chargeDuration;
            CancelCharge();
            PerformMelee(charged);
        }

        if (chargeIndicator != null)
        {
            chargeIndicator.gameObject.SetActive(isCharging);
            chargeIndicator.color = Color.Lerp(new Color(1f, 0.65f, 0.2f), Color.yellow, ChargeProgress);
            chargeIndicator.transform.localScale = new Vector3(0.2f + 0.6f * ChargeProgress, 0.1f, 1f);
        }

        if (keyboard.fKey.wasPressedThisFrame) FireProjectile();
    }

    private void PerformMelee(bool charged)
    {
        LastMeleeWasCharged = charged;
        swingDirection = movement.FacingDirection;
        strongSwing = charged;
        swingTimeRemaining = swingDuration;
        float reach = charged ? tailSweepReach : meleeReach;
        var direction = new Vector2(swingDirection, 0f);
        var center = (Vector2)bodyCollider.bounds.center + direction *
            (bodyCollider.bounds.extents.x + reach * 0.5f);
        int count = Physics2D.OverlapBox(center, new Vector2(reach, hitHeight),
            0f, targetFilter, hitColliders);
        hitTargets.Clear();
        damagedTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var target = hitColliders[i].GetComponentInParent<PracticeTarget>();
            if (target != null && hitTargets.Add(target))
                target.ReceiveHit(charged, direction, tailSweepKnockback);
            var receiver = DamageUtility.Find(hitColliders[i]);
            if (receiver != null && !hitColliders[i].transform.IsChildOf(transform) && damagedTargets.Add(receiver))
                receiver.TakeDamage(charged ? tailSweepDamage : meleeDamage, direction, charged ? tailSweepKnockback : 0f);
        }
        UpdateSwingVisual();
    }

    private void UpdateSwingVisual()
    {
        if (meleeVisual == null) return;
        swingTimeRemaining = Mathf.Max(0f, swingTimeRemaining - Time.deltaTime);
        meleeVisual.gameObject.SetActive(swingTimeRemaining > 0f);
        if (swingTimeRemaining <= 0f) return;
        float progress = 1f - swingTimeRemaining / swingDuration;
        meleeVisual.localScale = new Vector3(swingDirection * (strongSwing ? 1.8f : 0.9f), 1f, 1f);
        meleeVisual.localRotation = Quaternion.Euler(0f, 0f,
            strongSwing ? Mathf.Lerp(65f, -65f, progress) * swingDirection : 0f);
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null) return;
        var direction = movement.AimDirection;
        var bounds = bodyCollider.bounds;
        float clearance = Mathf.Abs(direction.x) * bounds.extents.x +
            Mathf.Abs(direction.y) * bounds.extents.y + 0.2f;
        Vector2 origin = (Vector2)bounds.center + direction * clearance;
        var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(direction, projectileSpeed, projectileRange, GetComponent<Rigidbody2D>(), projectileDamage);
    }

    private void CancelCharge()
    {
        isCharging = false;
        if (chargeIndicator != null) chargeIndicator.gameObject.SetActive(false);
    }

    private void ResetVisuals()
    {
        CancelCharge();
        swingTimeRemaining = 0f;
        if (meleeVisual != null) meleeVisual.gameObject.SetActive(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ResetVisuals();
    }

    private void OnDisable() => ResetVisuals();

    private void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 430, 114), "Movement practice");
        GUI.Label(new Rect(24, 38, 406, 22), "Left / Right: move    Up / Down: aim");
        GUI.Label(new Rect(24, 60, 406, 22), "Z: jump    Z again in the air: wing double jump");
        GUI.Label(new Rect(24, 82, 406, 22),
            $"X: melee    Hold X {chargeDuration:0.##}s, release: tail sweep");
        GUI.Label(new Rect(24, 104, 406, 22), "F: ranged    Orange block: practice target");
    }
}
