using System;
using UnityEngine;

/// <summary>
/// Keeps each biome image stationary on screen, with no scrolling or parallax.
/// Changes the image only while blending across a biome boundary in world space.
/// Platforms and other gameplay objects remain in world space.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public sealed class BiomeBackgroundController : MonoBehaviour
{
    public enum EditorPreviewMode
    {
        CoursePosition,
        Biome1,
        Biome2,
        Biome3,
        Biome4,
        Biome5,
        Biome6,
        CourseOverview
    }

    [Serializable]
    public sealed class Biome
    {
        public string name;
        [Tooltip("World X at the center of the transition into this biome. Ignored for the first biome. Keep entries in course order.")]
        public float startX;
        public SpriteRenderer background;
    }

    [SerializeField, Tooltip("Backgrounds stay fixed in this camera's view as it moves through the course.")]
    private Camera targetCamera;
    [SerializeField] private Biome[] biomes = Array.Empty<Biome>();
    [SerializeField, Min(0f), Tooltip("World units over which neighboring biome backgrounds blend.")]
    private float transitionWidth = 10f;
    [SerializeField, Min(0.01f), Tooltip("Distance in front of the camera, behind gameplay.")]
    private float distanceFromCamera = 20f;
    [SerializeField, Tooltip("Backgrounds render below ordinary gameplay sprites.")]
    private int sortingOrder = -1000;
    [SerializeField, Tooltip("Course Overview displays the biome artwork side by side. Choose an individual biome for a camera preview. Play Mode always uses the camera's course position.")]
    private EditorPreviewMode editorPreview = EditorPreviewMode.CoursePosition;

    private void OnEnable() => RefreshBackgrounds();
    private void LateUpdate() => RefreshBackgrounds();

    private void OnValidate()
    {
        transitionWidth = Mathf.Max(0f, transitionWidth);
        distanceFromCamera = Mathf.Max(0.01f, distanceFromCamera);
    }

    /// <summary>Updates both the scene preview and the running game.</summary>
    public void RefreshBackgrounds()
    {
        if (targetCamera == null || !targetCamera.orthographic || biomes == null || biomes.Length == 0)
            return;

#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject) && editorPreview == EditorPreviewMode.CourseOverview)
        {
            RefreshCourseOverview();
            return;
        }
#endif
        GetBlend(targetCamera.transform.position.x, out int from, out int to, out float blend);
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject) && editorPreview != EditorPreviewMode.CoursePosition)
        {
            int previewIndex = (int)editorPreview - 1;
            if (previewIndex < biomes.Length && biomes[previewIndex]?.background != null
                && biomes[previewIndex].background.sprite != null)
            {
                from = to = previewIndex;
                blend = 0f;
            }
        }
#endif
        // The outgoing image stays opaque underneath the incoming image. Fading both
        // alphas at once would expose the camera clear color and darken the midpoint.
        for (int i = 0; i < biomes.Length; i++)
        {
            var renderer = biomes[i]?.background;
            if (renderer == null || renderer.sprite == null) continue;

            bool visible = i == from || (i == to && blend > 0f);
            renderer.enabled = visible;
            var color = renderer.color;
            color.a = i == from ? 1f : i == to ? blend : 0f;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder + i;
            if (visible) FitToCamera(renderer);
        }
    }

#if UNITY_EDITOR
    private void RefreshCourseOverview()
    {
        // This is an artwork overview, not a measurement of each playable biome's length.
        // Runtime still presents a single fixed image and blends at configured Start X values.
        float nextLeft = 0f;
        bool firstVisible = true;
        foreach (var biome in biomes)
        {
            var renderer = biome?.background;
            if (renderer == null || renderer.sprite == null) continue;
            renderer.enabled = true;
            var color = renderer.color;
            color.a = 1f;
            renderer.color = color;
            FitToCamera(renderer);

            var bounds = renderer.bounds;
            float centerX = firstVisible ? transform.position.x : nextLeft + bounds.extents.x;
            renderer.transform.position += new Vector3(centerX - bounds.center.x,
                transform.position.y - bounds.center.y, 0f);
            nextLeft = renderer.bounds.max.x;
            firstVisible = false;
        }
    }
#endif

    private void GetBlend(float worldX, out int from, out int to, out float blend)
    {
        from = to = 0;
        blend = 0f;
        for (int i = 1; i < biomes.Length; i++)
        {
            if (biomes[i] == null || biomes[i].background == null || biomes[i].background.sprite == null)
                continue;

            float width = transitionWidth;
            // Prevent adjacent transition ranges from overlapping when biomes are short.
            if (i > 1 && biomes[i - 1] != null)
                width = Mathf.Min(width, Mathf.Max(0f, biomes[i].startX - biomes[i - 1].startX));
            if (i + 1 < biomes.Length && biomes[i + 1] != null)
                width = Mathf.Min(width, Mathf.Max(0f, biomes[i + 1].startX - biomes[i].startX));

            float left = biomes[i].startX - width * 0.5f;
            float right = biomes[i].startX + width * 0.5f;
            if (worldX < left) return;
            if (width > 0f && worldX < right)
            {
                to = i;
                blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(left, right, worldX));
                return;
            }
            from = to = i;
        }
    }

    private void FitToCamera(SpriteRenderer renderer)
    {
        var spriteBounds = renderer.sprite.bounds;
        if (spriteBounds.size.x <= 0f || spriteBounds.size.y <= 0f) return;

        float height = targetCamera.orthographicSize * 2f;
        float scale = Mathf.Max(height / spriteBounds.size.y,
            height * targetCamera.aspect / spriteBounds.size.x) * 1.001f;
        var backgroundTransform = renderer.transform;
        var parentScale = backgroundTransform.parent == null ? Vector3.one : backgroundTransform.parent.lossyScale;
        backgroundTransform.localScale = new Vector3(scale / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            scale / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f), 1f);
        backgroundTransform.rotation = targetCamera.transform.rotation;
        float distance = Mathf.Clamp(distanceFromCamera, targetCamera.nearClipPlane + 0.01f,
            Mathf.Max(targetCamera.nearClipPlane + 0.01f, targetCamera.farClipPlane - 0.01f));
        backgroundTransform.position = targetCamera.transform.position + targetCamera.transform.forward * distance
            - backgroundTransform.TransformVector(spriteBounds.center);
    }

    private void OnDrawGizmosSelected()
    {
        if (biomes == null) return;
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject) && editorPreview == EditorPreviewMode.CourseOverview)
        {
            foreach (var biome in biomes)
            {
                if (biome?.background == null || !biome.background.enabled) continue;
                var bounds = biome.background.bounds;
                UnityEditor.Handles.Label(new Vector3(bounds.min.x, bounds.max.y + 0.4f, bounds.center.z), biome.name);
            }
            return;
        }
#endif
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.8f);
        float centerY = targetCamera == null ? 0f : targetCamera.transform.position.y;
        for (int i = 1; i < biomes.Length; i++)
        {
            if (biomes[i] == null) continue;
            float x = biomes[i].startX;
            Gizmos.DrawLine(new Vector3(x, centerY - 6f, 0f), new Vector3(x, centerY + 6f, 0f));
        }
    }
}
