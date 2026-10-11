using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Styles only Level04. Original assets and gameplay components stay intact.</summary>
public static class StyleLevel04Visuals
{
    public const string ScenePath = "Assets/Scenes/GameLoop/Level04.unity";
    private const string MaterialFolder = "Assets/Materials/VeinrootHollow";
    private const string VisualPrefix = "VeinrootVisual_";

    [MenuItem("Tools/Lost In The Forest/Style Level 04 Visuals")]
    public static void ApplyFromMenu() { Apply(); }

    public static object Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your open scene changes before styling Level04.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var root = GameObject.Find("Level04Route");
        var geometry = root != null ? root.transform.Find("LevelGeometry") : null;
        var camera = Camera.main;
        var follow = camera != null ? camera.GetComponent<Level01Camera>() : null;
        var background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/VeinrootHollow-Background.png");
        var stone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PrismbloomTerrain/PrismbloomStone.png");
        var foliage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PrismbloomTerrain/PrismbloomMoss.png");
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Sprites/PrismbloomTerrain/PrismbloomTerrain.mat");
        if (geometry == null || follow == null || background == null || stone == null || foliage == null || unlit == null)
            throw new InvalidOperationException("Level04 or its existing visual reference assets are missing.");
        var shader = Shader.Find("LostInTheForest/VeinrootTerrain");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Veinroot terrain shader did not compile.");

