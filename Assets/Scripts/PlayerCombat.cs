using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Movement))]
[DefaultExecutionOrder(10)]
public class PlayerCombat : MonoBehaviour
{
    [Header("Melee")]
    [SerializeField, Min(0.01f)] private float swingDuration = 0.2f;
    [Tooltip("Minimum time between tail strikes; measured from the start of each swipe.")]
    [SerializeField, Min(0.01f)] private float meleeCooldown = 0.3f;
    [SerializeField, Min(0.01f)] private float meleeReach = 0.9f;
    [SerializeField, Min(0.01f)] private float tailSweepReach = 1.8f;
    [SerializeField, Min(0.01f)] private float hitHeight = 1.2f;
    [SerializeField, Min(0f)] private float tailSweepKnockback = 8f;
    [SerializeField, Min(0f)] private float meleeDamage = 15f;
    [SerializeField, Min(0f)] private float tailSweepDamage = 30f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private Transform meleeVisual;

    [Header("Ranged")]
    [SerializeField] private PlaceholderProjectile projectilePrefab;
    [Tooltip("Mouth launch point, directly under the player root. Its local X mirrors with facing.")]
    [SerializeField] private Transform projectileOrigin;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.01f)] private float projectileRange = 14f;
    [SerializeField, Min(0f)] private float projectileDamage = 15f;

    private readonly Collider2D[] hitColliders = new Collider2D[32];
    private readonly HashSet<PracticeTarget> hitTargets = new HashSet<PracticeTarget>();
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private Movement movement;
    private Collider2D bodyCollider;
    private ContactFilter2D targetFilter;
    private int swingDirection;
    private float nextMeleeAt = float.NegativeInfinity;

    public float MeleeCooldownRemaining => Mathf.Max(0f, nextMeleeAt - Time.time);

    public bool LastMeleeWasCharged { get; private set; }
    // The legacy "charged" flag selects the full-strength tail visuals and knockback.
    // Presentation and damage now start together on the X press, without a charge phase.
    public event System.Action<bool, int, float, float> MeleePerformed;
    public event System.Action VisualsReset;
    public event System.Action RangedPerformed;

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
        if (projectileOrigin != null)
        {
            var position = projectileOrigin.localPosition;
            position.x = Mathf.Abs(position.x) * movement.FacingDirection;
            projectileOrigin.localPosition = position;
        }
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // One full-strength tail sweep per press; holding and releasing do not attack.
        if (keyboard.xKey.wasPressedThisFrame) PerformMelee(true);

        if (keyboard.fKey.wasPressedThisFrame) FireProjectile();
    }

    private void PerformMelee(bool charged)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || Time.time < nextMeleeAt) return;
        nextMeleeAt = Time.time + Mathf.Max(meleeCooldown, swingDuration);
        LastMeleeWasCharged = charged;
        swingDirection = movement.FacingDirection;
        float reach = charged ? tailSweepReach : meleeReach;
        MeleePerformed?.Invoke(charged, swingDirection, swingDuration, bodyCollider.bounds.extents.x + reach);
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
        // Older scene copies may still reference the orange placeholder box.
        // Only the actual Lenny tail artwork should render during an attack.
        meleeVisual.gameObject.SetActive(false);
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null) return;
        var direction = movement.AimDirection;
        var bounds = bodyCollider.bounds;
        float clearance = Mathf.Abs(direction.x) * bounds.extents.x +
            Mathf.Abs(direction.y) * bounds.extents.y + 0.2f;
        // Keep the collider-clearance fallback for existing setups without a mouth reference.
        Vector2 origin = projectileOrigin != null ? (Vector2)projectileOrigin.position :
            (Vector2)bounds.center + direction * clearance;
        var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(direction, projectileSpeed, projectileRange, GetComponent<Rigidbody2D>(), projectileDamage);
        RangedPerformed?.Invoke();
    }

    private void ResetVisuals()
    {
        if (meleeVisual != null) meleeVisual.gameObject.SetActive(false);
        VisualsReset?.Invoke();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ResetVisuals();
    }

    private void OnDisable() => ResetVisuals();

}
