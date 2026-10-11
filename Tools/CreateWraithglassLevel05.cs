// Unity Pipeline eval statement block: duplicate Level05, preserve course and encounters.
const string sourcePath = "Assets/Scenes/Level05.unity";
const string destination = "Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity";
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
if (System.IO.File.Exists(destination)) throw new System.InvalidOperationException("Do not overwrite the existing separate biome scene.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Save open scene changes first.");
var source = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sourcePath);
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(source, destination, true)) throw new System.IO.IOException("Scene copy failed.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(destination);
var snapshots = new System.Collections.Generic.Dictionary<UnityEngine.Component, string>();
foreach (var root in scene.GetRootGameObjects())
    foreach (var component in root.GetComponentsInChildren<UnityEngine.Component>(true))
    {
        if (component == null) throw new System.InvalidOperationException("Level05 contains a missing script.");
        snapshots[component] = UnityEditor.EditorJsonUtility.ToJson(component);
    }
var backgroundRoot = new UnityEngine.GameObject("Wraithglass Background");
var placeholder = new UnityEngine.GameObject("Wraithglass Artwork");
placeholder.transform.SetParent(backgroundRoot.transform, false);
var renderer = placeholder.AddComponent<UnityEngine.SpriteRenderer>();
renderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/WraithglassBarrens-Background.png");
renderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(UnityEditor.AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
renderer.sortingOrder = -1000;
if (renderer.sprite == null || renderer.sharedMaterial == null) throw new System.InvalidOperationException("Background source missing.");
foreach (var pair in snapshots)
    if (pair.Value != UnityEditor.EditorJsonUtility.ToJson(pair.Key)) throw new System.InvalidOperationException("Inherited course changed.");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var builds = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!builds.Exists(s => s.path == destination)) builds.Add(new UnityEditor.EditorBuildSettingsScene(destination, true));
UnityEditor.EditorBuildSettings.scenes = builds.ToArray();
var follow = UnityEngine.Object.FindAnyObjectByType<Level01Camera>();
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var enemies = new System.Collections.Generic.List<string>();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<BlightEnemy>()) enemies.Add(enemy.name);
return new { scene = destination, inheritedComponentsUnchanged = snapshots.Count, enemyNames = string.Join(", ", enemies),
    cameraTracksPlayer = follow.target == player.transform, playerPosition = player.transform.position };
