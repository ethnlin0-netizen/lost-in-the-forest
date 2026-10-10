using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IntegrateLevelEnemies
{
    [MenuItem("Tools/Lost In The Forest/Integrate Level Enemies")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var s=SceneManager.GetSceneAt(i);
            if(s.isDirty && (string.IsNullOrEmpty(s.path)||!EditorSceneManager.SaveScene(s))) throw new IOException("Save open scenes first.");
        }
        CreateVariant("Chomper","Chomper"); CreateVariant("Spora","Spora"); CreateVariant("Collusus","Collosus");
        for(int level=1;level<=3;level++) InstallScene(level);
        EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");
        AssetDatabase.SaveAssets();
    }

    private static void CreateVariant(string aiName,string artName)
    {
        string path="Assets/Prefabs/IntegratedEnemies/"+aiName+".prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null) return;
        Directory.CreateDirectory("Assets/Prefabs/IntegratedEnemies"); AssetDatabase.Refresh();
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/"+aiName+".prefab");
            var art=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyCharacters/"+artName+".prefab");
            if(source==null||art==null) throw new InvalidOperationException("Missing enemy assets: "+aiName);
            var enemy=(GameObject)PrefabUtility.InstantiatePrefab(source,preview);
            // Animated gameplay prefabs already include their finished artwork.
            if(enemy.GetComponent<EnemyVisualAnimator>() != null)
            {
                if(PrefabUtility.SaveAsPrefabAsset(enemy,path)==null) throw new IOException("Could not save enemy variant.");
                return;
            }
            var data=new SerializedObject(enemy.GetComponent<BlightEnemy>());
            var visuals=(Transform)data.FindProperty("visuals").objectReferenceValue;
            var cue=(GameObject)data.FindProperty("attackVisual").objectReferenceValue;
            if(visuals==null) throw new InvalidOperationException("Enemy visual root missing.");
            foreach(var renderer in visuals.GetComponentsInChildren<SpriteRenderer>(true))
                if(cue==null || !renderer.transform.IsChildOf(cue.transform)) renderer.enabled=false;
            var skin=(GameObject)PrefabUtility.InstantiatePrefab(art,preview);
            skin.name="CharacterArtwork"; skin.transform.SetParent(visuals,false);
            var box=enemy.GetComponent<BoxCollider2D>(); var image=skin.GetComponent<SpriteRenderer>();
            float size=Mathf.Min(box.size.y*1.1f/image.sprite.bounds.size.y,box.size.x*1.6f/image.sprite.bounds.size.x);
            skin.transform.localScale=Vector3.one*size;
            skin.transform.localPosition=new Vector3(box.offset.x,box.offset.y-box.size.y/2,0);
            image.sortingOrder=2;
            if(PrefabUtility.SaveAsPrefabAsset(enemy,path)==null) throw new IOException("Could not save enemy variant.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    public static void InstallScene(int level)
    {
        string path="Assets/Scenes/Level0"+level+".unity";
        var scene=EditorSceneManager.OpenScene(path);
        Directory.CreateDirectory("Assets/Scenes/Backups");
        if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Level0"+level+"-before-enemies-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true)) throw new IOException("Backup failed.");
        foreach(var go in scene.GetRootGameObjects())
            foreach(var t in go.GetComponentsInChildren<Transform>(true).Reverse().ToArray())
                if(t!=null && (t.name=="IntegratedEnemies"||t.name=="EnemyPlaceholders_ReplaceWithTeamPrefabs")) Undo.DestroyObjectImmediate(t.gameObject);
        // Clear actual enemy components too if someone placed one in Level02.
        if(level==2)
        {
            foreach(var go in scene.GetRootGameObjects())
                foreach(var enemy in go.GetComponentsInChildren<BlightEnemy>(true)) Undo.DestroyObjectImmediate(enemy.gameObject);
            foreach(var go in scene.GetRootGameObjects())
                foreach(var target in go.GetComponentsInChildren<PracticeTarget>(true)) Undo.DestroyObjectImmediate(target.gameObject);
            EditorSceneManager.SaveScene(scene); return;
        }
        var terrain=GameObject.Find("Level0"+level+"Route/LevelGeometry");
        if(terrain==null) throw new InvalidOperationException("Terrain missing in "+path);
        var navigation=terrain.GetComponent<PlatformNavigation2D>();
        if(navigation==null) navigation=terrain.AddComponent<PlatformNavigation2D>();
        var navData=new SerializedObject(navigation); navData.FindProperty("surfaceRoot").objectReferenceValue=terrain.transform; navData.ApplyModifiedPropertiesWithoutUndo();
        var group=new GameObject("IntegratedEnemies").transform;
        Physics2D.SyncTransforms();
        var surfaces=terrain.GetComponentsInChildren<BoxCollider2D>().Where(b=>!b.isTrigger && b.bounds.size.x>=3 && b.bounds.size.x>b.bounds.size.y && !b.name.Contains("Ceiling") && !b.name.Contains("Wall") && !b.name.Contains("Overhang")).ToArray();
        // Use current terrain rather than rebuilding either level.
        if(level==1)
        {
            Place("Chomper",surfaces,2,-1.5f,group);
            Place("Spora",surfaces,23,11,group);
        }
        else
        {
            Place("Chomper",surfaces,1,-2,group);
            Place("Chomper",surfaces,9,-5,group);
            Place("Spora",surfaces,18,-5,group);
            Place("Collusus",surfaces,28,2.7f,group);
        }
        var player=scene.GetRootGameObjects().Select(g=>g.GetComponent<Movement>()).FirstOrDefault(p=>p!=null);
        if(player==null||player.GetComponent<PlayerHealth>()==null) throw new InvalidOperationException("Player health missing in "+path);
        EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Save failed.");
    }

    private static void Place(string kind,BoxCollider2D[] surfaces,float desiredX,float desiredTop,Transform group)
    {
        var surface=surfaces.OrderBy(b=>Mathf.Abs(b.bounds.max.y-desiredTop)*2 + Mathf.Max(0,b.bounds.min.x-desiredX)+Mathf.Max(0,desiredX-b.bounds.max.x)).FirstOrDefault();
        if(surface==null) throw new InvalidOperationException("No safe surface for "+kind);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IntegratedEnemies/"+kind+".prefab");
        var enemy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group.gameObject.scene);
        enemy.transform.SetParent(group); enemy.name=kind+"_"+group.childCount;
        var box=enemy.GetComponent<BoxCollider2D>();
        float x=Mathf.Clamp(desiredX,surface.bounds.min.x+box.size.x/2+0.3f,surface.bounds.max.x-box.size.x/2-0.3f);
        enemy.transform.position=new Vector3(x,surface.bounds.max.y+box.size.y/2-box.offset.y+0.02f,0);
    }
}
