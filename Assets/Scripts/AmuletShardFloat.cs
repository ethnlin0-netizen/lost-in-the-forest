using UnityEngine;

/// <summary>Bobs the artwork while the parent pickup trigger stays fixed.</summary>
public sealed class AmuletShardFloat : MonoBehaviour
{
    [SerializeField, Min(0f)] private float amplitude = 0.12f;
    [SerializeField, Min(0.1f)] private float period = 2.4f;

    private Vector3 restPosition;
    private float startedAt;

    private void OnEnable()
    {
        restPosition = transform.localPosition;
        startedAt = Time.time;
    }

    private void LateUpdate()
    {
        Vector3 up = transform.parent != null
            ? transform.parent.InverseTransformVector(Vector3.up)
            : Vector3.up;
        float phase = (Time.time - startedAt) * (2f * Mathf.PI / Mathf.Max(0.1f, period));
        transform.localPosition = restPosition + up * (Mathf.Sin(phase) * amplitude);
    }

    private void OnDisable()
    {
        transform.localPosition = restPosition;
    }
}
