using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
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
        if (fill == null || health == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(health.CurrentHealth / health.MaximumHealth), 1f);
        if (health.IsDead) gameObject.SetActive(false);
    }
}
