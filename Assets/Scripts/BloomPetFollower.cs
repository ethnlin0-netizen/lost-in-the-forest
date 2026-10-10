using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(EnemyNavigator2D))]
[DefaultExecutionOrder(40)]
public sealed class BloomPetFollower : MonoBehaviour
{
    [Header("Walking follow")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 6f;
    [SerializeField, Min(0.1f)] private float catchUpSpeed = 8f;
    [SerializeField, Min(0.1f)] private float catchUpDistance = 4f;
    [SerializeField, Min(0.1f)] private float followSpacing = 1.25f;
    [SerializeField, Min(0.1f)] private float firstPetDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.12f;
    [SerializeField, Min(0.02f)] private float resumeDistance = 0.35f;
    [SerializeField, Min(0.1f)] private float acceleration = 30f;

    [Header("Terrain safety")]
    [Tooltip("Only solid colliders with no Rigidbody2D, or a Static Rigidbody2D, are walkable terrain.")]
    [SerializeField] private LayerMask terrainLayers = ~0;
    [SerializeField, Min(0.01f)] private float edgeLookAhead = 0.12f;
    [SerializeField, Min(0.05f)] private float groundProbeDepth = 0.18f;
    [SerializeField, Min(0.001f)] private float skinWidth = 0.025f;
    [SerializeField, Min(0.1f)] private float maximumFallSpeed = 30f;

    [Header("Jump following")]
    [SerializeField] private bool mirrorPlayerJumps = true;
    [SerializeField, Min(0f)] private float jumpDelayPerPet = 0.08f;
    [SerializeField, Min(0.1f)] private float gravityMultiplier = 3f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField, Min(0.05f)] private float recruitmentFlashDuration = 0.35f;
    [SerializeField] private bool spriteFacesRight = true;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private ContactFilter2D terrainFilter;
    private BloomPetGroup group;
    private Collider2D playerShape;
    private PlayerHealth playerHealth;
    private EnemyNavigator2D navigator;
    private PlatformNavigation2D navigationWorld;
    private BoxCollider2D mirroredJumpSupport;
    private Color restingColor;
    private Vector3 restingVisualScale;
    private float flashRemaining, horizontalSpeed, fallSpeed;
    private float pendingJumpSpeed, jumpDelay;
    private int seenJumpRevision;
    private bool usedAirJump;
    private bool walking;