        Directory.CreateDirectory("Temp/Level04VisualWork");
        if (!File.Exists("Temp/Level04VisualWork/Level04.before.unity"))
            File.Copy(ScenePath, "Temp/Level04VisualWork/Level04.before.unity");
        var components = new Dictionary<Component, string>();
        var transforms = new Dictionary<Transform, (Vector3, Quaternion, Vector3, Transform)>();
        foreach (var entity in scene.GetRootGameObjects())
            foreach (var component in entity.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new InvalidOperationException("An existing component is missing its script.");
                if (component is Transform t && !t.name.StartsWith(VisualPrefix))
                    transforms[t] = (t.localPosition, t.localRotation, t.localScale, t.parent);
                if (component is MonoBehaviour || component is Collider2D || component is Rigidbody2D || component is Camera)
                    components[component] = EditorJsonUtility.ToJson(component);
            }
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "VeinrootHollow");
        var stoneMaterial = Material("VeinrootStone", shader, false, false);
        var foliageMaterial = Material("VeinrootFoliage", shader, true, false);
        var distantMaterial = Material("VeinrootDistantStone", shader, false, true);
        Physics2D.SyncTransforms();
        int platformCount = 0;
        foreach (var box in geometry.GetComponentsInChildren<BoxCollider2D>())
        {
            var old = box.GetComponent<SpriteRenderer>();
            if (old == null || box.isTrigger) continue;
            RemoveVisualChildren(box.transform);
            var bounds = box.bounds;
            old.enabled = false;
            Surface(VisualPrefix + "Stone", box.transform, bounds.center, bounds.size, stone, stoneMaterial, old.sortingLayerID, old.sortingOrder);
            if (!box.name.Contains("Roof") && !box.name.Contains("Wall") && !box.name.Contains("Hanging") && bounds.size.x > 1.2f)
            {
                float height = Mathf.Min(.28f, bounds.size.y);
                Surface(VisualPrefix + "Foliage", box.transform, new Vector3(bounds.center.x, bounds.max.y - height * .5f, bounds.center.z - .01f),
                    new Vector2(bounds.size.x, height), foliage, foliageMaterial, old.sortingLayerID, old.sortingOrder + 1);
            }
            ++platformCount;
        }

        var decoration = root.transform.Find("DecayingRuins_NoColliders");
        if (decoration != null)
            foreach (var renderer in decoration.GetComponentsInChildren<SpriteRenderer>().Where(r => !r.name.StartsWith(VisualPrefix)).ToArray())
            {
                if (renderer.name.Contains("DecayedEdge")) { renderer.enabled = false; continue; }
                if (renderer.name.Contains("Column"))
                {
                    RemoveVisualChildren(renderer.transform);
                    var bounds = renderer.bounds;
                    renderer.enabled = false;
                    var distant = Surface(VisualPrefix + "DistantStone", renderer.transform, bounds.center, bounds.size, stone, distantMaterial, renderer.sortingLayerID, renderer.sortingOrder);
                    distant.color = new Color(1, 1, 1, .32f);
                }
                else if (renderer.name.Contains("Vine")) renderer.color = new Color(.16f, .10f, .065f, .55f);
            }
        ApplyAcidMaterials(root);
        var backgroundRoot = GameObject.Find("Veinroot Hollow Background");
        if (backgroundRoot == null) backgroundRoot = new GameObject("Veinroot Hollow Background");
        backgroundRoot.transform.position = Vector3.zero;
        backgroundRoot.transform.rotation = Quaternion.identity;
        backgroundRoot.transform.localScale = Vector3.one;
        RemoveVisualChildren(backgroundRoot.transform);
        float maxAspect = Mathf.Max(2.4f, camera.aspect);
        float left = Mathf.Min(follow.minimum.x, camera.transform.position.x) - camera.orthographicSize * maxAspect - 2;
        float right = Mathf.Max(follow.maximum.x, camera.transform.position.x) + camera.orthographicSize * maxAspect + 2;
        float bottom = Mathf.Min(follow.minimum.y, camera.transform.position.y) - camera.orthographicSize - 2;
        float top = Mathf.Max(follow.maximum.y, camera.transform.position.y) + camera.orthographicSize + 2;
        float scale = Mathf.Max((right - left) / background.bounds.size.x, (top - bottom) / background.bounds.size.y);
        var image = new GameObject(VisualPrefix + "Background");
        image.transform.SetParent(backgroundRoot.transform, false);
        image.transform.position = new Vector3((left + right) * .5f, (bottom + top) * .5f, 10);
        image.transform.localScale = new Vector3(scale, scale, 1);
        var backgroundRenderer = image.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = background;
        backgroundRenderer.sharedMaterial = unlit;
        backgroundRenderer.color = new Color(.72f, .68f, .64f, 1);
        backgroundRenderer.sortingOrder = -1000;
        int coverageChecks = CheckCoverage(camera, follow, backgroundRenderer);
        foreach (var entry in transforms)
        {
            var t = entry.Key; var old = entry.Value;
            if (t == null || t.localPosition != old.Item1 || t.localRotation != old.Item2 || t.localScale != old.Item3 || t.parent != old.Item4)
                throw new InvalidOperationException("Existing transform changed: " + (t != null ? t.name : "deleted object"));
        }
        foreach (var entry in components)
            if (entry.Key == null || EditorJsonUtility.ToJson(entry.Key) != entry.Value)
                throw new InvalidOperationException("Gameplay component changed: " + entry.Key?.name);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Level04 could not be saved.");
        AssetDatabase.SaveAssets();
        return new { scene = ScenePath, texturedPlatforms = platformCount, coverageChecks, preservedGameplayComponents = components.Count,
            preservedTransforms = transforms.Count, backgroundWorldSize = backgroundRenderer.bounds.size, originalArtworkUnchanged = true };
    }

    public static int CheckCoverage(Camera camera, Level01Camera follow, SpriteRenderer background)
    {
        int checks = 0;
        foreach (float aspect in new[] { 4f / 3, 16f / 9, 2.4f, camera.aspect })
            foreach (float x in new[] { follow.minimum.x, (follow.minimum.x + follow.maximum.x) * .5f, follow.maximum.x, camera.transform.position.x })
                foreach (float y in new[] { follow.minimum.y, (follow.minimum.y + follow.maximum.y) * .5f, follow.maximum.y, camera.transform.position.y })
                {
                    var center = new Vector3(x, y, background.bounds.center.z);
                    var half = new Vector3(camera.orthographicSize * aspect, camera.orthographicSize, 0);
                    if (!background.bounds.Contains(center - half) || !background.bounds.Contains(center + half))
                        throw new InvalidOperationException("Background leaves a camera gap at " + center + " aspect " + aspect);
                    ++checks;
                }
        return checks;
    }

    public static void ApplyAcidMaterials(GameObject root)
    {
        var shader = Shader.Find("LostInTheForest/VeinrootAcid");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Acid shader did not compile.");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "VeinrootHollow");
        foreach (bool surface in new[] { false, true })
        {
            string path = MaterialFolder + (surface ? "/VeinrootAcidSurface.mat" : "/VeinrootAcidPool.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetFloat("_SurfaceMode", surface ? 1 : 0);
            material.SetFloat("_FlowSpeed", .35f);
            EditorUtility.SetDirty(material);
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.name.EndsWith(surface ? "_BrightSurface" : "_Water"))
                {
                    renderer.sharedMaterial = material;
                    renderer.color = Color.white;
                }
        }
    }

    private static Material Material(string name, Shader shader, bool foliage, bool distant)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetColor("_ShadowColor", distant ? new Color(.045f, .036f, .028f) : new Color(.12f, .09f, .065f));
        material.SetColor("_LightColor", distant ? new Color(.18f, .13f, .085f) : new Color(.49f, .36f, .24f));
        material.SetColor("_AccentColor", new Color(.49f, .22f, .17f));
        material.SetColor("_HighlightColor", new Color(.83f, .58f, .28f));
        material.SetFloat("_SurfaceMode", foliage ? 1 : 0);
        material.SetFloat("_WorldMode", foliage ? 2 : 1);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void RemoveVisualChildren(Transform parent)
    {
        foreach (var child in parent.Cast<Transform>().Where(t => t.name.StartsWith(VisualPrefix)).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static SpriteRenderer Surface(string name, Transform parent, Vector3 center, Vector2 size, Sprite sprite, Material material, int layer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var scale = parent.lossyScale;
        go.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;
        return renderer;
    }
}
