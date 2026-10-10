using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level02ThornHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) { ReturnPlayer(other); }
    private void OnTriggerStay2D(Collider2D other) { ReturnPlayer(other); }

    private static void ReturnPlayer(Collider2D other)
    {
        // Only the player carrying this scene-specific component is returned.
        var body = other.attachedRigidbody;
        if (body != null)
        {
            var respawn = body.GetComponent<Level02ThornRespawn>();
            if (respawn != null) respawn.ReturnToEntrance();
        }
    }
}