    public Movement Owner { get; private set; }
    public bool IsRecruited => Owner != null;
    public int FollowSlot => group == null ? -1 : group.SlotOf(this);
    public bool IsWalking => walking && Mathf.Abs(horizontalSpeed) > 0.01f;
    public bool IsGrounded { get; private set; }
    public Vector2 Feet => new Vector2(shape.bounds.center.x, shape.bounds.min.y);
    public Vector2 Destination { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<BoxCollider2D>();
        // A trigger-only kinematic hull cannot push actors or be mistaken for
        // ground by the player's existing non-trigger combat/ground queries.
        body.bodyType = RigidbodyType2D.Kinematic;
        // Kinematic gravity is integrated below. The navigator uses this scale
        // when validating ballistic routes, so it must match that integration.
        body.gravityScale = gravityMultiplier;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        shape.isTrigger = true;
        terrainFilter = new ContactFilter2D { useTriggers = false };
        terrainFilter.SetLayerMask(terrainLayers);
        navigator = GetComponent<EnemyNavigator2D>();
        navigationWorld = FindAnyObjectByType<PlatformNavigation2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            restingColor = visual.color;
            restingVisualScale = visual.transform.localScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryRecruit(other);
    private void OnTriggerStay2D(Collider2D other) => TryRecruit(other);

    private void TryRecruit(Collider2D other)
    {
        if (IsRecruited || other.attachedRigidbody == null) return;
        // A projectile's shooter reference does not make it the player body.
        var candidate = other.attachedRigidbody.GetComponent<Movement>();
        if (candidate == null || !candidate.isActiveAndEnabled) return;
        var health = candidate.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;
        Owner = candidate;
        playerShape = candidate.GetComponent<Collider2D>();
        playerHealth = health;
        group = candidate.GetComponent<BloomPetGroup>();
        if (group == null) group = candidate.gameObject.AddComponent<BloomPetGroup>();
        group.Register(this);
        seenJumpRevision = group.JumpRevision;
        flashRemaining = recruitmentFlashDuration;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 feet = Feet;
        IsGrounded = fallSpeed <= 0.1f && GroundBelow(feet.x, feet.y, Mathf.Min(0.06f, groundProbeDepth));
        if (IsGrounded) { fallSpeed = 0f; usedAirJump = false; mirroredJumpSupport = null; }
        // MovePosition does not reliably publish its kinematic vertical speed.
        // Supply our integrated speed so the navigator recognizes landings.
        body.linearVelocity = new Vector2(horizontalSpeed, fallSpeed);
        float desiredSpeed = 0f;
        bool usingRoute = false;
        if (IsRecruited && Owner.isActiveAndEnabled && group != null && playerShape != null &&
            (playerHealth == null || !playerHealth.IsDead))
        {
            Destination = FollowDestination();
            float error = Destination.x - feet.x;
            float speed = Mathf.Abs(error) >= catchUpDistance ? catchUpSpeed : movementSpeed;
            if (mirrorPlayerJumps && seenJumpRevision != group.JumpRevision)
            {
                seenJumpRevision = group.JumpRevision;
                pendingJumpSpeed = group.LatestJumpSpeed;
                jumpDelay = jumpDelayPerPet * (FollowSlot + 1);
            }
            if (pendingJumpSpeed > 0f)
            {
                jumpDelay -= dt;
                if (jumpDelay <= 0f)
                {
                    if (!navigator.IsTraversing && (IsGrounded || !usedAirJump))
                    {
                        if (IsGrounded && navigationWorld != null) mirroredJumpSupport = navigationWorld.SupportAt(feet);
                        if (!IsGrounded) usedAirJump = true;
                        navigator.Stop();
                        fallSpeed = pendingJumpSpeed;
                        IsGrounded = false;
                    }
                    pendingJumpSpeed = 0f;
                }
            }

            bool sameSurface = IsGrounded && Mathf.Abs(Destination.y - feet.y) < 0.2f;
            if (walking && sameSurface && Mathf.Abs(error) <= stoppingDistance) walking = false;
            else if (!walking && (!sameSurface || Mathf.Abs(error) >= resumeDistance)) walking = true;
            navigator.SetDestination(Destination);
            int jumps = navigator.JumpsPerformed;
            // Stable speed keeps the navigation graph cached; catch-up is for
            // ordinary walking and mirrored airborne following only.
            float routedSpeed = navigator.Tick(dt, movementSpeed);
            usingRoute = navigator.IsTraversing;
            if (navigator.JumpsPerformed != jumps)
            {
                fallSpeed = body.linearVelocity.y;
                IsGrounded = false;
            }
            if (navigator.IsTraversing) desiredSpeed = routedSpeed;
            else if (!IsGrounded)
            {
                float airX = Destination.x;
                // A reactive hop stays over its takeoff platform. Cross-platform
                // flight is reserved for the navigator's validated jump/drop.
                if (mirroredJumpSupport != null)
                {
                    float margin = shape.bounds.extents.x + skinWidth * 2f;
                    airX = Mathf.Clamp(airX, mirroredJumpSupport.bounds.min.x + margin,
                        mirroredJumpSupport.bounds.max.x - margin);
                }
                float airError = airX - feet.x;
                desiredSpeed = Mathf.Sign(airError) * Mathf.Min(speed, Mathf.Abs(airError) / dt);
            }
            else if (walking)
            {
                bool clearWalk = sameSurface && (navigationWorld == null ||
                    navigationWorld.ClearSegment(feet, new Vector2(Destination.x, feet.y), shape.bounds.size));
                usingRoute = !clearWalk;
                desiredSpeed = clearWalk ? Mathf.Sign(error) * Mathf.Min(speed,
                    Mathf.Max(0f, Mathf.Abs(error) - stoppingDistance) / dt) : routedSpeed;
            }
        }
        else { walking = false; navigator.Stop(); pendingJumpSpeed = 0f; }

        // Validated jumps require their exact planned horizontal speed.
        horizontalSpeed = navigator.IsTraversing ? desiredSpeed :
            desiredSpeed == 0f ? 0f : Mathf.MoveTowards(horizontalSpeed, desiredSpeed, acceleration * dt);
        float dx = horizontalSpeed * dt;
        // Routed walking has its own hull/edge checks and must be allowed to
        // reach the validated takeoff node just inside a platform's lip.
        if (IsGrounded && !usingRoute && Mathf.Abs(dx) > 0.0001f)
        {
            float direction = Mathf.Sign(dx);
            float leadingFoot = feet.x + direction * (shape.bounds.extents.x + edgeLookAhead + Mathf.Abs(dx));
            if (!GroundBelow(leadingFoot, feet.y, groundProbeDepth)) dx = horizontalSpeed = 0f;
        }
        if (Mathf.Abs(horizontalSpeed) > 0.01f && visual != null)
            visual.flipX = (horizontalSpeed < 0f) == spriteFacesRight;
        fallSpeed = IsGrounded ? 0f : Mathf.Max(-maximumFallSpeed,
            fallSpeed + Physics2D.gravity.y * gravityMultiplier * dt);
        body.MovePosition(body.position + SweepMove(new Vector2(dx, fallSpeed * dt)));
    }

    private Vector2 FollowDestination()
    {
        float distance = firstPetDistance + FollowSlot * followSpacing;
        var ownerFeet = new Vector2(playerShape.bounds.center.x, playerShape.bounds.min.y);
        var support = navigationWorld == null ? null : navigationWorld.SupportAt(ownerFeet, 0.1f);
        if (support != null)
        {
            // Compress the trailing group on a narrow landing, retaining a
            // distinct position for each pet instead of clamping all to a lip.
            float margin = shape.bounds.extents.x + 0.08f;
            float available = Owner.FacingDirection > 0 ? ownerFeet.x - support.bounds.min.x - margin :
                support.bounds.max.x - margin - ownerFeet.x;
            float spacing = Mathf.Max(0f, available) / Mathf.Max(1, group.Count);
            distance = Mathf.Min(firstPetDistance, spacing) + FollowSlot * Mathf.Min(followSpacing, spacing);
        }
        return new Vector2(ownerFeet.x - Owner.FacingDirection * distance, ownerFeet.y);
    }

    private bool GroundBelow(float x, float footY, float depth)
    {
        int count = Physics2D.Raycast(new Vector2(x, footY + skinWidth), Vector2.down, terrainFilter, hits, depth + skinWidth);
        for (int i = 0; i < count; i++)
            if (IsTerrain(hits[i]) && hits[i].normal.y > 0.55f) return true;
        return false;
    }

    private Vector2 SweepMove(Vector2 remaining)
    {
        Vector2 travelled = Vector2.zero;
        // Sweep the whole diagonal step, then slide along a floor/wall. This
        // catches ceilings too; trigger hulls do not receive collision response.
        for (int pass = 0; pass < 3 && remaining.sqrMagnitude > 0.0000001f; pass++)
        {
            float distance = remaining.magnitude;
            Vector2 direction = remaining / distance;
            int count = Physics2D.BoxCast((Vector2)shape.bounds.center + travelled,
                shape.bounds.size, 0f, direction, terrainFilter, hits, distance + skinWidth);
            float allowed = distance;
            Vector2 normal = Vector2.zero;
            for (int i = 0; i < count; i++)
                if (IsTerrain(hits[i]) && Vector2.Dot(hits[i].normal, direction) < -0.001f)
                {
                    float hitDistance = Mathf.Max(0f, hits[i].distance - skinWidth);
                    if (hitDistance < allowed) { allowed = hitDistance; normal = hits[i].normal; }
                }
            travelled += direction * allowed;
            remaining -= direction * allowed;
            if (normal == Vector2.zero) break;
            remaining -= normal * Mathf.Min(0f, Vector2.Dot(remaining, normal));
            if (Mathf.Abs(normal.y) > 0.5f) fallSpeed = 0f;
            if (Mathf.Abs(normal.x) > 0.5f) horizontalSpeed = 0f;
        }
        return travelled;
    }

    private bool IsTerrain(RaycastHit2D hit) => hit.collider != null && hit.collider != shape &&
        (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static);

    private void Update()
    {
        if (visual == null) return;
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
        float pulse = recruitmentFlashDuration > 0f ? flashRemaining / recruitmentFlashDuration : 0f;
        visual.color = Color.Lerp(restingColor, new Color(0.6f, 1f, 0.65f, restingColor.a), pulse);
        visual.transform.localScale = restingVisualScale * (1f + 0.12f * Mathf.Sin(pulse * Mathf.PI));
    }

    private void OnDestroy()
    {
        if (group != null) group.Unregister(this);
    }

    private void OnValidate()
    {
        resumeDistance = Mathf.Max(resumeDistance, stoppingDistance + 0.05f);
        catchUpSpeed = Mathf.Max(catchUpSpeed, movementSpeed);
    }
}
