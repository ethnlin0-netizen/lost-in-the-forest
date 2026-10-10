using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel03
{
    private static Sprite sprite;
    private static Material material;
    private static Transform root;

    [MenuItem("Tools/Lost In The Forest/Build Level 03")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        for (int i=0; i<SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (open.isDirty && (string.IsNullOrEmpty(open.path) || !EditorSceneManager.SaveScene(open)))
                throw new IOException("Save the open scene before building.");
        }
        const string path = "Assets/Scenes/Level03.unity";
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path) scene = File.Exists(path) ? EditorSceneManager.OpenScene(path) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        if (File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if (!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Level03-before-combat-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))
                throw new IOException("Backup failed.");
        }
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        if(sprite==null || material==null) throw new InvalidOperationException("Blockout assets missing.");
        foreach(var go in scene.GetRootGameObjects())
            if(go.name=="Level01Route" || go.name=="Level02Route" || go.name=="Level03Route" || go.name=="LevelGeometry")
                Undo.DestroyObjectImmediate(go);
        root = new GameObject("Level03Route").transform;
        var geometry = new GameObject("LevelGeometry").transform; geometry.SetParent(root);
        var stone = new Color(0.075f,0.095f,0.18f);
        Box("ContinuousCombatFloor",20,-4,60,4,stone,geometry,true);
        Box("EnclosedCeiling",20,11,60,3,stone,geometry,true);
        Box("LeftWall",-9.5f,5,1,20,stone,geometry,true);
        Box("RightWall",49.5f,5,1,20,stone,geometry,true);
        var scenery = new GameObject("DecorativeStonework_NoColliders").transform; scenery.SetParent(root);
        // Border rocks stay outside the playable corridor. The entire walkable
        // floor remains at Y=-2; decoration introduces no traversal obstacles.
        for(int i=0; i<40; i++)
        {
            float x=-9+i*1.5f;
            var color=Color.Lerp(stone,new Color(0.16f,0.20f,0.37f),(i%5)/5f);
            var floorRock=Box("FloorStone_"+i,x,-2.4f,1.4f,0.65f,color,scenery,false);
            floorRock.transform.rotation=Quaternion.Euler(0,0,(i%3-1)*8);
            Box("CeilingStone_"+i,x,10.1f,1.3f,0.9f,color,scenery,false);
        }
        for(int i=0; i<9; i++)
        {
            var column=Box("DistantPillar_"+i,-8+i*7,4,1.2f+(i%3)*0.4f,16,new Color(0.045f,0.065f,0.12f),scenery,false);
            column.GetComponent<SpriteRenderer>().sortingOrder=-20; column.transform.position+=Vector3.forward*2;
        }
        for(int i=0;i<50;i++)
        {
            var mote=Box("PaleMote_"+i,-7+(i*7.73f)%55,-0.5f+(i*3.17f)%12,0.045f,0.045f,new Color(0.48f,0.58f,0.8f),scenery,false);
            mote.GetComponent<SpriteRenderer>().sortingOrder=-10;
        }
        var spawn=new GameObject("PlayerSpawn").transform; spawn.SetParent(root); spawn.position=new Vector3(-5,-1,0);
        var player=scene.GetRootGameObjects().FirstOrDefault(g=>g.GetComponent<Movement>()!=null);
        if(player==null) player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"),scene);
        player.transform.position=spawn.position;
        var thornReset=player.GetComponent<Level02ThornRespawn>(); if(thornReset!=null) Undo.DestroyObjectImmediate(thornReset);
        var recovery=player.GetComponent<Level01Recovery>(); if(recovery!=null) Undo.DestroyObjectImmediate(recovery);
        var targets=new GameObject("EnemyPlaceholders_ReplaceWithTeamPrefabs").transform; targets.SetParent(root);
        for(int i=0;i<3;i++)
        {
            var target=Box("EnemyPlaceholder_"+(i+1),8+i*12,-1.2f,0.9f,1.6f,new Color(0.57f,0.39f,0.73f),targets,true);
            var body=target.AddComponent<Rigidbody2D>(); body.gravityScale=3; body.constraints=RigidbodyConstraints2D.FreezeRotation;
            target.AddComponent<PracticeTarget>();
        }
        var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).FirstOrDefault(c=>c!=null);
        if(camera==null) camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=7;
        camera.backgroundColor=new Color(0.025f,0.035f,0.085f); camera.clearFlags=CameraClearFlags.SolidColor;
        var follow=camera.GetComponent<Level01Camera>(); if(follow==null) follow=camera.gameObject.AddComponent<Level01Camera>();
        float halfWidth=camera.orthographicSize*camera.aspect;
        follow.target=player.transform; follow.minimum=new Vector2(-9+halfWidth,-3f); follow.maximum=new Vector2(49-halfWidth,3.5f);
        camera.transform.position=new Vector3(follow.minimum.x,3.5f,-10);
        var amulet=Box("Amulet03",44,-0.7f,0.7f,0.7f,new Color(0.35f,0.65f,1),root,true);
        amulet.transform.rotation=Quaternion.Euler(0,0,45); amulet.GetComponent<BoxCollider2D>().isTrigger=true;
        amulet.GetComponent<SpriteRenderer>().sortingOrder=3;
        var pickup=amulet.AddComponent<AmuletPickup>(); pickup.amuletNumber=3; pickup.nextScene="Level04";
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,path)) throw new IOException("Could not save Level03.");
        var scenes=EditorBuildSettings.scenes.ToList();
        var entry=scenes.FirstOrDefault(s=>s.path==path);
        if(entry==null) scenes.Add(new EditorBuildSettingsScene(path,true)); else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets(); Selection.activeGameObject=root.gameObject;
        Debug.Log("Level03 ready: flat enclosed blue combat room, three practice enemies, no traversal hazards, Amulet03.");
    }

    private static GameObject Box(string name,float x,float y,float width,float height,Color color,Transform parent,bool solid)
    {
        var go=new GameObject(name); go.transform.SetParent(parent); go.transform.position=new Vector3(x,y,0);
        go.transform.localScale=new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
        var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sharedMaterial=material; renderer.color=color;
        if(solid)
        {
            var box=go.AddComponent<BoxCollider2D>(); box.size=sprite.bounds.size;
            box.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
        }
        return go;
    }
}
