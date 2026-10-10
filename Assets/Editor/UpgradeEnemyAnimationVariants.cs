using System;
using UnityEditor;
using UnityEngine;

/// <summary>Keeps level variants using the animated artwork from their gameplay base.</summary>
public static class UpgradeEnemyAnimationVariants
{
    [MenuItem("Tools/Lost In The Forest/Upgrade Enemy Animation Variants")]
    public static void Apply()
    {
        foreach (string name in new[] { "Chomper", "Spora", "Collusus" })
        {
            string path = "Assets/Prefabs/IntegratedEnemies/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator = root.GetComponent<EnemyVisualAnimator>();
                if (animator == null || !animator.enabled)
                    throw new InvalidOperationException("Missing inherited animation: " + path);
                var data = new SerializedObject(animator);
                var artwork = data.FindProperty("artwork").objectReferenceValue as SpriteRenderer;
                if (artwork == null || artwork.sprite == null)
                    throw new InvalidOperationException("Missing inherited artwork: " + path);
                string colliderBefore = EditorJsonUtility.ToJson(root.GetComponent<BoxCollider2D>());
                var duplicate = artwork.transform.parent.Find("CharacterArtwork");
                if (duplicate != null) UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                artwork.enabled = true;
                if (colliderBefore != EditorJsonUtility.ToJson(root.GetComponent<BoxCollider2D>()))
                    throw new InvalidOperationException("Collider changed: " + path);
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Could not save: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Verify();
    }

    public static void Verify()
    {
        foreach (string folder in new[] { "Enemies", "IntegratedEnemies" })
        foreach (string name in new[] { "Chomper", "Spora", "Collusus" })
        {
            string path = "Assets/Prefabs/" + folder + "/" + name + ".prefab";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) throw new InvalidOperationException("Missing prefab: " + path);
            var animator = root.GetComponent<EnemyVisualAnimator>();
            if (animator == null || !animator.enabled)
                throw new InvalidOperationException("Missing animation: " + path);
            var data = new SerializedObject(animator);
            var artwork = data.FindProperty("artwork").objectReferenceValue as SpriteRenderer;
            if (artwork == null || !artwork.enabled || artwork.sprite == null ||
                data.FindProperty("artworkMaterial").objectReferenceValue == null ||
                data.FindProperty("particleMaterial").objectReferenceValue == null ||
                artwork.transform.parent.Find("CharacterArtwork") != null)
                throw new InvalidOperationException("Invalid or duplicate artwork: " + path);
            Debug.Log("PASS: animated prefab with one artwork and valid materials: " + path);
        }
    }
}
