// Run through Unity eval_file in Edit mode. Random selection happens only here,
// never at runtime. Existing placements abort the operation rather than rerolling.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.Exception("Stop Play mode before placing companions.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
        throw new System.Exception("Save your open scene edits first.");
var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var levels = new[] { "PrismbloomLevel01Story", "GloamthorneLevel02", "Ashpetal Level 03",
    "Veinroot Level04", "Wraithglass Barrens Level 05" };
var names = new[] { "Whisperseed", "Dryad", "Petalborn" };
var output = new System.Collections.Generic.List<string>();
var random = new System.Random();
try {
    foreach (string name in levels) {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameLoop/" + name + ".unity");
        if (scene.GetRootGameObjects().Any(root => root.GetComponentsInChildren<BloomPetFollower>(true).Length > 0))
            throw new System.Exception("Saved companions already exist. Move their prefab instances in Unity instead of rerolling.");
    }
    // Store stable identities and asset references on the existing reusable pets.
    string catalogPath = "Assets/Prefabs/BloomPets/BloomCompanions.asset";
    var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<BloomCompanionCatalog>(catalogPath);
    if (catalog == null) {
        catalog = UnityEngine.ScriptableObject.CreateInstance<BloomCompanionCatalog>();
        UnityEditor.AssetDatabase.CreateAsset(catalog, catalogPath);
    }
    var catalogData = new UnityEditor.SerializedObject(catalog);
    var catalogFields = new[] { "whisperseed", "dryad", "petalborn" };
    for (int i = 0; i < names.Length; i++)
        catalogData.FindProperty(catalogFields[i]).objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/Prefabs/BloomPets/" + names[i] + "Pet.prefab").GetComponent<BloomPetFollower>();
    catalogData.ApplyModifiedPropertiesWithoutUndo();
    for (int i = 0; i < names.Length; i++) {
        string path = "Assets/Prefabs/BloomPets/" + names[i] + "Pet.prefab";
        var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
        try {
            var serialized = new UnityEditor.SerializedObject(root.GetComponent<BloomPetFollower>());
            serialized.FindProperty("companionId").enumValueIndex = i + 1;
            serialized.FindProperty("companionCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    }
    for (int i = levels.Length - 1; i > 0; i--) {
        int j = random.Next(i + 1); string swap = levels[i]; levels[i] = levels[j]; levels[j] = swap;
    }
    for (int i = 0; i < names.Length; i++) {
        string path = "Assets/Scenes/GameLoop/" + levels[i] + ".unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Movement>()).Single();
        UnityEngine.Physics2D.SyncTransforms();
        var bounds = player.GetComponent<UnityEngine.Collider2D>().bounds;
        var playerFeet = new UnityEngine.Vector2(bounds.center.x, bounds.min.y);
        var hit = UnityEngine.Physics2D.RaycastAll(playerFeet + UnityEngine.Vector2.up * .08f, UnityEngine.Vector2.down, 5f)
            .Where(h => h.collider is UnityEngine.BoxCollider2D && !h.collider.isTrigger && h.normal.y > .55f &&
                (h.rigidbody == null || h.rigidbody.bodyType == UnityEngine.RigidbodyType2D.Static))
            .OrderBy(h => h.distance).FirstOrDefault();
        if (hit.collider == null) throw new System.Exception("No safe entrance platform in " + path);
        var support = hit.collider.bounds;
        UnityEngine.Vector2 spawn = default; bool found = false;
        float preferred = 5f + (float)random.NextDouble() * 3f;
        for (int attempt = 0; attempt < 40; attempt++) {
            float distance = preferred + (attempt / 2) * .5f;
            float x = playerFeet.x + (attempt % 2 == 0 ? 1 : -1) * distance;
            if (x < support.min.x + 1f || x > support.max.x - 1f) continue;
            var candidate = new UnityEngine.Vector2(x, support.max.y + .04f);
            bool unsafeRoute = false;
            // Check the whole walk from the entrance, including lethal trigger regions.
            float middleX = (x + playerFeet.x) * .5f;
            foreach (var collider in UnityEngine.Physics2D.OverlapBoxAll(
                new UnityEngine.Vector2(middleX, candidate.y + .75f),
                new UnityEngine.Vector2(UnityEngine.Mathf.Abs(x - playerFeet.x) + .85f, 1.4f), 0f)) {
                if (collider.GetComponentInParent<Level02ThornHazard>() != null ||
                    collider.GetComponentInParent<BlightBossHazard>() != null ||
                    collider.GetComponentInParent<BlightEnemy>() != null ||
                    !collider.isTrigger && collider != hit.collider &&
                    (collider.attachedRigidbody == null || collider.attachedRigidbody.bodyType == UnityEngine.RigidbodyType2D.Static)) {
                    unsafeRoute = true; break;
                }
            }
            if (!unsafeRoute) { spawn = candidate; found = true; break; }
        }
        if (!found) throw new System.Exception("No clear reachable placement found in " + path);
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/BloomPets/" + names[i] + "Pet.prefab");
        var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = names[i] + " Companion";
        instance.transform.position = new UnityEngine.Vector3(spawn.x, spawn.y, 0);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("Save failed: " + path);
        output.Add(names[i] + " | " + path + " | feet=" + spawn.ToString("F3"));
    }
    UnityEditor.AssetDatabase.SaveAssets();
} finally { UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup); }
System.IO.File.WriteAllLines("Temp/BloomCompanionPlacements.txt", output);
return string.Join("\n", output);
