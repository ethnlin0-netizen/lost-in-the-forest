using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class BlightBossHazard : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 4.5f;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField] private bool followsGround;
    [SerializeField] private LayerMask collisionLayers = ~0;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private BlightBoss owner;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private Vector2 direction;
    private ContactFilter2D filter;
    private float remaining;
    public bool Spent { get; private set; }
    public BlightBoss Owner => owner;
    public bool FollowsGround => followsGround;

    public void Launch(BlightBoss source, Vector2 aim, float travelSpeed, float duration, float amount)
    {
        owner = source; direction = aim.normalized; speed = travelSpeed;
        remaining = duration; damage = amount;
    }
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; shape.isTrigger = true;
        filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(collisionLayers);
        remaining = lifetime;
    }
    private void FixedUpdate()
    {
        if (Spent) return;
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled) { Cancel(); return; }
        float dt = Time.fixedDeltaTime; remaining -= dt;
        if (remaining <= 0f) { Cancel(); return; }
        Vector2 delta = direction * speed * dt;
        if (followsGround)
        {
            Vector2 probe = body.position + delta + Vector2.up * 0.5f;
            int groundCount = Physics2D.Raycast(probe, Vector2.down, filter, hits, 1.1f);
            float top = float.NegativeInfinity;
            for (int i = 0; i < groundCount; i++)
                if (IsTerrain(hits[i]) && hits[i].normal.y > 0.5f) top = Mathf.Max(top, hits[i].point.y);
            float feet = shape.bounds.min.y;
            if (float.IsNegativeInfinity(top) || Mathf.Abs(top - feet) > 0.25f) { Cancel(); return; }
            delta.y = top + shape.bounds.extents.y + 0.025f - body.position.y;
        }
        int count = Physics2D.BoxCast(shape.bounds.center, shape.bounds.size, 0f, delta.normalized, filter, hits, delta.magnitude);
        int nearest = -1; float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c == null || c.attachedRigidbody == body || c.transform.IsChildOf(owner.transform)) continue;
            // Horizontal shockwaves touch the floor; its upward normal is support.
            if (followsGround && IsTerrain(hits[i]) && hits[i].normal.y > 0.5f) continue;
            if (hits[i].distance < distance) { distance = hits[i].distance; nearest = i; }
        }
        if (nearest >= 0)
        {
            var hit = hits[nearest];
            var player = hit.collider.GetComponentInParent<Movement>();
            if (player != null && player.isActiveAndEnabled && owner.HasLineOfSight(body.position, hit.point))
                DamageUtility.Find(player)?.TakeDamage(damage, direction, followsGround ? 2f : 0.5f);
            Cancel(); return;
        }
        body.MovePosition(body.position + delta);
    }
    private bool IsTerrain(RaycastHit2D hit) => hit.collider != null &&
        (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static);
    public void Cancel()
    {
        if (Spent) return;
        Spent = true; gameObject.SetActive(false); Destroy(gameObject);
    }
}
