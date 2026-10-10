using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Level01Recovery : MonoBehaviour
{
    public Transform[] checkpoints;
    private int current;
    private Rigidbody2D body;

    private void Awake() { body = GetComponent<Rigidbody2D>(); }

    private void FixedUpdate()
    {
        if (checkpoints == null || checkpoints.Length == 0) return;
        var movement = GetComponent<Movement>();
        if (movement != null && movement.IsGrounded)
            for (int i = current + 1; i < checkpoints.Length; i++)
                if (Mathf.Abs(transform.position.x - checkpoints[i].position.x) < 3f &&
                    Mathf.Abs(transform.position.y - checkpoints[i].position.y) < 2f)
                    current = i;
        if (transform.position.y >= checkpoints[current].position.y - 12f) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = checkpoints[current].position;
    }
}
