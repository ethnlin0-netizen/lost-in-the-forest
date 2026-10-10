using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Generates normal, editable scene objects, not terrain constructed during play.
[InitializeOnLoad]
public static class BuildLevel01
{
    private const string Request = "Temp/BuildLevel01.request";
    private const string Receipt = "Temp/BuildLevel01.result";
    private static Sprite square;
    private static Material material;
    private static Transform root;

    static BuildLevel01() { EditorApplication.update += CheckRequest; }

    private static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Build(); File.WriteAllText(Receipt, "SUCCESS: Level01 generated and saved."); }
        catch (Exception ex) { File.WriteAllText(Receipt, ex.ToString()); Debug.LogException(ex); }
    }

    [MenuItem("Tools/Lost In The Forest/Build Level 01")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before building the level.");
        const string path = "Assets/Scenes/Level01.unity";
        // Save named open scenes before switching, preserving in-editor edits.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (open.isDirty && string.IsNullOrEmpty(open.path))
                throw new InvalidOperationException("Save your untitled scene before generating Level01.");
            if (open.isDirty && !EditorSceneManager.SaveScene(open))
                throw new IOException("Could not save the open scene.");
        }
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path) scene = EditorSceneManager.OpenScene(path);
        Directory.CreateDirectory("Assets/Scenes/Backups");
        string backup = "Assets/Scenes/Backups/Level01-before-parkour-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true))
            throw new IOException("Could not back up Level01.");

        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        if (square == null) throw new InvalidOperationException("PlaceholderSquare sprite is missing.");
        const string materialPath = "Assets/Materials/Level01Blockout.mat";
        material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "LevelGeometry" || go.name == "Level01Route")
                Undo.DestroyObjectImmediate(go);
        root = new GameObject("Level01Route").transform;
        var terrain = new GameObject("LevelGeometry").transform;
        terrain.SetParent(root);
        root = terrain;

        Color earth = new Color(0.29f, 0.36f, 0.27f);
        Color platform = new Color(0.36f, 0.55f, 0.35f);
        // A compact enclosed forest chamber. The floor spans the whole room;
        // filled pillars and the hill prevent bypassing the platform route.
        Surface("01_ContinuousForestFloor", 20, -1.5f, 56, 5, earth);
        Surface("02_FirstRootPillar", 8, 1, 4, 2.5f, earth);
        Surface("03_LowerBranch", 14, 3.5f, 4, 0.75f, platform);
        Surface("04_SolidTrunkWall", 20, 6, 4, 7.5f, earth);
        Surface("05_SwitchbackBranch", 14, 8.5f, 4, 0.75f, platform);
        Surface("06_CombatRestLedge", 23, 11, 6, 0.75f, earth);
        Surface("07_UpperBranch", 27, 13.5f, 4, 0.75f, platform);
        Surface("08_HillStep", 33, 16, 4, 0.75f, platform);
        Surface("Summit", 39, 16, 10, 17.5f, earth);
        Surface("ForestCeiling", 20, 23, 58, 3, earth);
        Surface("LeftEnclosureWall", -8.5f, 23, 1, 29.5f, earth);
        Surface("RightEnclosureWall", 48.5f, 23, 1, 29.5f, earth);
        Surface("EntranceOverhang", -4, 20, 8, 13, earth);
        Surface("HangingRootWall", 4, 20, 2, 7, earth);

        var encounters = new GameObject("EnemyPlacementMarkers").transform;
        encounters.SetParent(root.parent);
        foreach (var location in new[] { new Vector2(1, -0.5f), new Vector2(23, 12), new Vector2(38, 17) })
        {
            var marker = new GameObject("EnemyPlacement_" + encounters.childCount).transform;
            marker.SetParent(encounters); marker.position = location;
        }

        var checkpoints = new GameObject("Checkpoints").transform;
        checkpoints.SetParent(root.parent);
        var points = new[] { new Vector2(-4, 0), new Vector2(23, 12), new Vector2(39, 17) };
        var spawnPoints = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var point = new GameObject(i == 0 ? "PlayerSpawn" : "Checkpoint_" + i).transform;
            point.SetParent(checkpoints); point.position = points[i]; spawnPoints[i] = point;
        }
        var player = scene.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<Movement>() != null);
        if (player == null)
            player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"), scene);
        player.transform.position = points[0];
        // A missed jump now lands on solid ground rather than triggering recovery.
        var recovery = player.GetComponent<Level01Recovery>();
        if (recovery != null) Undo.DestroyObjectImmediate(recovery);
        var camera = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c != null);
        if (camera == null) throw new InvalidOperationException("Level01 needs a camera.");
        camera.orthographic = true; camera.orthographicSize = 6;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.14f, 0.17f);
        camera.transform.position = new Vector3(2, 4, -10);
        var follow = camera.GetComponent<Level01Camera>();
        if (follow == null) follow = camera.gameObject.AddComponent<Level01Camera>();
        follow.target = player.transform;
        follow.minimum = new Vector2(2, 4);
        follow.maximum = new Vector2(37, 14);

        var goal = new GameObject("Amulet01");
        goal.transform.SetParent(root.parent);
        goal.transform.position = new Vector3(41, 17.5f, 0);
        goal.transform.localScale = new Vector3(0.7f, 0.7f, 1);
        goal.transform.rotation = Quaternion.Euler(0, 0, 45);
        var renderer = goal.AddComponent<SpriteRenderer>();
        renderer.sprite = square; renderer.sharedMaterial = material;
        renderer.color = new Color(1f, 0.8f, 0.2f); renderer.sortingOrder = 3;
        var trigger = goal.AddComponent<BoxCollider2D>();
        trigger.size = square.bounds.size; trigger.isTrigger = true;
        var pickup = goal.AddComponent<AmuletPickup>();
        pickup.amuletNumber = 1; pickup.nextScene = "Level02";
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save generated scene.");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.parent.gameObject;
        Debug.Log("Level01 ready: enclosed 56-unit forest room, solid floor and ceiling, 8-jump hill route, enemy placement markers and summit item marker. Backup: " + backup);
    }

    private static void Surface(string name, float x, float top, float width, float depth, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = new Vector3(x, top - depth / 2, 0);
        // Normalize the source sprite dimensions so the requested terrain dimensions are exact.
        go.transform.localScale = new Vector3(width / square.bounds.size.x, depth / square.bounds.size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square; renderer.sharedMaterial = material; renderer.color = color;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = square.bounds.size;
        collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
    }
}
