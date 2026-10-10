using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DemoCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float levelLeft = -32f;
    [SerializeField] private float levelRight = 32f;
    [SerializeField, Min(0.1f)] private float followSpeed = 6f;
    private Camera view;

    private void Awake() => view = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (target == null) return;
        float halfWidth = view.orthographicSize * view.aspect;
        float minimum = levelLeft + halfWidth;
        float maximum = levelRight - halfWidth;
        float desired = minimum <= maximum ? Mathf.Clamp(target.position.x, minimum, maximum) : (levelLeft + levelRight) * 0.5f;
        var position = transform.position;
        position.x = Mathf.Lerp(position.x, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        position.x = minimum <= maximum ? Mathf.Clamp(position.x, minimum, maximum) : desired;
        transform.position = position;
    }
}
