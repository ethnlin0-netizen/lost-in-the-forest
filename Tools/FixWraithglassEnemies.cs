// Unity Pipeline eval: reconnect the placed enemies without replacing or moving them.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity") throw new System.InvalidOperationException("Open WraithglassLevel05 first.");
var preserved = new System.Collections.Generic.Dictionary<UnityEngine.Component, string>();
foreach (var root in scene.GetRootGameObjects()) foreach (var component in root.GetComponentsInChildren<UnityEngine.Component>(true))
    if (component != null && !(component is BlightEnemy) && !(component is EnemyVisualAnimator) && !(component is EnemyHealth) && !(component is EnemyHealthBar) && !(component is EnemyNavigator2D) && !(component is PlatformNavigation2D))
        preserved[component] = UnityEditor.EditorJsonUtility.ToJson(component);
UnityEngine.Transform Child(UnityEngine.Transform root, string name) {
    foreach (var child in root.GetComponentsInChildren<UnityEngine.Transform>(true)) if (child.name == name) return child;
    throw new System.InvalidOperationException("Missing " + name + " under " + root.name);
}
void Bind(UnityEngine.Component component, string property, UnityEngine.Object value) {
    UnityEditor.Undo.RecordObject(component, "Reconnect Wraithglass enemy");
    var data = new UnityEditor.SerializedObject(component);
    data.FindProperty(property).objectReferenceValue = value;
    data.ApplyModifiedProperties();
}
var geometry = UnityEngine.GameObject.Find("Level05Route/LevelGeometry");
if (geometry == null) throw new System.InvalidOperationException("Level05 geometry missing.");
var navigationWorld = UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>();
if (navigationWorld == null) navigationWorld = UnityEditor.Undo.AddComponent<PlatformNavigation2D>(geometry.transform.parent.gameObject);
Bind(navigationWorld, "surfaceRoot", geometry.transform);
navigationWorld.RebuildSurfaces();
int repaired = 0;
foreach (var health in UnityEngine.Object.FindObjectsByType<EnemyHealth>()) {
    if (health.gameObject.scene != scene) continue;
    var ai = health.GetComponent<BlightEnemy>();
    var animator = health.GetComponent<EnemyVisualAnimator>();
    var visuals = Child(health.transform, "Visuals");
    var cue = Child(health.transform, "AttackCue");
    var artwork = Child(health.transform, "EnemyArtwork").GetComponent<UnityEngine.SpriteRenderer>();
    if (ai == null || animator == null || artwork == null || artwork.sprite == null) throw new System.InvalidOperationException("Incomplete enemy: " + health.name);
    Bind(ai, "visuals", visuals);
    Bind(ai, "attackVisual", cue.gameObject);
    Bind(health, "visuals", visuals);
    Bind(animator, "artwork", artwork);
    foreach (var bar in health.GetComponentsInChildren<EnemyHealthBar>(true)) {
        Bind(bar, "health", health);
        Bind(bar, "fill", Child(bar.transform, "Fill").GetComponent<UnityEngine.RectTransform>());
    }
    ai.enabled = true;
    animator.enabled = true;
    var aiData = new UnityEditor.SerializedObject(ai);
    if (ai.Kind == BlightEnemy.BlightType.Chomper) aiData.FindProperty("patrolRadius").floatValue = .55f;
    if (ai.Kind == BlightEnemy.BlightType.Collusus) aiData.FindProperty("patrolRadius").floatValue = 1.1f;
    aiData.ApplyModifiedProperties();
    var nav = health.GetComponent<EnemyNavigator2D>();
    if (nav == null) throw new System.InvalidOperationException("Missing navigator: " + health.name);
    nav.enabled = true;
    if (ai.Kind == BlightEnemy.BlightType.Collusus) {
        var navData = new UnityEditor.SerializedObject(nav);
        navData.FindProperty("maximumDropDistance").floatValue = 4f;
        navData.FindProperty("jumpSpeed").floatValue = 22f;
        navData.ApplyModifiedProperties();
    }
    ++repaired;
}
foreach (var pair in preserved) if (pair.Value != UnityEditor.EditorJsonUtility.ToJson(pair.Key)) throw new System.InvalidOperationException("Placement or gameplay changed: " + pair.Key.name);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new { saved = scene.path, enemyBindingsRepaired = repaired, navigationSurfaces = navigationWorld.Surfaces.Count, placementsAndCourseUnchanged = true };
