using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Level02ThornRespawn : MonoBehaviour
{
    public Transform entrance;
    private Rigidbody2D body;
    private float lastReturn = float.NegativeInfinity;
    public event System.Action ReturnedToEntrance;

    private void Awake() { body = GetComponent<Rigidbody2D>(); }

    public void ReturnToEntrance()
    {
        if (Time.unscaledTime - lastReturn < 0.2f) return;
        if (!LevelRestartOnDeath.RestartScene(gameObject.scene, this)) return;
        lastReturn = Time.unscaledTime;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0;
        // Keep the serialized entrance reference and notification for compatibility.
        // The saved scene now restores the player and all other level objects.
        ReturnedToEntrance?.Invoke();
    }
}
