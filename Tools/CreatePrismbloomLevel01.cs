// Unity Editor eval statement block. Creates a separate combined scene once.
const string original = "Assets/Scenes/Level01.unity";
const string destination = "Assets/Scenes/PrismbloomLevel01.unity";
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play Mode before creating the biome.");
if (System.IO.File.Exists(destination))
    throw new System.InvalidOperationException("The separate biome already exists; do not overwrite it.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
        throw new System.InvalidOperationException("Save open scene changes first.");
var source = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(source, destination, true))
    throw new System.IO.IOException("Could not create a separate scene copy.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(destination);
var camera = UnityEngine.Object.FindAnyObjectByType<Level01Camera>();
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
if (camera == null || player == null) throw new System.InvalidOperationException("Missing camera or player.");
var preserved = new System.Collections.Generic.Dictionary<UnityEngine.Object, string>();
foreach (var root in scene.GetRootGameObjects())
foreach (var component in root.GetComponentsInChildren<UnityEngine.Component>(true))
{
    if (component == null) throw new System.InvalidOperationException("Missing script in Level01.");
    if (component != camera) preserved.Add(component, UnityEditor.EditorJsonUtility.ToJson(component));
}
// Repair the inherited broken follow reference only in this new scene.
camera.target = player.transform;
var renderCamera = camera.GetComponent<UnityEngine.Camera>();
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(
    "Assets/Art/Sprites/PrismbloodForest-Background.png");
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(
    UnityEditor.AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
if (sprite == null || material == null) throw new System.InvalidOperationException("Missing original background assets.");
var backgroundRoot = new UnityEngine.GameObject("Prismbloom Background");
var art = new UnityEngine.GameObject("Prismbloom Forest Artwork");
art.transform.SetParent(backgroundRoot.transform, false);
var renderer = art.AddComponent<UnityEngine.SpriteRenderer>();
renderer.sprite = sprite;
renderer.sharedMaterial = material;
renderer.sortingOrder = -1000;
var controller = backgroundRoot.AddComponent<BiomeBackgroundController>();
var settings = new UnityEditor.SerializedObject(controller);
settings.FindProperty("targetCamera").objectReferenceValue = renderCamera;
settings.FindProperty("biomes").arraySize = 1;
var biome = settings.FindProperty("biomes").GetArrayElementAtIndex(0);
biome.FindPropertyRelative("name").stringValue = "Prismbloom Forest";
biome.FindPropertyRelative("startX").floatValue = 0f;
biome.FindPropertyRelative("background").objectReferenceValue = renderer;
settings.FindProperty("transitionWidth").floatValue = 0f;
settings.FindProperty("editorPreview").enumValueIndex = 0;
settings.ApplyModifiedPropertiesWithoutUndo();
controller.RefreshBackgrounds();
foreach (var entry in preserved)
    if (entry.Value != UnityEditor.EditorJsonUtility.ToJson(entry.Key))
        throw new System.InvalidOperationException("An inherited gameplay component changed: " + entry.Key.name);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))
    throw new System.IO.IOException("Could not save the separate biome.");
var builds = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!builds.Exists(x => x.path == destination))
    builds.Add(new UnityEditor.EditorBuildSettingsScene(destination, true));
UnityEditor.EditorBuildSettings.scenes = builds.ToArray();
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = destination, preservedComponents = preserved.Count,
    enemies = UnityEngine.Object.FindObjectsByType<BlightEnemy>().Length,
    background = UnityEditor.AssetDatabase.GetAssetPath(renderer.sprite), cameraFollowsPlayer = camera.target == player.transform };
