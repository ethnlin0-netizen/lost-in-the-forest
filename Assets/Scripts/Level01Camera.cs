using UnityEngine;

public class Level01Camera : MonoBehaviour
{
    public Transform target;
    public Vector2 minimum = new Vector2(2f, 2f);
    public Vector2 maximum = new Vector2(112f, 22f);

    private void LateUpdate()
    {
        if (target == null) return;
        // Track vertically as well as horizontally for the hill climb.
        transform.position = new Vector3(
            Mathf.Clamp(target.position.x + 2f, minimum.x, maximum.x),
            Mathf.Clamp(target.position.y + 1.5f, minimum.y, maximum.y), -10f);
    }
}
