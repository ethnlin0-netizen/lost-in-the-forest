// Unity Editor eval statement block. Updates only the separate combined scene.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play Mode before configuring Lenny.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity")
    throw new System.InvalidOperationException("Open the separate Wraithglass level first.");
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var shape = player.GetComponent<UnityEngine.CapsuleCollider2D>();
string colliderBefore = UnityEditor.EditorJsonUtility.ToJson(shape);
string movementBefore = UnityEditor.EditorJsonUtility.ToJson(player);
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/LennyVisual.prefab");
var visual = player.transform.Find("LennyVisual");
if (visual == null)
{
    var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, scene);
    instance.transform.SetParent(player.transform, false);
    visual = instance.transform;
}
var artwork = visual.GetComponent<UnityEngine.SpriteRenderer>();
var texture = new UnityEngine.Texture2D(2, 2);
UnityEngine.Rect crop;
try
{
    UnityEngine.ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes("Assets/Art/Sprites/Lenny.png"));
    var pixels = texture.GetPixels32();
    int left = texture.width, bottom = texture.height, right = 0, top = 0;
    for (int y = 0; y < texture.height; y++)
    for (int x = 0; x < texture.width; x++)
        if (pixels[y * texture.width + x].a > 12)
        { left = System.Math.Min(left, x); right = System.Math.Max(right, x); bottom = System.Math.Min(bottom, y); top = System.Math.Max(top, y); }
    crop = new UnityEngine.Rect(left, bottom, right - left + 1, top - bottom + 1);
}
finally { UnityEngine.Object.DestroyImmediate(texture); }
float scale = shape.size.y * 1.15f / (crop.height / artwork.sprite.pixelsPerUnit);
var spriteMin = artwork.sprite.bounds.min;
float visibleCenterX = spriteMin.x + crop.center.x / artwork.sprite.pixelsPerUnit;
float visibleBottom = spriteMin.y + crop.y / artwork.sprite.pixelsPerUnit;
visual.localScale = UnityEngine.Vector3.one * scale;
visual.localPosition = new UnityEngine.Vector3(shape.offset.x - visibleCenterX * scale,
    shape.offset.y - shape.size.y * .5f - visibleBottom * scale, 0);
artwork.sortingOrder = 3;
foreach (var renderer in player.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
    if (renderer != artwork) renderer.enabled = false;
var healthData = new UnityEditor.SerializedObject(player.GetComponent<PlayerHealth>());
healthData.FindProperty("bodyVisual").objectReferenceValue = artwork;
healthData.ApplyModifiedPropertiesWithoutUndo();
const string materialPath = "Assets/Materials/EnemyAnimations/LennyArtwork.mat";
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if (material == null)
{
    material = new UnityEngine.Material(UnityEngine.Shader.Find("LostInTheWoods/EnemyAnimatedArtwork"));
    UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
}
material.mainTexture = artwork.sprite.texture;
material.SetColor("_Tint", UnityEngine.Color.white);
material.SetFloat("_Blink", 0); material.SetFloat("_ShapeMode", 0);
UnityEditor.EditorUtility.SetDirty(material);
var animator = player.GetComponent<LennyVisualAnimator>();
if (animator == null) animator = player.gameObject.AddComponent<LennyVisualAnimator>();
var data = new UnityEditor.SerializedObject(animator);
data.FindProperty("artwork").objectReferenceValue = artwork;
data.FindProperty("artworkMaterial").objectReferenceValue = material;
data.FindProperty("visiblePixels").rectValue = crop;
data.ApplyModifiedPropertiesWithoutUndo();
var inheritedAnimator = player.GetComponent<LennyAnimation>();
if (inheritedAnimator == null) throw new System.InvalidOperationException("Lenny's separate tail rig is missing.");
var bodySprite = player.GetComponent<UnityEngine.SpriteRenderer>().sprite;
data.FindProperty("separatedBodyTexture").objectReferenceValue = bodySprite.texture;
data.ApplyModifiedPropertiesWithoutUndo();
var parts = new UnityEditor.SerializedObject(inheritedAnimator);
UnityEngine.Transform FindPart(string name)
{
    foreach (var child in player.GetComponentsInChildren<UnityEngine.Transform>(true))
        if (child.name == name) return child;
    throw new System.InvalidOperationException("Missing Lenny part: " + name);
}
var facing = FindPart("Lenny Artwork");
float pixelsToWorld = scale / artwork.sprite.pixelsPerUnit;
facing.localScale = UnityEngine.Vector3.one * (pixelsToWorld * bodySprite.pixelsPerUnit);
facing.localPosition = new UnityEngine.Vector3(
    shape.offset.x + (bodySprite.pivot.x - crop.center.x) * pixelsToWorld,
    shape.offset.y - shape.size.y * .5f + (bodySprite.pivot.y - crop.y) * pixelsToWorld, 0);
facing.gameObject.SetActive(true);
parts.FindProperty("facingRoot").objectReferenceValue = facing;
parts.FindProperty("leftWing").objectReferenceValue = FindPart("Left Wing");
parts.FindProperty("rightWing").objectReferenceValue = FindPart("Right Wing");
parts.FindProperty("tail").objectReferenceValue = FindPart("Lenny Tail");
parts.ApplyModifiedPropertiesWithoutUndo();
FindPart("Double Jump Wings").gameObject.SetActive(true);
foreach (var name in new[] { "Left Wing", "Right Wing", "Lenny Tail" })
    FindPart(name).GetComponent<UnityEngine.SpriteRenderer>().enabled = true;
inheritedAnimator.enabled = true;
if (colliderBefore != UnityEditor.EditorJsonUtility.ToJson(shape) || movementBefore != UnityEditor.EditorJsonUtility.ToJson(player))
    throw new System.InvalidOperationException("Player movement or collider changed.");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scene.path, lennyVisiblePixels = crop, colliderUnchanged = true, singleArtworkAnimator = true };
