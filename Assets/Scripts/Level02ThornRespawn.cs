using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Level02ThornRespawn : MonoBehaviour
{
    public Transform entrance;
    private Rigidbody2D body;
    private float lastReturn = float.NegativeInfinity;

    private void Awake() { body = GetComponent<Rigidbody2D>(); }

    public void ReturnToEntrance()
    {
        if (entrance == null || Time.time - lastReturn < 0.2f) return;
        lastReturn = Time.time;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0;
        body.position = entrance.position;
        transform.position = entrance.position;
    }
}
