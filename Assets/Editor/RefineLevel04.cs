using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RefineLevel04
{
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        EditorSceneManager.SaveOpenScenes();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level04.unity");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Level04-before-refinement-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        Physics2D.SyncTransforms();
        var root=GameObject.Find("Level04Route").transform;
        var terrain=root.Find("LevelGeometry");
        var deco=root.Find("DecayingRuins_NoColliders");
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Level01Blockout.mat");
        foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="IntegratedEnemies"||t.name.StartsWith("FirstDip")||t.name.StartsWith("FinalDip")||t.name.StartsWith("ExtendedBrokenColumn_")||t.name.StartsWith("ExtendedDeadVine_")).ToArray())
            if(t!=null) UnityEngine.Object.DestroyImmediate(t.gameObject);
        var first=GameObject.Find("PurpleAcidPool01");
        first.GetComponent<BoxCollider2D>().enabled=true;
        if(first.GetComponent<Level02ThornHazard>()==null) first.AddComponent<Level02ThornHazard>();
        var second=GameObject.Find("PurpleAcidPool02").GetComponent<BoxCollider2D>();
        second.transform.position=new Vector3(51.75f,-2.835f,0);
        second.offset=Vector2.zero; second.size=new Vector2(22.7f,4.67f); second.isTrigger=true;
        foreach(var name in new[]{"PurpleAcidPool02_Water","PurpleAcidPool02_BrightSurface"})
        {
            var visual=GameObject.Find(name); if(visual==null) continue;
            bool water=name.EndsWith("_Water");visual.transform.position=new Vector3(51.75f,water?-2.835f:-0.5f,0);
            visual.transform.localScale=new Vector3(22.7f/sprite.bounds.size.x,(water?4.67f:0.14f)/sprite.bounds.size.y,1);
        }
        // Extend the enclosure to the user's new final platform.
        Resize(GameObject.Find("EnclosedRoof"),new Vector2(27.55f,12.5f),new Vector2(73.1f,3),sprite);
        Resize(GameObject.Find("LeftWall"),new Vector2(-8.5f,2),new Vector2(1,22),sprite);
        // Bring decorative ledges into alignment with all edited terrain.
        foreach(var t in deco.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("_DecayedEdge")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        Physics2D.SyncTransforms();
        foreach(var box in terrain.GetComponentsInChildren<BoxCollider2D>())
        {
            box.GetComponent<SpriteRenderer>().color=Color.Lerp(new Color(0.10f,0.17f,0.19f),new Color(0.18f,0.10f,0.20f),Mathf.Clamp01((box.bounds.center.x+8)/72));
            if(box.bounds.size.x>1.2f&&!box.name.Contains("Roof")&&!box.name.Contains("Wall"))
                Visual(box.name+"_DecayedEdge",box.bounds.center.x,box.bounds.max.y-0.1f,box.bounds.size.x,0.2f,deco,sprite,mat,new Color(0.23f,0.23f,0.30f),1);
        }
        for(int i=0;i<5;i++)
        {
            float x=45+i*4;
            Visual("ExtendedBrokenColumn_"+i,x,4,0.7f,14,deco,sprite,mat,new Color(0.035f,0.045f,0.07f),-20);
            Visual("ExtendedDeadVine_"+i,x,10.5f,0.13f,3+(i%3),deco,sprite,mat,new Color(0.10f,0.075f,0.13f),1);
        }
        var last=terrain.GetComponentsInChildren<BoxCollider2D>().Where(b=>b.bounds.size.x>1.2f&&!b.name.Contains("Wall")&&!b.name.Contains("Roof")).OrderByDescending(b=>b.bounds.center.x).First();
        var item=UnityEngine.Object.FindAnyObjectByType<AmuletPickup>();
        if(item==null)
        {
            var diamond=Visual("Amulet04_Diamond",0,0,0.7f,0.7f,root,sprite,mat,new Color(0.95f,0.55f,1),3);
            diamond.transform.rotation=Quaternion.Euler(0,0,45);
            diamond.AddComponent<BoxCollider2D>().isTrigger=true;
            item=diamond.AddComponent<AmuletPickup>(); item.amuletNumber=4;item.nextScene="Level05";
        }
        item.name="Amulet04_Diamond";item.transform.position=new Vector3(last.bounds.center.x,last.bounds.max.y+1.3f,0);
        var nav=terrain.GetComponent<PlatformNavigation2D>();if(nav==null) nav=terrain.gameObject.AddComponent<PlatformNavigation2D>();
        var data=new SerializedObject(nav);data.FindProperty("surfaceRoot").objectReferenceValue=terrain;data.ApplyModifiedPropertiesWithoutUndo();
        var group=new GameObject("IntegratedEnemies").transform;group.SetParent(root);
        Enemy("Chomper",-1,0,group); Enemy("Spora",17,1,group); Enemy("Chomper",36,4.38f,group);
        var follow=Camera.main.GetComponent<Level01Camera>();float half=Camera.main.orthographicSize*Camera.main.aspect;
        follow.minimum=new Vector2(-8+half,-3);follow.maximum=new Vector2(63.1f-half,5);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Enemy(string kind,float x,float top,Transform parent)
    {
        var e=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IntegratedEnemies/"+kind+".prefab"));
        e.transform.SetParent(parent);var b=e.GetComponent<BoxCollider2D>();e.transform.position=new Vector3(x,top+b.size.y/2-b.offset.y+0.02f,0);
    }
    private static void Resize(GameObject go,Vector2 center,Vector2 size,Sprite sprite)
    {go.transform.position=center;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);}
    private static GameObject Visual(string name,float x,float y,float w,float h,Transform parent,Sprite sprite,Material mat,Color color,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,0);go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=mat;r.color=color;r.sortingOrder=order;return go;
    }
    private static void AddBlock(string name,float x,float top,float w,float h,Transform parent,Sprite sprite,Material mat)
    {
        var go=Visual(name,x,top-h/2,w,h,parent,sprite,mat,Color.white,0);var b=go.AddComponent<BoxCollider2D>();b.size=sprite.bounds.size;
        b.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/MovementNoFriction.physicsMaterial2D");
    }
}
