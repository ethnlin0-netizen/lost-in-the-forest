// Run after configuring Lenny and the panorama. Preserve main's shared prefab and combat changes.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/PrismbloomLevel01.unity") throw new System.InvalidOperationException("Open PrismbloomLevel01.");
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var animator = player.GetComponent<LennyVisualAnimator>();
if (animator == null) throw new System.InvalidOperationException("Course 1 artwork animator missing.");
var ownArtwork = new UnityEditor.SerializedObject(animator).FindProperty("artwork").objectReferenceValue as UnityEngine.SpriteRenderer;
var inheritedAnimator = player.GetComponent<LennyAnimation>();
if (inheritedAnimator != null) inheritedAnimator.enabled = false;
foreach (var renderer in player.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))
    renderer.enabled = renderer == ownArtwork;
var scenes = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!scenes.Exists(s => s.path == scene.path)) scenes.Add(new UnityEditor.EditorBuildSettingsScene(scene.path, true));
UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new { saved = scene.path, singleArtworkAnimator = true, buildSceneCount = scenes.Count,
    colliderSize = player.GetComponent<UnityEngine.CapsuleCollider2D>().size };
