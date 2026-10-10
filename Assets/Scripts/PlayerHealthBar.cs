using UnityEngine;

[DefaultExecutionOrder(30)]
public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RectTransform fill;
    private void OnEnable()
    {
        if (health == null) return;
        health.HealthChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= Refresh;
    }
    private void Refresh()
    {
        if (fill != null && health != null)
            fill.anchorMax = new Vector2(Mathf.Clamp01(health.CurrentHealth / health.MaximumHealth), 1f);
    }
}
