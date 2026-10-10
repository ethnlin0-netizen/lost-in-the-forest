using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildLevel05
{
    private static Sprite sprite;
    private static Material material;
    private static Transform root, terrain, scenery, hazards;
    [MenuItem("Tools/Lost In The Forest/Build Level 05")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes()) throw new IOException("Save open scenes first.");
        const string path="Assets/Scenes/Level05.unity";
        if(File.Exists(path))
        {
            var old=EditorSceneManager.OpenScene(path);
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if(!EditorSceneManager.SaveScene(old,"Assets/Scenes/Backups/Level05-before-corruption-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true)) throw new IOException("Backup failed.");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        if(sprite==null||material==null) throw new InvalidOperationException("Blockout assets missing.");
        root=new GameObject("Level05Route").transform;
        terrain=new GameObject("LevelGeometry").transform;terrain.SetParent(root);
        scenery=new GameObject("DenseCorruption_NoColliders").transform;scenery.SetParent(root);
        hazards=new GameObject("ThornAndAcidHazards").transform;hazards.SetParent(root);
        Block("SealedFloor",29,-4,76,3);
        Block("MazeRoof",11,14,40,3);
        Block("ArenaApproachRoof",49,18,36,3);
        Block("LeftWall",-8.5f,14,1,21);
        Block("RightWall",66.5f,18,1,25);
        Block("01_EntranceBank",-3,0,10,7);
        Block("02_ClimbPlatform",4,1,2.2f,0.6f);
        Block("03_UpperTurnPlatform",7,3.5f,2.2f,0.6f);
        Block("04_DescentPlatform",12,3.5f,2.2f,0.6f);
        Block("05_LowPassagePlatform",16,0,2.2f,0.6f);
        Block("06_ClimbAgainPlatform",21,1.5f,2.2f,0.6f);
        Block("07_UpperExitPlatform",24,3.8f,2.2f,0.6f);
        Block("08_ExitPlatform",28,4.8f,2.2f,0.6f);
        Block("MazeLowerWall01",9,2.5f,1.3f,6.5f);
        Block("MazeHangingWall",18,11,1.3f,6.5f);
        Block("MazeLowerWall02",26,2.5f,1.3f,6.5f);
        WallThorns("LowerWall01_Left",8.1f,-0.75f,6.5f,false);
        WallThorns("LowerWall01_Right",9.9f,-0.75f,6.5f,true);
        Thorns("LowerWall01_Tip",9,2.75f,1.3f,0.5f,false);
        WallThorns("HangingWall_Left",17.1f,7.75f,6.5f,false);
        WallThorns("HangingWall_Right",18.9f,7.75f,6.5f,true);
        Thorns("HangingWall_Tip",18,4.25f,1.3f,0.5f,true);
        WallThorns("LowerWall02_Left",25.1f,-0.75f,6.5f,false);
        WallThorns("LowerWall02_Right",26.9f,-0.75f,6.5f,true);
        Thorns("LowerWall02_Tip",26,2.75f,1.3f,0.5f,false);
        WallThorns("EntranceWallSpikes",-7.7f,5,10,true);
        Block("09_SafeDivider",32,2,6,9);
        Block("10_AcidApproachPlatform",40,1.5f,4,0.7f);
        Block("11_AcidApproachPlatform",46,4,3.5f,0.7f);
        Block("12_AcidApproachPlatform",52,6,3.5f,0.7f);
        Block("13_BossThreshold",61,8,10,15);
        Thorns("ThornFloor",15.5f,-3.4f,27,1.2f,false);
        Thorns("ThornRoof",15.5f,10.4f,27,1.2f,true);
        // Corruption continues over the acid; the corridor stays clear below it.
        Thorns("AcidRoofThorns",45.5f,14.4f,21,1.2f,true);
        var acid=new GameObject("PurpleAcidPool");acid.transform.SetParent(hazards);acid.transform.position=new Vector3(45.5f,-2.25f,0);
        acid.AddComponent<BoxCollider2D>().size=new Vector2(21,3.5f);
        acid.GetComponent<BoxCollider2D>().isTrigger=true;acid.AddComponent<Level02ThornHazard>();
        Visual("PurpleAcidWater",45.5f,-2.25f,21,3.5f,new Color(0.48f,0.045f,0.65f),scenery,2);
        Visual("AcidSurface",45.5f,-0.5f,21,0.14f,new Color(0.92f,0.35f,1),scenery,3);
        // A looming ruined arch signals the next scene's boss arena.
        Visual("BossArch_LeftPillar",58,11.5f,1.2f,7,new Color(0.24f,0.12f,0.29f),scenery,-2);
        Visual("BossArch_RightPillar",64,11.5f,1.2f,7,new Color(0.24f,0.12f,0.29f),scenery,-2);
        Visual("BossArch_Lintel",61,15,7.5f,1,new Color(0.30f,0.12f,0.34f),scenery,-2);
        Visual("ArenaDoorway",61,11.5f,5,7,new Color(0.07f,0.015f,0.10f),scenery,-3);
        for(int i=0;i<40;i++)
        {
            float x=-7+i*1.8f;
            float roofY=x<31?11.3f:15.3f;
            Visual("DeadHangingVine_"+i,x,roofY-0.5f+(i%3)*0.2f,0.14f+(i%2)*0.08f,1.6f+(i%4)*0.5f,new Color(0.075f,0.025f,0.095f),scenery,1);
            Visual("CorruptedRoofRoot_"+i,x,roofY,1.2f,0.4f,new Color(0.22f,0.055f,0.25f),scenery,1);
            if(i%3==0) Visual("DistantDeadTrunk_"+i,x,4,0.9f,15,new Color(0.025f,0.055f,0.065f),scenery,-20);
        }
        for(int i=0;i<30;i++)
            Visual("AcidRipple_"+i,35.35f+i*0.68f,-0.43f,0.35f,0.04f,new Color(1,0.65f,1),scenery,3);
        var spawn=new GameObject("PlayerSpawn").transform;spawn.SetParent(root);spawn.position=new Vector3(-5,1,0);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"),scene);
        player.transform.position=spawn.position;
        var reset=player.GetComponent<Level02ThornRespawn>();if(reset==null)reset=player.AddComponent<Level02ThornRespawn>();reset.entrance=spawn;
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=8;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.015f,0.03f,0.045f);
        var follow=camera.gameObject.AddComponent<Level01Camera>();follow.target=player.transform;
        float half=camera.orthographicSize*camera.aspect;follow.minimum=new Vector2(-8+half,1);follow.maximum=new Vector2(66-half,10);
        camera.transform.position=new Vector3(follow.minimum.x,2,-10);
        var diamond=Visual("Amulet05_Diamond",61,9.3f,0.7f,0.7f,new Color(1,0.35f,0.7f),root,3);
        diamond.transform.rotation=Quaternion.Euler(0,0,45);diamond.AddComponent<BoxCollider2D>().isTrigger=true;
        var pickup=diamond.AddComponent<AmuletPickup>();pickup.amuletNumber=5;pickup.nextScene="Level06";
        if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("Could not save Level05.");
        var list=EditorBuildSettings.scenes.ToList();var entry=list.FirstOrDefault(s=>s.path==path);
        if(entry==null)list.Add(new EditorBuildSettingsScene(path,true));else entry.enabled=true;
        EditorBuildSettings.scenes=list.ToArray();AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
    }
    private static GameObject Visual(string name,float x,float y,float w,float h,Color color,Transform parent,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,0);
        go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=color;r.sortingOrder=order;return go;
    }
    private static void Block(string name,float x,float top,float w,float depth)
    {
        var go=Visual(name,x,top-depth/2,w,depth,new Color(0.13f,0.075f,0.16f),terrain,0);
        var b=go.AddComponent<BoxCollider2D>();b.size=sprite.bounds.size;b.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
        if(!name.Contains("Roof")&&!name.Contains("Wall"))Visual(name+"_CorruptedEdge",x,top-0.1f,w,0.2f,new Color(0.26f,0.15f,0.30f),scenery,1);
    }
    [MenuItem("Tools/Lost In The Forest/Add Level 05 Thorn Enemies")]
    public static void AddThornEnemies()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level05.unity");
        if(GameObject.Find("Level05Route/ThornSectionEnemies")!=null)return;
        Directory.CreateDirectory("Assets/Scenes/Backups");
        if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Level05-before-enemies-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Backup failed.");
        var route=GameObject.Find("Level05Route").transform;
        var geometry=route.Find("LevelGeometry");
        var nav=geometry.GetComponent<PlatformNavigation2D>();if(nav==null)nav=geometry.gameObject.AddComponent<PlatformNavigation2D>();
        var navData=new SerializedObject(nav);navData.FindProperty("surfaceRoot").objectReferenceValue=geometry;navData.ApplyModifiedPropertiesWithoutUndo();
        var group=new GameObject("ThornSectionEnemies").transform;group.SetParent(route);
        Physics2D.SyncTransforms();
        string[] kinds={"Chomper","Spora","Collusus"};
        string[] platforms={"04_DescentPlatform","07_UpperExitPlatform","09_SafeDivider"};
        for(int i=0;i<kinds.Length;i++)
        {
            var floor=geometry.Find(platforms[i]).GetComponent<BoxCollider2D>();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IntegratedEnemies/"+kinds[i]+".prefab");
            if(prefab==null)throw new InvalidOperationException("Enemy prefab missing: "+kinds[i]);
            var enemy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);enemy.transform.SetParent(group);
            enemy.name=kinds[i]+"_ThornSection";var box=enemy.GetComponent<BoxCollider2D>();
            float offset=i==2?1.0f:0.3f;
            enemy.transform.position=new Vector3(floor.bounds.center.x+offset,floor.bounds.max.y+box.size.y/2-box.offset.y+0.02f,0);
            var data=new SerializedObject(enemy.GetComponent<BlightEnemy>());
            data.FindProperty("patrolRadius").floatValue=i==2?0.8f:0.15f;data.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void WallThorns(string name,float x,float y,float height,bool right)
    {
        Thorns(name,x,y,height,0.5f,false);
        hazards.Find(name).rotation=Quaternion.Euler(0,0,right?-90:90);
    }
    private static void Thorns(string name,float x,float y,float w,float h,bool inverted)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Materials/Level02ThornTriangle.asset");
        if(mesh==null)throw new InvalidOperationException("Thorn mesh missing.");
        var go=new GameObject(name);go.transform.SetParent(hazards);go.transform.position=new Vector3(x,y,0);
        var b=go.AddComponent<BoxCollider2D>();b.size=new Vector2(w,h);b.isTrigger=true;go.AddComponent<Level02ThornHazard>();
        int count=Mathf.CeilToInt(w/0.55f);
        for(int i=0;i<count;i++)
        {
            var spike=new GameObject("Thorn_"+i);spike.transform.SetParent(go.transform);
            spike.transform.localPosition=new Vector3(-w/2+(i+0.5f)*w/count,0,0);
            spike.transform.localScale=new Vector3(w/count,inverted?-h:h,1);
            spike.AddComponent<MeshFilter>().sharedMesh=mesh;var r=spike.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=2;
        }
    }
}
