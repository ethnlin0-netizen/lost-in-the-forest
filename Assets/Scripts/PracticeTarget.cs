using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PracticeTarget : MonoBehaviour
{
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Color restingColor;
    private float flashRemaining;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        restingColor = spriteRenderer.color;
    }

    public void ReceiveHit(bool charged, Vector2 direction, float knockbackImpulse)
    {
        spriteRenderer.color = charged ? Color.yellow : Color.white;
        flashRemaining = 0.15f;
        if (charged)
            body.AddForce(direction.normalized * knockbackImpulse, ForceMode2D.Impulse);
    }

    private void Update()
    {
        if (flashRemaining <= 0f) return;
        flashRemaining -= Time.deltaTime;
        if (flashRemaining <= 0f) spriteRenderer.color = restingColor;
    }
}
