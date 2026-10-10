using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel02
{
    private static Sprite square;
    private static Material material;
    private static Transform terrain;
    private static Transform hazards;

    [MenuItem("Tools/Lost In The Forest/Build Level 02")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (open.isDirty && string.IsNullOrEmpty(open.path))
                throw new InvalidOperationException("Save your untitled scene first.");
            if (open.isDirty && !EditorSceneManager.SaveScene(open)) throw new IOException("Scene could not be saved.");
        }
        const string path = "Assets/Scenes/Level02.unity";
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path)
            scene = File.Exists(path) ? EditorSceneManager.OpenScene(path) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        if (File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if (!EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Level02-before-thorns-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true))
                throw new IOException("Backup failed.");
        }
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        if (square == null || material == null) throw new InvalidOperationException("Blockout assets are missing.");
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Level01Route" || go.name == "Level02Route" || go.name == "LevelGeometry")
                Undo.DestroyObjectImmediate(go);
        var route = new GameObject("Level02Route").transform;
        terrain = new GameObject("LevelGeometry").transform; terrain.SetParent(route);
        hazards = new GameObject("ThornHazards").transform; hazards.SetParent(route);

        Block("ContinuousFloor", 23, -3, 64, 5);
        Block("Ceiling", 23, 23, 64, 3);
        Block("LeftWall", -9.5f, 23, 1, 31);
        Block("RightWall", 55.5f, 23, 1, 31);
        // An elevated entrance continues the feeling of Level01's hilltop.
        Block("01_EntranceLedge", -3, 11, 14, 14);
        Block("02_DropLanding", 8, 6, 5, 0.8f);
        Block("03_LowerSafeIsland", 14, 3, 6, 6);
        Block("04_DecayingBranch", 21, 4, 4, 0.75f);
        Block("05_ThornCrossing", 27, 6.5f, 4, 0.75f);
        Block("06_CombatLedge", 33, 6.5f, 8, 0.75f);
        Block("07_FinalBranch", 40, 9, 4, 0.75f);
        Block("08_ItemSanctuary", 47, 11.5f, 10, 14.5f);
        ThornStrip("ThornFloor", 25.5f, -2.5f, 43, 1, false);
        ThornStrip("CeilingThorns", 25, 19.5f, 14, 1, true);

        var entrance = new GameObject("PlayerSpawn").transform;
        entrance.SetParent(route); entrance.position = new Vector3(-5, 12, 0);
        var player = scene.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<Movement>() != null);
        if (player == null)
            player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"), scene);
        player.transform.position = entrance.position;
        var oldRecovery = player.GetComponent<Level01Recovery>();
        if (oldRecovery != null) Undo.DestroyObjectImmediate(oldRecovery);
        var respawn = player.GetComponent<Level02ThornRespawn>();
        if (respawn == null) respawn = player.AddComponent<Level02ThornRespawn>();
        respawn.entrance = entrance;
        var camera = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c != null);
        if (camera == null) camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 7.5f;
        camera.backgroundColor = new Color(0.035f, 0.06f, 0.09f); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(-9 + camera.orthographicSize * camera.aspect, 12.5f, -10);
        var follow = camera.GetComponent<Level01Camera>();
        if (follow == null) follow = camera.gameObject.AddComponent<Level01Camera>();
        follow.target = player.transform;
        float halfWidth = camera.orthographicSize * camera.aspect;
        follow.minimum = new Vector2(-9 + halfWidth, 4.5f);
        follow.maximum = new Vector2(55 - halfWidth, 12.5f);

        var item = Visual("Amulet02", 49, 13, 0.7f, 0.7f, route, new Color(1, 0.65f, 0.2f));
        item.transform.rotation = Quaternion.Euler(0, 0, 45);
        item.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var trigger = item.AddComponent<BoxCollider2D>();
        trigger.size = square.bounds.size; trigger.isTrigger = true;
        var pickup = item.AddComponent<AmuletPickup>();
        pickup.amuletNumber = 2; pickup.nextScene = "Level03";
        var encounter = new GameObject("EnemyPlacementMarkers").transform; encounter.SetParent(route);
        foreach (var position in new[] { new Vector2(14, 4), new Vector2(33, 7.5f), new Vector2(46, 12.5f) })
        {
            var marker = new GameObject("EnemyPlacement_" + encounter.childCount).transform;
            marker.SetParent(encounter); marker.position = position;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Level02 could not be saved.");
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
        AssetDatabase.SaveAssets(); Selection.activeGameObject = route.gameObject;
        Debug.Log("Level02 ready: elevated entrance, thorn room, decaying platforms, and return-to-entrance hazards.");
    }

    private static GameObject Visual(string name, float x, float y, float width, float height, Transform parent, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(width / square.bounds.size.x, height / square.bounds.size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = square;
        renderer.sharedMaterial = material; renderer.color = color;
        return go;
    }

    private static void Block(string name, float x, float top, float width, float depth)
    {
        var color = Color.Lerp(new Color(0.25f, 0.39f, 0.32f), new Color(0.22f, 0.14f, 0.24f), Mathf.Clamp01((x + 3) / 50));
        var go = Visual(name, x, top - depth / 2, width, depth, terrain, color);
        var box = go.AddComponent<BoxCollider2D>(); box.size = square.bounds.size;
        box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
    }

    private static void ThornStrip(string name, float x, float y, float width, float height, bool ceiling)
    {
        var go = new GameObject(name); go.transform.SetParent(hazards);
        go.transform.position = new Vector3(x, y, 0);
        var box = go.AddComponent<BoxCollider2D>(); box.size = new Vector2(width, height); box.isTrigger = true;
        go.AddComponent<Level02ThornHazard>();
        const string meshPath = "Assets/Materials/Level02ThornTriangle.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "ThornTriangle" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, -0.5f, 0) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.colors = new[] { new Color(0.7f, 0.22f, 0.3f), new Color(0.95f, 0.48f, 0.44f), new Color(0.7f, 0.22f, 0.3f) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.right }; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
        int count = Mathf.CeilToInt(width / 0.65f);
        for (int i = 0; i < count; i++)
        {
            var spike = new GameObject("Thorn_" + i); spike.transform.SetParent(go.transform);
            spike.transform.localPosition = new Vector3(-width / 2 + (i + 0.5f) * width / count, 0, 0);
            spike.transform.localScale = new Vector3(width / count, ceiling ? -height : height, 1);
            spike.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = spike.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 2;
        }
    }
}
