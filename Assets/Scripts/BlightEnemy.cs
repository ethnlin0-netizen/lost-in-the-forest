using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth), typeof(BoxCollider2D))]
[RequireComponent(typeof(EnemyNavigator2D))]
public class BlightEnemy : MonoBehaviour
{
    public enum BlightType { Chomper, Spora, Collusus }
    public enum BehaviourState { Patrol, Chase, Windup, Recovery, Dead }
    [SerializeField] private BlightType kind;
    [SerializeField, Min(0f)] private float movementSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float detectionRange = 7f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
    [SerializeField, Min(0.1f)] private float verticalTolerance = 1.2f;
    [SerializeField, Min(0f)] private float damage = 12f;
    [SerializeField, Min(0.01f)] private float windup = 0.55f;
    [SerializeField, Min(0.01f)] private float recovery = 0.4f;
    [SerializeField, Min(0.01f)] private float cooldown = 1.5f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private Transform visuals;
    [SerializeField] private GameObject attackVisual;
    [SerializeField] private PlaceholderProjectile sporePrefab;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;
    [SerializeField, Min(0.1f)] private float projectileRange = 10f;
    [SerializeField, Range(0.1f, 0.9f)] private float attackDirectionLockFraction = 0.65f;
    [SerializeField, Min(0f)] private float pursuitMemory = 2f;
    [Header("Movement and combat feel")]
    [SerializeField, Min(0.1f)] private float groundAcceleration = 45f;
    [SerializeField, Min(0f)] private float patrolRadius = 3f;
    [SerializeField, Range(0.5f, 1f)] private float meleeStopFraction = 0.9f;
    [SerializeField, Min(0f)] private float meleeResumeMargin = 0.2f;
    [SerializeField, Min(0f)] private float hitReactionDuration = 0.22f;
    [SerializeField] private Color windupColor = new Color(1f, 0.85f, 0.15f);
    [SerializeField] private Color committedColor = new Color(1f, 0.35f, 0.1f);
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private EnemyHealth health;
    private EnemyNavigator2D navigator;
    private Movement target;
    private Collider2D targetShape;
    private Vector3 visualScale;
    private float stateUntil;
    private float nextAttack;
    private int facing = 1;
    private float move;
    private bool useNavigation, attackDirectionLocked;
    private float windupStarted, lastDetected = float.NegativeInfinity;
    private Vector2 lastKnownFeet, attackDirection;
    private bool holdingMeleeDistance;
    private float patrolCenter, impactUntil, observedHitTime = float.NegativeInfinity;
    private SpriteRenderer[] cueRenderers;
    private float[] cueAlpha;
    private readonly RaycastHit2D[] rays = new RaycastHit2D[32];
    private readonly Collider2D[] overlaps = new Collider2D[32];
    private readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();
    public BehaviourState State { get; private set; }
    public int AttacksPerformed { get; private set; }
    public BlightType Kind => kind;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<BoxCollider2D>();
        health = GetComponent<EnemyHealth>();
        navigator = GetComponent<EnemyNavigator2D>();
        visualScale = visuals != null ? visuals.localScale : Vector3.one;
        health.Died += Die;
        FindPlayer();
        patrolCenter = transform.position.x;
        cueRenderers = attackVisual != null ? attackVisual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        cueAlpha = new float[cueRenderers.Length];
        for (int i = 0; i < cueRenderers.Length; i++) cueAlpha[i] = cueRenderers[i].color.a;
        if (attackVisual != null) attackVisual.SetActive(false);
    }
    private void OnDisable()
    {
        if (navigator != null) navigator.Stop();
        move = 0f; useNavigation = holdingMeleeDistance = false; impactUntil = 0f;
        if (attackVisual != null) attackVisual.SetActive(false);
        if (health != null && !health.IsDead) State = BehaviourState.Patrol;
        if (body != null && body.simulated) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
    private void OnDestroy() { if (health != null) health.Died -= Die; }
    private void FindPlayer()
    {
        target = FindFirstObjectByType<Movement>();
        targetShape = target != null ? target.GetComponent<Collider2D>() : null;
    }
    private void Die()
    {
        State = BehaviourState.Dead;
        move = 0f;
        navigator.Stop();
        if (attackVisual != null) attackVisual.SetActive(false);
        enabled = false;
    }
    private Vector2 TargetPosition => targetShape != null ? (Vector2)targetShape.bounds.center : (Vector2)target.transform.position;
    private bool CanSeePlayer()
    {
        if (target == null || !target.isActiveAndEnabled) return false;
        var receiver = DamageUtility.Find(target);
        if (receiver != null && receiver.IsDead) return false;
        Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
        if (delta.magnitude > detectionRange) return false;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayers);
        int count = Physics2D.Raycast(shape.bounds.center, delta.normalized, filter, rays, delta.magnitude);
        for (int i = 0; i < count; i++)
            if (rays[i].collider != null && rays[i].rigidbody != body && rays[i].collider.GetComponentInParent<Movement>() != target) return false;
        return true;
    }
    private bool InAttackRange()
    {
        if (!CanSeePlayer()) return false;
        Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
        return kind == BlightType.Spora ? delta.magnitude <= attackRange :
            Mathf.Abs(delta.x) <= attackRange && Mathf.Abs(delta.y) <= verticalTolerance;
    }
    private bool PlayerNearby()
    {
        if (target == null || !target.isActiveAndEnabled) return false;
        var receiver = DamageUtility.Find(target);
        return (receiver == null || !receiver.IsDead) && Vector2.Distance(TargetPosition, shape.bounds.center) <= detectionRange;
    }
    private void AimAtPlayer()
    {
        Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
        if (Mathf.Abs(delta.x) > 0.05f) facing = delta.x > 0f ? 1 : -1;
        attackDirection = delta.normalized;
    }
    private void StartRecovery()
    {
        State = BehaviourState.Recovery; stateUntil = Time.time + recovery;
        nextAttack = Time.time + Mathf.Max(cooldown, recovery);
    }
    private bool HoldMeleeDistance()
    {
        if (kind == BlightType.Spora || !navigator.IsGrounded || navigator.IsTraversing || !CanSeePlayer())
        { holdingMeleeDistance = false; return false; }
        Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
        if (Mathf.Abs(delta.y) > verticalTolerance) { holdingMeleeDistance = false; return false; }
        float distance = Mathf.Abs(delta.x);
        if (holdingMeleeDistance) holdingMeleeDistance = distance <= attackRange + meleeResumeMargin;
        else holdingMeleeDistance = distance <= attackRange * meleeStopFraction + 0.02f;
        return holdingMeleeDistance;
    }
    private void Update()
    {
        if (health.IsDead) return;
        if (target == null) FindPlayer();
        move = 0f;
        useNavigation = false;
        if (health.LastHitTime > observedHitTime)
        {
            observedHitTime = health.LastHitTime;
            navigator.Interrupt(); holdingMeleeDistance = false; impactUntil = 0f;
            State = BehaviourState.Recovery;
            stateUntil = Mathf.Max(Time.time + recovery, health.LastHitTime + hitReactionDuration);
            nextAttack = Mathf.Max(nextAttack, stateUntil);
        }
        if (Time.time < health.LastHitTime + hitReactionDuration) { UpdateVisuals(); return; }
        if (State == BehaviourState.Windup)
        {
            if (!attackDirectionLocked)
            {
                // Lock before sampling a new aim: a late frame must not retarget
                // an attack after the committed portion has already begun.
                attackDirectionLocked = Time.time - windupStarted >= windup * attackDirectionLockFraction;
                if (!attackDirectionLocked && target != null) AimAtPlayer();
            }
            if (!InAttackRange()) StartRecovery();
            else if (Time.time >= stateUntil) { Attack(); StartRecovery(); }
        }
        else if (State == BehaviourState.Recovery)
        {
            if (Time.time >= stateUntil) State = BehaviourState.Patrol;
        }
        else
        {
            bool nearby = PlayerNearby();
            bool sees = nearby && CanSeePlayer();
            if (nearby && kind != BlightType.Spora)
            {
                // Nearby moving enemies can pursue around cover; damage still requires sight.
                lastDetected = Time.time;
                lastKnownFeet = new Vector2(TargetPosition.x, targetShape != null ? targetShape.bounds.min.y : TargetPosition.y);
            }
            if (nearby) AimAtPlayer();
            if (sees && InAttackRange() && Time.time >= nextAttack && !navigator.IsTraversing && (kind == BlightType.Spora || navigator.IsGrounded))
            {
                State = BehaviourState.Windup; windupStarted = Time.time; stateUntil = Time.time + windup;
                attackDirectionLocked = false; navigator.Stop();
            }
            else if (kind != BlightType.Spora)
            {
                bool pursue = nearby || Time.time - lastDetected < pursuitMemory;
                if (pursue)
                {
                    State = BehaviourState.Chase;
                    if (!HoldMeleeDistance())
                    { navigator.SetDestination(lastKnownFeet); useNavigation = true; }
                    else navigator.Stop();
                }
                else
                {
                    if (State == BehaviourState.Chase) patrolCenter = transform.position.x;
                    State = BehaviourState.Patrol; holdingMeleeDistance = false; navigator.Stop();
                    if (patrolRadius > 0f)
                    {
                        if (facing > 0 && transform.position.x >= patrolCenter + patrolRadius - 0.02f ||
                            facing < 0 && transform.position.x <= patrolCenter - patrolRadius + 0.02f) facing *= -1;
                        move = facing * movementSpeed * 0.55f;
                    }
                }
            }
            else State = BehaviourState.Patrol;
        }
        UpdateVisuals();
    }
    private void UpdateVisuals()
    {
        if (visuals != null)
        {
            float pulse = State == BehaviourState.Windup ? 1f + 0.12f * Mathf.Sin(Time.time * 28f) : 1f;
            visuals.localScale = new Vector3(visualScale.x * facing * pulse, visualScale.y * pulse, visualScale.z);
        }
        if (attackVisual != null)
        {
            bool windupActive = State == BehaviourState.Windup;
            bool impactActive = State == BehaviourState.Recovery && Time.time < impactUntil;
            attackVisual.SetActive(windupActive || impactActive);
            Color tint = impactActive ? Color.white : attackDirectionLocked ? committedColor : windupColor;
            for (int i = 0; i < cueRenderers.Length; i++)
                cueRenderers[i].color = new Color(tint.r, tint.g, tint.b, cueAlpha[i]);
        }
    }
    private bool SafeToMove(int direction)
    {
        var bounds = shape.bounds;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayers);
        Vector2 edge = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.18f), bounds.min.y + 0.12f);
        int count = Physics2D.Raycast(edge, Vector2.down, filter, rays, 0.45f);
        bool floor = false;
        for (int i = 0; i < count; i++)
            if (rays[i].rigidbody != body && rays[i].normal.y > 0.5f && rays[i].collider.GetComponentInParent<Movement>() == null && rays[i].collider.GetComponentInParent<BlightEnemy>() == null) floor = true;
        if (!floor) return false;
        count = Physics2D.BoxCast(bounds.center, new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.7f), 0f, Vector2.right * direction, filter, rays, 0.2f);
        for (int i = 0; i < count; i++) if (rays[i].rigidbody != body && rays[i].normal.y < 0.5f) return false;
        return true;
    }
    private void FixedUpdate()
    {
        if (health.IsDead) return;
        if (Time.time < health.LastHitTime + hitReactionDuration) { navigator.Interrupt(); return; }
        if (useNavigation)
        {
            move = navigator.Tick(Time.fixedDeltaTime, movementSpeed);
            if (!navigator.IsTraversing && target != null && CanSeePlayer())
            {
                Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
                if (Mathf.Abs(delta.y) <= verticalTolerance && move * delta.x > 0f)
                    move = Mathf.Sign(move) * Mathf.Min(Mathf.Abs(move), Mathf.Max(0f, Mathf.Abs(delta.x) - attackRange * meleeStopFraction) / Time.fixedDeltaTime);
            }
            if (Mathf.Abs(move) > 0.05f) facing = move > 0f ? 1 : -1;
            ApplyMovement(move);
            return;
        }
        if (move != 0f && !SafeToMove(move > 0f ? 1 : -1))
        {
            move = 0f;
            if (State == BehaviourState.Patrol) facing *= -1;
        }
        if (State == BehaviourState.Patrol && move != 0f)
        {
            float boundary = patrolCenter + Mathf.Sign(move) * patrolRadius;
            move = Mathf.Sign(move) * Mathf.Min(Mathf.Abs(move), Mathf.Abs(boundary - transform.position.x) / Time.fixedDeltaTime);
        }
        ApplyMovement(move);
    }
    private void ApplyMovement(float desired)
    {
        float velocity = body.linearVelocity.x;
        if (!navigator.IsTraversing && desired != 0f)
        {
            if (velocity * desired < 0f) velocity = 0f;
            if (Mathf.Abs(desired) > Mathf.Abs(velocity)) desired = Mathf.MoveTowards(velocity, desired, groundAcceleration * Time.fixedDeltaTime);
        }
        // Stops and safety constraints remain immediate; airborne links keep their validated velocity.
        body.linearVelocity = new Vector2(desired, body.linearVelocity.y);
    }
    private void Attack()
    {
        if (!InAttackRange() || health.IsDead || Time.time < health.LastHitTime + hitReactionDuration) return;
        Vector2 delta = TargetPosition - (Vector2)shape.bounds.center;
        if (kind != BlightType.Spora && delta.x * facing < -0.05f) return;
        if (kind == BlightType.Spora && attackDirectionLocked && Vector2.Dot(delta.normalized, attackDirection) < 0.9f) return;
        AttacksPerformed++;
        impactUntil = Time.time + Mathf.Min(0.12f, recovery * 0.3f);
        if (kind == BlightType.Spora)
        {
            if (sporePrefab == null) return;
            Vector2 direction = attackDirection;
            float clearance = shape.bounds.extents.magnitude + 0.25f;
            var projectile = Instantiate(sporePrefab, (Vector2)shape.bounds.center + direction * clearance, Quaternion.identity);
            projectile.Launch(direction, projectileSpeed, projectileRange, body, damage, target.transform);
            return;
        }
        Vector2 center = (Vector2)shape.bounds.center + Vector2.right * facing * attackRange * 0.5f;
        var filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.OverlapBox(center, new Vector2(attackRange + shape.bounds.size.x, verticalTolerance * 2f), 0f, filter, overlaps);
        struck.Clear();
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i].GetComponentInParent<Movement>() != target) continue;
            var receiver = DamageUtility.Find(overlaps[i]);
            if (receiver != null && struck.Add(receiver)) receiver.TakeDamage(damage, Vector2.right * facing, kind == BlightType.Collusus ? 5f : 1f);
        }
    }
}
