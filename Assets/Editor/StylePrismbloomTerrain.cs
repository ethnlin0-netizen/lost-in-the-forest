using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StylePrismbloomTerrain
{
    [MenuItem("Tools/Lost In The Forest/Style Prismbloom Terrain")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var title=EditorSceneManager.OpenScene("Assets/Scenes/TitleScreen.unity");
        foreach(var menu in UnityEngine.Object.FindObjectsByType<TitleMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {var data=new SerializedObject(menu);data.FindProperty("firstBiomeScene").stringValue="PrismbloomLevel01";data.ApplyModifiedPropertiesWithoutUndo();}
        EditorSceneManager.MarkSceneDirty(title);EditorSceneManager.SaveScene(title);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrismbloomLevel01.unity");
        Directory.CreateDirectory("Assets/Scenes/Backups");
        if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Prismbloom-before-textures-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Backup failed.");
        var stone=Texture("PrismbloomStone",false);var moss=Texture("PrismbloomMoss",true);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Sprites/PrismbloomTerrain/PrismbloomTerrain.mat");
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material,"Assets/Art/Sprites/PrismbloomTerrain/PrismbloomTerrain.mat");
        }
        var geometry=GameObject.Find("Level01Route/LevelGeometry").transform;
        Physics2D.SyncTransforms();
        foreach(var box in geometry.GetComponentsInChildren<BoxCollider2D>())
        {
            var old=box.GetComponent<SpriteRenderer>();if(old==null)continue;
            foreach(var child in box.GetComponentsInChildren<Transform>().Where(t=>t!=box.transform&&t.name.StartsWith("PrismbloomTexture_")).ToArray())if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);
            var bounds=box.bounds;old.enabled=false;
            Surface("PrismbloomTexture_Stone",box.transform,bounds.center,new Vector2(bounds.size.x,bounds.size.y),stone,material,old.sortingLayerID,old.sortingOrder);
            if(!box.name.Contains("Ceiling")&&!box.name.Contains("Enclosure")&&!box.name.Contains("Overhang")&&!box.name.Contains("Hanging"))
                Surface("PrismbloomTexture_Moss",box.transform,new Vector3(bounds.center.x,bounds.max.y-0.18f,bounds.center.z),new Vector2(bounds.size.x,0.36f),moss,material,old.sortingLayerID,old.sortingOrder+1);
        }
        var scenes=EditorBuildSettings.scenes.ToList();var entry=scenes.FirstOrDefault(s=>s.path==scene.path);
        if(entry==null)scenes.Add(new EditorBuildSettingsScene(scene.path,true));else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Surface(string name,Transform parent,Vector3 position,Vector2 size,Sprite sprite,Material mat,int layer,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;
        if(name.EndsWith("Moss"))go.transform.position+=new Vector3(0,0,-0.01f);
        var scale=parent.lossyScale;go.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=mat;r.color=Color.white;
        r.sortingLayerID=layer;r.sortingOrder=order;r.drawMode=SpriteDrawMode.Tiled;r.tileMode=SpriteTileMode.Continuous;r.size=size;
    }
    private static Sprite Texture(string name,bool moss)
    {
        string dir="Assets/Art/Sprites/PrismbloomTerrain";Directory.CreateDirectory(dir);string path=dir+"/"+name+".png";
        int height=moss?12:32;
        var texture=new Texture2D(32,height,TextureFormat.RGBA32,false);
        Color[] stone={new Color32(38,29,70,255),new Color32(51,42,86,255),new Color32(66,53,107,255),new Color32(82,70,127,255),new Color32(105,87,147,255)};
        for(int y=0;y<height;y++)for(int x=0;x<32;x++)
        {
            int row=y/8;int shifted=(x+(row%2)*8)%32;int hash=((shifted/8)*13+row*7)%4;
            Color color=stone[hash+1];
            if(y%8==0||shifted%16==0)color=stone[0];
            else if(y%8==7)color=stone[Mathf.Min(hash+2,4)];
            else if((x*17+y*29)%47<3)color=stone[0];
            if(moss)
            {
                int edge=3+(x*7%3);color=y<edge?stone[(x+y)%3]:new Color32(32,91,115,255);
                if(y>=edge+2)color=((x/3+y/2)%3==0)?new Color32(98,181,193,255):new Color32(51,127,151,255);
                if(y>8&&((x==5||x==6)||(x==23||x==24)))color=new Color32(231,132,215,255);
                if(y==10&&(x==5||x==23))color=new Color32(255,214,226,255);
            }
            texture.SetPixel(x,y,color);
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
