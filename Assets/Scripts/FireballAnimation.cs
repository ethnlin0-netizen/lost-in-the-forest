using UnityEngine;

/// <summary>Loops the flame frames and points the artwork along the projectile's existing aim.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FireballAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.01f)] private float framesPerSecond = 16f;
    private SpriteRenderer spriteRenderer;
    private float elapsed;

    public int CurrentFrame { get; private set; }

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    private void OnEnable()
    {
        elapsed = 0f;
        SetFrame(0);
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0f)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        elapsed += Time.deltaTime;
        SetFrame(Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length);
    }

    private void SetFrame(int index)
    {
        CurrentFrame = index;
        if (spriteRenderer != null && frames != null && frames.Length > 0 && frames[index] != null)
            spriteRenderer.sprite = frames[index];
    }
}
