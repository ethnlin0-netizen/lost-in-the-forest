using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlaceholderProjectile : MonoBehaviour
{
    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private Rigidbody2D owner;
    private CircleCollider2D shape;
    private Vector2 direction;
    private float speed;
    private float remainingRange;
    private bool launched;
    private bool spent;

    public void Launch(Vector2 aim, float travelSpeed, float range, Rigidbody2D shooter)
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<CircleCollider2D>();
        owner = shooter;
        direction = aim.normalized;
        speed = travelSpeed;
        remainingRange = range;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || spent) return;
        float distance = Mathf.Min(speed * Time.fixedDeltaTime, remainingRange);
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        float radius = shape.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        int count = Physics2D.CircleCast(body.position, radius, direction, filter, hits, distance);
        Collider2D nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if ((owner != null && hit.rigidbody == owner) || hit.rigidbody == body) continue;
            if (hit.distance < nearestDistance)
            {
                nearest = hit.collider;
                nearestDistance = hit.distance;
            }
        }

        if (nearest != null)
        {
            var target = nearest.GetComponentInParent<PracticeTarget>();
            if (target != null) target.ReceiveHit(false, direction, 0f);
            Spend();
            return;
        }

        body.MovePosition(body.position + direction * distance);
        remainingRange -= distance;
        if (remainingRange <= 0f) Spend();
    }

    private void Spend()
    {
        spent = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
