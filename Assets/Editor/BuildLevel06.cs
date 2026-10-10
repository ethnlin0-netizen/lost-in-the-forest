using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildLevel06
{
    private static Sprite sprite;
    private static Material material;
    private static Transform root, terrain, decoration;
    [MenuItem("Tools/Lost In The Forest/Build Level 06")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        const string path="Assets/Scenes/Level06.unity";
        if(File.Exists(path))
        {
            var old=EditorSceneManager.OpenScene(path);
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if(!EditorSceneManager.SaveScene(old,"Assets/Scenes/Backups/Level06-before-arena-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Backup failed.");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        if(sprite==null||material==null)throw new InvalidOperationException("Blockout assets missing.");
        root=new GameObject("Level06BossArena").transform;
        terrain=new GameObject("LevelGeometry").transform;terrain.SetParent(root);
        decoration=new GameObject("ArenaRuins_NoColliders").transform;decoration.SetParent(root);
        Block("WideArenaFloor",20,-2.5f,62,5);
        Block("LeftArenaWall",-10.5f,7,1,24);
        Block("RightArenaWall",50.5f,7,1,24);
        Block("HighArenaRoof",20,19,62,3);
        for(int i=0;i<9;i++)
        {
            float x=-7+i*7;
            Visual("DistantRuinedPillar_"+i,x,8,1.4f,17,new Color(0.055f,0.04f,0.09f),decoration,-20);
            Visual("RuinedColumnCap_"+i,x,15.5f,2.7f,0.65f,new Color(0.10f,0.055f,0.14f),decoration,-19);
        }
        Visual("ArenaFloorEdge",20,-0.12f,60,0.24f,new Color(0.31f,0.19f,0.36f),decoration,1);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"),scene);
        player.transform.position=new Vector3(-5,1,0);
        var spawn=new GameObject("PlayerSpawn").transform;spawn.SetParent(root);spawn.position=player.transform.position;
        var nav=terrain.gameObject.AddComponent<PlatformNavigation2D>();
        var navData=new SerializedObject(nav);navData.FindProperty("surfaceRoot").objectReferenceValue=terrain;navData.ApplyModifiedPropertiesWithoutUndo();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IntegratedEnemies/Collusus.prefab");
        if(prefab==null)throw new InvalidOperationException("Placeholder boss prefab missing.");
        var boss=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        boss.name="PlaceholderBoss_Collusus";boss.transform.SetParent(root);boss.transform.localScale=Vector3.one*1.5f;
        var box=boss.GetComponent<BoxCollider2D>();boss.transform.position=new Vector3(27,(box.size.y/2-box.offset.y)*1.5f+0.02f,0);
        var health=new SerializedObject(boss.GetComponent<EnemyHealth>());health.FindProperty("maximumHealth").floatValue=300;health.ApplyModifiedPropertiesWithoutUndo();
        var ai=new SerializedObject(boss.GetComponent<BlightEnemy>());ai.FindProperty("detectionRange").floatValue=60;ai.ApplyModifiedPropertiesWithoutUndo();
        var crystal=Visual("FinalCrystal",20,1.2f,1,1,new Color(0.9f,0.55f,1),root,3);
        crystal.transform.rotation=Quaternion.Euler(0,0,45);crystal.SetActive(false);
        var controller=new GameObject("BossRewardController");controller.transform.SetParent(root);
        var reward=controller.AddComponent<BossCrystalReward>();reward.boss=boss.GetComponent<EnemyHealth>();reward.crystal=crystal;
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=11;
        camera.backgroundColor=new Color(0.025f,0.015f,0.045f);camera.clearFlags=CameraClearFlags.SolidColor;
        var follow=camera.gameObject.AddComponent<Level01Camera>();follow.target=player.transform;
        float half=camera.orthographicSize*camera.aspect;follow.minimum=new Vector2(-10+half,7);follow.maximum=new Vector2(50-half,7);
        camera.transform.position=new Vector3(follow.minimum.x,7,-10);
        if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("Could not save Level06.");
        var scenes=EditorBuildSettings.scenes.ToList();var entry=scenes.FirstOrDefault(s=>s.path==path);
        if(entry==null)scenes.Add(new EditorBuildSettingsScene(path,true));else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
    }
    private static GameObject Visual(string name,float x,float y,float w,float h,Color color,Transform parent,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,0);
        go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=color;r.sortingOrder=order;return go;
    }
    private static void Block(string name,float x,float y,float w,float h)
    {
        var go=Visual(name,x,y,w,h,new Color(0.15f,0.085f,0.19f),terrain,0);
        var b=go.AddComponent<BoxCollider2D>();b.size=sprite.bounds.size;
        b.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
    }
}
