using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(BlightBossHealth))]
public sealed class BlightBoss : MonoBehaviour
{
    public enum BossState { Waiting, Pursuit, Windup, Lunge, Recovery, PhaseTransition, Dead }
    public enum AttackKind { Slam, Lunge, Spores }
    [Header("Pursuit")]
    [SerializeField, Min(0f)] private float movementSpeed = 1.2f;
    [SerializeField, Min(0f)] private float phaseTwoMovementSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float detectionRange = 20f;
    [SerializeField, Min(0.1f)] private float verticalTolerance = 1.35f;
    [SerializeField] private float arenaLeft = -8.5f, arenaRight = 8.5f;
    [SerializeField] private LayerMask terrainLayers = ~0;
    [Header("Rock slam")]
    [SerializeField, Min(0.1f)] private float slamRange = 2.2f;
    [SerializeField, Min(0f)] private float slamDamage = 20f;
    [SerializeField, Min(0.1f)] private float slamWindup = 0.9f;
    [SerializeField, Min(0.1f)] private float slamRecovery = 1.2f;
    [SerializeField, Min(0.1f)] private float slamCooldown = 2.6f;
    [SerializeField] private BlightBossHazard shockwavePrefab;
    [SerializeField, Min(0.1f)] private float shockwaveSpeed = 4f;
    [SerializeField, Min(0.1f)] private float shockwaveLifetime = 2.6f;
    [SerializeField, Min(0f)] private float shockwaveDamage = 10f;
    [Header("Flytrap lunge")]
    [SerializeField, Min(0.1f)] private float lungeRange = 5.5f;
    [SerializeField, Min(0f)] private float lungeDamage = 16f;
    [SerializeField, Min(0.1f)] private float lungeWindup = 0.75f;
    [SerializeField, Min(0.1f)] private float lungeDistance = 2.6f;
    [SerializeField, Min(0.1f)] private float lungeSpeed = 6f;
    [SerializeField, Min(0.1f)] private float lungeRecovery = 1.05f;
    [SerializeField, Min(0.1f)] private float lungeCooldown = 2.4f;
    [Header("Spore volley")]
    [SerializeField, Min(0.1f)] private float projectileRange = 14f;
    [SerializeField, Min(0f)] private float projectileDamage = 10f;
    [SerializeField, Min(0.1f)] private float projectileWindup = 1f;
    [SerializeField, Min(0.1f)] private float projectileRecovery = 0.8f;
    [SerializeField, Min(0.1f)] private float projectileCooldown = 3.4f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3.2f;
    [SerializeField, Range(10f, 45f)] private float projectileSpread = 22f;
    [SerializeField] private BlightBossHazard sporePrefab;
    [Header("Timing and phase")]
    [SerializeField, Range(0.2f, 0.9f)] private float directionLockFraction = 0.6f;
    [SerializeField, Range(0.1f, 0.9f)] private float phaseThreshold = 0.5f;
    [SerializeField, Min(0.1f)] private float phaseTransitionDuration = 1.3f;
    [SerializeField, Min(0.1f)] private float comboDelay = 0.65f;
    [SerializeField, Min(0f)] private float vulnerabilityDuration = 0.85f;
    [SerializeField, Min(1f)] private float vulnerabilityMultiplier = 1.5f;
    [Header("Visual rig")]
    [SerializeField] private Transform visualRoot, mouth, slamArm, mushroomCaps;
    [SerializeField] private SpriteRenderer chestFragment;
    [SerializeField] private SpriteRenderer[] corruptionVeins;
    [SerializeField] private Transform attackCue;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private readonly Collider2D[] overlaps = new Collider2D[32];
    private readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();
    private readonly List<BlightBossHazard> hazards = new List<BlightBossHazard>();
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private BlightBossHealth health;
    private Movement player;
    private Collider2D playerShape;
    private ContactFilter2D filter;
    private Vector3 mouthRest, armRest, capsRest, visualRestPosition;
    private SpriteRenderer[] capRenderers;
    private float timer, cooldown, vulnerability, lungeRemaining, phaseElapsed;
    private float windupDuration;
    private int facing = -1, cycle, slamCount;
    private Vector2 lockedAim;
    private bool directionLocked, queuedVolley;
    public BossState State { get; private set; } = BossState.Waiting;
    public AttackKind CurrentAttack { get; private set; }
    public bool PhaseTwo { get; private set; }
    public bool IsDead => health == null || health.IsDead;
    public bool ChestExposed => !IsDead && vulnerability > 0f;
    public float IncomingDamageMultiplier => ChestExposed ? vulnerabilityMultiplier : 1f;
    public int PhaseTransitions { get; private set; }
    public int SlamsPerformed { get; private set; }
    public int LungesPerformed { get; private set; }
    public int VolleysPerformed { get; private set; }
    public int Facing => facing;
    public bool DirectionLocked => directionLocked;
    public int ActiveHazards => hazards.FindAll(h => h != null && !h.Spent).Count;
    public Vector2 Feet => new Vector2(shape.bounds.center.x, shape.bounds.min.y);
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>(); health = GetComponent<BlightBossHealth>();
        filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(terrainLayers);
        mouthRest = mouth != null ? mouth.localScale : Vector3.one;
        armRest = slamArm != null ? slamArm.localPosition : Vector3.zero;
        capsRest = mushroomCaps != null ? mushroomCaps.localScale : Vector3.one;
        visualRestPosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
        capRenderers = mushroomCaps != null ? mushroomCaps.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];
        health.HealthChanged += CheckPhase; health.Died += Die;
        FindPlayer(); RefreshVisuals();
    }
    private void FindPlayer() { player = FindAnyObjectByType<Movement>(); playerShape = player != null ? player.GetComponent<Collider2D>() : null; }
    private bool LivePlayer => player != null && player.isActiveAndEnabled && playerShape != null &&
        (player.GetComponent<PlayerHealth>() == null || !player.GetComponent<PlayerHealth>().IsDead);
    private float Gap => Mathf.Max(0f, Mathf.Abs(playerShape.bounds.center.x - shape.bounds.center.x) - shape.bounds.extents.x - playerShape.bounds.extents.x);
    private bool SameHeight => Mathf.Abs(playerShape.bounds.min.y - Feet.y) <= verticalTolerance;
    private bool Grounded => body.linearVelocity.y <= 0.1f && HasGround(Feet.x, Feet.y);
    private bool HasGround(float x, float y)
    {
        int count = Physics2D.Raycast(new Vector2(x, y + 0.08f), Vector2.down, filter, hits, 0.3f);
        for (int i = 0; i < count; i++) if (Terrain(hits[i]) && hits[i].normal.y > 0.5f) return true;
        return false;
    }
    private bool Terrain(RaycastHit2D hit) => hit.collider != null && hit.rigidbody != body &&
        (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static);
    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        int count = Physics2D.Raycast(from, delta.normalized, filter, hits, delta.magnitude);
        for (int i = 0; i < count; i++) if (Terrain(hits[i])) return false;
        return true;
    }
    private Vector2 ShotOrigin => new Vector2(shape.bounds.center.x + facing * (shape.bounds.extents.x + 0.25f), Feet.y + 1.15f);
    private bool CanAttack(AttackKind attack)
    {
        if (!LivePlayer || !Grounded) return false;
        if (Vector2.Distance(playerShape.bounds.center, shape.bounds.center) > detectionRange) return false;
        if (attack == AttackKind.Spores)
            return Vector2.Distance(ShotOrigin, playerShape.bounds.center) <= projectileRange && HasLineOfSight(ShotOrigin, playerShape.bounds.center);
        return SameHeight && Gap <= (attack == AttackKind.Slam ? slamRange : lungeRange) &&
            HasLineOfSight(new Vector2(shape.bounds.center.x, Feet.y + 0.7f), playerShape.bounds.center);
    }
    private void Aim()
    {
        float dx = playerShape.bounds.center.x - shape.bounds.center.x;
        if (Mathf.Abs(dx) > 0.05f) facing = dx > 0f ? 1 : -1;
        lockedAim = ((Vector2)playerShape.bounds.center - ShotOrigin).normalized;
    }
    private void BeginAttack(AttackKind attack)
    {
        CurrentAttack = attack; State = BossState.Windup; struck.Clear(); directionLocked = false; Aim();
        windupDuration = attack == AttackKind.Slam ? slamWindup : attack == AttackKind.Lunge ? lungeWindup : projectileWindup;
        timer = windupDuration;
    }
    private void FixedUpdate()
    {
        if (IsDead) return;
        float dt = Time.fixedDeltaTime;
        cooldown = Mathf.Max(0f, cooldown - dt); vulnerability = Mathf.Max(0f, vulnerability - dt);
        if (player == null) FindPlayer();
        float desired = 0f;
        if (State == BossState.PhaseTransition)
        {
            timer -= dt; phaseElapsed += dt;
            if (timer <= 0f) { State = BossState.Pursuit; cooldown = 1f; }
        }
        else if (!LivePlayer)
        {
            State = BossState.Waiting; queuedVolley = false; vulnerability = 0f;
        }
        else if (State == BossState.Windup)
        {
            timer -= dt;
            if (!directionLocked)
            {
                if (timer <= windupDuration * (1f - directionLockFraction)) directionLocked = true;
                else Aim();
            }
            if (!CanAttack(CurrentAttack)) Recover(false);
            else if (timer <= 0f) ExecuteAttack();
        }
        else if (State == BossState.Lunge)
        {
            float step = Mathf.Min(lungeSpeed * dt, lungeRemaining);
            float allowed = SafeDistance(facing, step);
            desired = facing * allowed / dt; lungeRemaining -= allowed;
            DamageMelee(lungeDamage, 1f);
            if (allowed < step - 0.001f || lungeRemaining <= 0.01f) Recover(true);
        }
        else if (State == BossState.Recovery)
        {
            timer -= dt;
            if (timer <= 0f) State = BossState.Pursuit;
        }
        else
        {
            bool detected = Vector2.Distance(playerShape.bounds.center, shape.bounds.center) <= detectionRange;
            State = detected ? BossState.Pursuit : BossState.Waiting;
            if (detected)
            {
                Aim();
                if (cooldown <= 0f)
                {
                    AttackKind choice = queuedVolley ? AttackKind.Spores : (AttackKind)(cycle % 3);
                    if (!CanAttack(choice)) choice = SameHeight && Gap <= slamRange ? AttackKind.Slam :
                        SameHeight && Gap <= lungeRange ? AttackKind.Lunge : AttackKind.Spores;
                    if (CanAttack(choice)) { BeginAttack(choice); queuedVolley = false; cycle++; }
                }
                if (State == BossState.Pursuit && Grounded && Gap > slamRange * 0.8f)
                {
                    float step = (PhaseTwo ? phaseTwoMovementSpeed : movementSpeed) * dt;
                    desired = facing * SafeDistance(facing, step) / dt;
                }
            }
        }
        body.linearVelocity = new Vector2(desired, body.linearVelocity.y);
        RefreshVisuals();
    }
    private float SafeDistance(int direction, float distance)
    {
        float center = shape.bounds.center.x;
        float boundary = direction > 0 ? arenaRight - shape.bounds.extents.x : arenaLeft + shape.bounds.extents.x;
        float allowed = Mathf.Min(distance, Mathf.Max(0f, (boundary - center) * direction));
        if (!HasGround(center + direction * (shape.bounds.extents.x + allowed + 0.08f), Feet.y)) return 0f;
        int count = shape.Cast(Vector2.right * direction, filter, hits, allowed + 0.04f);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != null && hits[i].rigidbody != body && Vector2.Dot(hits[i].normal, Vector2.right * direction) < -0.5f)
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hits[i].distance - 0.04f));
        return allowed;
    }
    private void ExecuteAttack()
    {
        if (CurrentAttack == AttackKind.Slam)
        {
            SlamsPerformed++; slamCount++; DamageMelee(slamDamage, slamRange);
            if (shockwavePrefab != null)
            {
                var wave = Instantiate(shockwavePrefab, new Vector2(ShotOrigin.x, Feet.y + 0.25f), Quaternion.identity);
                wave.Launch(this, Vector2.right * facing, shockwaveSpeed, shockwaveLifetime, shockwaveDamage); hazards.Add(wave);
            }
            queuedVolley = PhaseTwo && slamCount % 2 == 0; Recover(true);
        }
        else if (CurrentAttack == AttackKind.Lunge)
        {
            LungesPerformed++; State = BossState.Lunge; lungeRemaining = lungeDistance;
        }
        else { FireVolley(); Recover(false); }
    }
    private void FireVolley()
    {
        if (sporePrefab == null || !CanAttack(AttackKind.Spores)) return;
        VolleysPerformed++;
        for (int i = -1; i <= 1; i++)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, i * projectileSpread) * lockedAim;
            var shot = Instantiate(sporePrefab, ShotOrigin, Quaternion.identity);
            shot.Launch(this, direction, projectileSpeed, projectileLifetime, projectileDamage); hazards.Add(shot);
        }
        hazards.RemoveAll(h => h == null || h.Spent);
    }
    private void DamageMelee(float amount, float reach)
    {
        if (!LivePlayer || !SameHeight || playerShape.bounds.center.x * facing < shape.bounds.center.x * facing - 0.05f) return;
        Vector2 origin = new Vector2(shape.bounds.center.x, Feet.y + 0.7f);
        if (!HasLineOfSight(origin, playerShape.bounds.center)) return;
        Vector2 center = origin + Vector2.right * facing * (shape.bounds.extents.x + reach * 0.5f);
        int count = Physics2D.OverlapBox(center, new Vector2(reach, 1.4f), 0f, filter, overlaps);
        for (int i = 0; i < count; i++)
            if (overlaps[i].GetComponentInParent<Movement>() == player)
            {
                var receiver = DamageUtility.Find(overlaps[i]);
                if (receiver != null && struck.Add(receiver)) receiver.TakeDamage(amount, Vector2.right * facing, 2f);
            }
    }
    private void Recover(bool expose)
    {
        State = BossState.Recovery;
        timer = CurrentAttack == AttackKind.Slam ? slamRecovery : CurrentAttack == AttackKind.Lunge ? lungeRecovery : projectileRecovery;
        cooldown = queuedVolley ? timer + comboDelay : CurrentAttack == AttackKind.Slam ? slamCooldown :
            CurrentAttack == AttackKind.Lunge ? lungeCooldown : projectileCooldown;
        vulnerability = expose ? Mathf.Min(vulnerabilityDuration, timer) : 0f;
    }
    private void CheckPhase()
    {
        if (health.IsDead || PhaseTwo || health.CurrentHealth > health.MaximumHealth * phaseThreshold) return;
        PhaseTwo = true; PhaseTransitions++; State = BossState.PhaseTransition; timer = phaseTransitionDuration;
        phaseElapsed = 0f; slamCount = 0; vulnerability = 0f; queuedVolley = false; CancelHazards();
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
    private void RefreshVisuals()
    {
        if (visualRoot != null) visualRoot.localScale = new Vector3(facing, 1f, 1f);
        float progress = State == BossState.Windup ? Mathf.Clamp01(1f - timer / windupDuration) : 0f;
        if (visualRoot != null) visualRoot.localPosition = visualRestPosition +
            (State == BossState.Windup && CurrentAttack == AttackKind.Lunge ? Vector3.left * facing * progress * 0.22f : Vector3.zero);
        if (mouth != null) mouth.localScale = mouthRest * (State == BossState.Windup && CurrentAttack == AttackKind.Lunge ? 1f + progress * 0.3f : 1f);
        if (slamArm != null) slamArm.localPosition = armRest + Vector3.up * (State == BossState.Windup && CurrentAttack == AttackKind.Slam ? progress * 0.6f : 0f);
        if (mushroomCaps != null) mushroomCaps.localScale = capsRest * (1f +
            (State == BossState.Windup && CurrentAttack == AttackKind.Spores ? progress * 0.25f : PhaseTwo ? Mathf.Sin(Time.time * 4f) * 0.04f : 0f));
        foreach (var cap in capRenderers)
            if (cap != null && Time.time >= health.LastHitTime + 0.15f)
                cap.color = State == BossState.Windup && CurrentAttack == AttackKind.Spores ?
                    Color.Lerp(Color.white, new Color(1f, 0.45f, 1f), progress) : Color.white;
        if (chestFragment != null) { chestFragment.transform.localScale = Vector3.one * (ChestExposed ? 0.65f : 0.3f); chestFragment.color = ChestExposed ? Color.yellow : new Color(0.8f, 0.25f, 1f); }
        if (corruptionVeins != null) foreach (var vein in corruptionVeins) if (vein != null)
            vein.color = PhaseTwo ? new Color(1f, 0.25f, 0.9f) : new Color(0.45f, 0.15f, 0.65f);
        if (attackCue != null)
        {
            attackCue.gameObject.SetActive(State == BossState.Windup || State == BossState.PhaseTransition);
            attackCue.position = State == BossState.PhaseTransition ? (Vector3)(Feet + Vector2.up * 2f) :
                new Vector3(Feet.x + facing * (shape.bounds.extents.x + (CurrentAttack == AttackKind.Slam ? slamRange * 0.5f : lungeDistance * 0.5f)), Feet.y + 0.04f, 0f);
            attackCue.localScale = State == BossState.PhaseTransition ? new Vector3(4f, 0.2f, 1f) :
                new Vector3(CurrentAttack == AttackKind.Slam ? slamRange : CurrentAttack == AttackKind.Lunge ? lungeDistance : 1f, 0.12f + progress * 0.12f, 1f);
            var renderer = attackCue.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = State == BossState.PhaseTransition ? Color.magenta :
                directionLocked ? new Color(1f, 0.25f, 0.15f) : new Color(1f, 0.8f, 0.2f);
        }
    }
    // Health's normal Update owns hit flashing; cues must remain visible after it.
    private void LateUpdate() { if (!IsDead) RefreshVisuals(); }
    private void CancelHazards() { foreach (var h in hazards) if (h != null) h.Cancel(); hazards.Clear(); }
    private void Die()
    {
        State = BossState.Dead; vulnerability = 0f; queuedVolley = false; CancelHazards();
        if (attackCue != null) attackCue.gameObject.SetActive(false);
        enabled = false;
    }
    private void OnDisable() { CancelHazards(); if (body != null && body.simulated) body.linearVelocity = Vector2.zero; }
    private void OnDestroy() { if (health != null) { health.HealthChanged -= CheckPhase; health.Died -= Die; } }
}
