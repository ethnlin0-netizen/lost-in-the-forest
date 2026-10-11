using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StyleLevel03Forest
{
    public static string SealRightBoundary()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        var wall=GameObject.Find("Level03Route/LevelGeometry/RightWall").transform;
        var stone=wall.Find("ForestTexture_Stone").GetComponent<SpriteRenderer>();
        var camera=Camera.main;var follow=camera.GetComponent<Level01Camera>();
        var existing=wall.Find("ForestBoundaryFill");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var bounds=stone.bounds;
        float left=bounds.min.x;
        float right=Mathf.Max(bounds.max.x,follow.maximum.x+camera.orthographicSize*Mathf.Max(camera.aspect,2.4f)+1);
        float bottom=Mathf.Min(bounds.min.y,follow.minimum.y-camera.orthographicSize-1);
        float top=Mathf.Max(bounds.max.y,follow.maximum.y+camera.orthographicSize+1);
        Surface("ForestBoundaryFill",wall,new Vector3((left+right)/2,(bottom+top)/2,bounds.center.z),new Vector2(right-left,top-bottom),stone.sprite,stone.sharedMaterial,stone.sortingLayerID,stone.sortingOrder);
        var scene=wall.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Extended visible stone wall; collision unchanged.";
    }
    public static string ApplyCleanBackground()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");
        var art=GameObject.Find("Level03 Forest Background").GetComponent<SpriteRenderer>();
        var oldSize=art.bounds.size;
        const string path="Assets/Art/Sprites/Level03Forest-Background-Clean.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        art.sprite=sprite;art.transform.localScale=Vector3.one*Mathf.Max(oldSize.x/sprite.bounds.size.x,oldSize.y/sprite.bounds.size.y);
        SealRightBoundary();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return sprite.texture.width+"x"+sprite.texture.height;
    }
    [MenuItem("Tools/Lost In The Forest/Style Level 03 Forest")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");
        Directory.CreateDirectory("Assets/Scenes/Backups");
        if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Level03-before-forest-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Backup failed.");
        const string directory="Assets/Art/Sprites/Level03Terrain";
        Directory.CreateDirectory(directory);AssetDatabase.Refresh();
        var material=AssetDatabase.LoadAssetAtPath<Material>(directory+"/Level03Terrain.mat");
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,directory+"/Level03Terrain.mat");}
        var stone=Texture(directory,"BurgundyStone",false);var leaves=Texture(directory,"CrimsonLeafEdge",true);
        var geometry=GameObject.Find("Level03Route/LevelGeometry").transform;
        Physics2D.SyncTransforms();
        foreach(var box in geometry.GetComponentsInChildren<BoxCollider2D>())
        {
            var renderer=box.GetComponent<SpriteRenderer>();if(renderer==null)continue;
            foreach(var t in box.GetComponentsInChildren<Transform>().Where(t=>t!=box.transform&&t.name.StartsWith("ForestTexture_")).ToArray())if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
            var bounds=box.bounds;renderer.enabled=false;
            Surface("ForestTexture_Stone",box.transform,bounds.center,new Vector2(bounds.size.x,bounds.size.y),stone,material,renderer.sortingLayerID,renderer.sortingOrder);
            if(!box.name.Contains("Ceiling")&&!box.name.Contains("Wall"))
                Surface("ForestTexture_Leaves",box.transform,new Vector3(bounds.center.x,bounds.max.y-0.18f,bounds.center.z-0.01f),new Vector2(bounds.size.x,0.36f),leaves,material,renderer.sortingLayerID,renderer.sortingOrder+1);
        }
        // Original blue scenery is superseded by the supplied forest artwork.
        var oldScenery=GameObject.Find("Level03Route/DecorativeStonework_NoColliders");
        if(oldScenery!=null)foreach(var renderer in oldScenery.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
        const string imagePath="Assets/Art/Sprites/Level03Forest-Background-Clean.png";
        AssetDatabase.ImportAsset(imagePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.SaveAndReimport();var backgroundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        var existing=GameObject.Find("Level03 Forest Background");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
        var background=new GameObject("Level03 Forest Background");var art=background.AddComponent<SpriteRenderer>();
        art.sprite=backgroundSprite;art.sharedMaterial=material;art.sortingOrder=-1000;
        var follow=Camera.main.GetComponent<Level01Camera>();
        float halfWidth=Camera.main.orthographicSize*Mathf.Max(Camera.main.aspect,2.4f);
        float width=follow.maximum.x-follow.minimum.x+halfWidth*2+1;
        float height=follow.maximum.y-follow.minimum.y+Camera.main.orthographicSize*2+1;
        float scale=Mathf.Max(width/backgroundSprite.bounds.size.x,height/backgroundSprite.bounds.size.y);
        background.transform.localScale=Vector3.one*scale;
        background.transform.position=new Vector3((follow.minimum.x+follow.maximum.x)/2,(follow.minimum.y+follow.maximum.y)/2,10);
        SealRightBoundary();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Surface(string name,Transform parent,Vector3 center,Vector2 size,Sprite sprite,Material mat,int layer,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=center;var scale=parent.lossyScale;
        go.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=mat;renderer.sortingLayerID=layer;renderer.sortingOrder=order;
        renderer.drawMode=SpriteDrawMode.Tiled;renderer.tileMode=SpriteTileMode.Continuous;renderer.size=size;
    }
    private static Sprite Texture(string directory,string name,bool foliage)
    {
        string path=directory+"/"+name+".png";int height=foliage?12:32;
        var texture=new Texture2D(32,height,TextureFormat.RGBA32,false);
        Color32[] stone={new Color32(39,27,43,255),new Color32(57,39,57,255),new Color32(76,49,70,255),new Color32(95,62,84,255),new Color32(119,81,99,255)};
        for(int y=0;y<height;y++)for(int x=0;x<32;x++)
        {
            int row=y/8;int shifted=(x+(row%2)*8)%32;int hash=((shifted/8)*13+row*7)%4;Color32 color=stone[hash+1];
            if(y%8==0||shifted%16==0)color=stone[0];else if(y%8==7)color=stone[Mathf.Min(hash+2,4)];
            else if((x*17+y*29)%47<3)color=stone[0];
            if(foliage)
            {
                int edge=3+(x*7%3);color=y<edge?stone[(x+y)%3]:new Color32(72,26,45,255);
                if(y>=edge+2)color=((x/3+y/2)%3==0)?new Color32(164,72,99,255):new Color32(109,39,65,255);
                if(y>8&&(x==5||x==6||x==23||x==24))color=new Color32(202,124,148,255);
                if(y==10&&(x==5||x==23))color=new Color32(237,183,190,255);
            }
            texture.SetPixel(x,y,color);
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
