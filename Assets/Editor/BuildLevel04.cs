using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildLevel04
{
    private static Sprite sprite;
    private static Material material;
    private static Transform root, geometry, decoration;
    [MenuItem("Tools/Lost In The Forest/Build Level 04")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes()) throw new IOException("Save open scenes first.");
        const string path="Assets/Scenes/Level04.unity";
        if(File.Exists(path))
        {
            var previous=EditorSceneManager.OpenScene(path);
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if(!EditorSceneManager.SaveScene(previous,"Assets/Scenes/Backups/Level04-before-acid-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true)) throw new IOException("Backup failed.");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        if(sprite==null||material==null) throw new InvalidOperationException("Blockout assets missing.");
        root=new GameObject("Level04Route").transform;
        geometry=new GameObject("LevelGeometry").transform; geometry.SetParent(root);
        decoration=new GameObject("DecayingRuins_NoColliders").transform; decoration.SetParent(root);
        // Two enclosed pools with a dry landing between them.
        Block("SealedPoolBottom",18,-5,54,3);
        Block("EnclosedRoof",18,14,54,3);
        Block("LeftWall",-8.5f,6,1,20);
        Block("RightWall",44.5f,6,1,20);
        Block("01_EntranceBank",-3,0,10,7);
        Block("02_LowPoolPlatform",5,0.7f,3.5f,0.7f);
        Block("03_RaisedRuins",10,2.3f,4,0.7f);
        Block("04_CentralBank",17,1,8,8);
        Block("05_SecondPoolPlatform",24,2.5f,3,0.7f);
        Block("06_HighRuins",29,4,4,0.7f);
        Block("07_FinalBank",38,2.5f,12,9.5f);
        // Hanging blocks frame the room without closing the jump corridor.
        Block("HangingRuinsLeft",5,12,7,3);
        Block("HangingRuinsRight",28,12,8,3);
        Acid("PurpleAcidPool01",7.5f,11);
        Acid("PurpleAcidPool02",26.5f,11);
        for(int i=0;i<12;i++)
        {
            var pillar=Visual("DistantBrokenColumn_"+i,-7+i*4.3f,4,0.7f,14,new Color(0.035f,0.045f,0.07f),decoration,-20);
            Visual("ColumnCap_"+i,pillar.transform.position.x,8.5f,1.6f,0.4f,new Color(0.06f,0.09f,0.12f),decoration,-19);
            Visual("HangingDeadVine_"+i,-6+i*4.2f,10.5f-(i%3)*0.5f,0.13f,3+(i%3),new Color(0.10f,0.075f,0.13f),decoration,1);
        }
        var spawn=new GameObject("PlayerSpawn").transform; spawn.SetParent(root); spawn.position=new Vector3(-5,1,0);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"),scene);
        player.transform.position=spawn.position;
        var reset=player.GetComponent<Level02ThornRespawn>(); if(reset==null) reset=player.AddComponent<Level02ThornRespawn>(); reset.entrance=spawn;
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=8;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.025f,0.025f,0.055f);
        var follow=camera.gameObject.AddComponent<Level01Camera>(); follow.target=player.transform;
        float half=camera.orthographicSize*camera.aspect;
        follow.minimum=new Vector2(-8+half,1); follow.maximum=new Vector2(44-half,5);
        camera.transform.position=new Vector3(follow.minimum.x,2.5f,-10);
        var item=Visual("Amulet04",40,4,0.7f,0.7f,new Color(0.95f,0.55f,1),root,3);
        item.transform.rotation=Quaternion.Euler(0,0,45);
        item.AddComponent<BoxCollider2D>().isTrigger=true;
        var pickup=item.AddComponent<AmuletPickup>(); pickup.amuletNumber=4; pickup.nextScene="Level05";
        if(!EditorSceneManager.SaveScene(scene,path)) throw new IOException("Could not save Level04.");
        var scenes=EditorBuildSettings.scenes.ToList(); var entry=scenes.FirstOrDefault(s=>s.path==path);
        if(entry==null) scenes.Add(new EditorBuildSettingsScene(path,true)); else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets(); Selection.activeGameObject=root.gameObject;
    }
    private static GameObject Visual(string name,float x,float y,float width,float height,Color color,Transform parent,int order=0)
    {
        var go=new GameObject(name); go.transform.SetParent(parent); go.transform.position=new Vector3(x,y,0);
        go.transform.localScale=new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>(); r.sprite=sprite; r.sharedMaterial=material; r.color=color; r.sortingOrder=order;
        return go;
    }
    private static void Block(string name,float x,float top,float width,float depth)
    {
        var go=Visual(name,x,top-depth/2,width,depth,Color.Lerp(new Color(0.10f,0.17f,0.19f),new Color(0.18f,0.10f,0.20f),Mathf.Clamp01((x+8)/52)),geometry);
        var b=go.AddComponent<BoxCollider2D>(); b.size=sprite.bounds.size;
        b.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
        Visual(name+"_DecayedEdge",x,top-0.1f,width,0.2f,new Color(0.23f,0.23f,0.30f),decoration,1);
    }
    private static void Acid(string name,float x,float width)
    {
        var pool=new GameObject(name); pool.transform.SetParent(root); pool.transform.position=new Vector3(x,-2.75f,0);
        var trigger=pool.AddComponent<BoxCollider2D>(); trigger.size=new Vector2(width,4.5f); trigger.isTrigger=true;
        pool.AddComponent<Level02ThornHazard>();
        Visual(name+"_Water",x,-2.75f,width,4.5f,new Color(0.42f,0.075f,0.62f),decoration,2);
        Visual(name+"_BrightSurface",x,-0.5f,width,0.14f,new Color(0.85f,0.38f,1),decoration,3);
        for(int i=0;i<18;i++) Visual(name+"_SurfaceRipple_"+i,x-width/2+(i+0.5f)*width/18,-0.42f+(i%3)*0.025f,0.28f,0.04f,new Color(0.95f,0.65f,1),decoration,3);
    }
}
