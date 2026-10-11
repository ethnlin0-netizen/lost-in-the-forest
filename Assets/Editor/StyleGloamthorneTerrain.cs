using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StyleGloamthorneTerrain
{
    public static string FixLevel03Transition()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var destination=EditorBuildSettings.scenes.FirstOrDefault(s=>s.enabled&&File.Exists(s.path)&&Path.GetFileNameWithoutExtension(s.path)=="Ashpetal Level 03");
        if(destination==null)throw new InvalidOperationException("Add Ashpetal Level 03 to the build list first.");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        int changed=0;
        foreach(var path in new[]{ScenePath,"Assets/Scenes/Level02.unity"})
        {
            if(!File.Exists(path))continue;
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var root in scene.GetRootGameObjects())foreach(var pickup in root.GetComponentsInChildren<AmuletPickup>(true))
                if(pickup.amuletNumber==2){pickup.nextScene=Path.GetFileNameWithoutExtension(destination.path);EditorUtility.SetDirty(pickup);changed++;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        if(!string.IsNullOrEmpty(original)&&File.Exists(original))EditorSceneManager.OpenScene(original);
        return "Updated "+changed+" crystals to "+destination.path;
    }
    private const string ScenePath="Assets/Scenes/GameLoop/GloamthorneLevel02.unity";
    private const string DirectoryPath="Assets/Art/Sprites/GloamthorneTerrain";

    [MenuItem("Tools/Lost In The Forest/Style Gloamthorne Terrain")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
        System.IO.Directory.CreateDirectory("Assets/Scenes/Backups");
        if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/Backups/Gloamthorne-before-textures-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new IOException("Backup failed.");
        var snapshots=new Dictionary<Component,string>();
        foreach(var root in scene.GetRootGameObjects())
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component is Collider2D || component is MonoBehaviour)
                    snapshots[component]=EditorJsonUtility.ToJson(component);
        AssetDatabase.Refresh();
        var leaves=Import("BlueVioletLeaves.png",false);
        var thorns=Import("SilverThornVines.png",true);
        string materialPath=DirectoryPath+"/GloamthorneTerrain.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,materialPath);}
        Physics2D.SyncTransforms();
        var geometry=GameObject.Find("Level02Route/LevelGeometry").transform;
        int platforms=0,hazards=0;
        foreach(var box in geometry.GetComponentsInChildren<BoxCollider2D>(true))
        {
            var original=box.GetComponent<SpriteRenderer>();if(original==null)continue;
            RemoveVisual(box.transform);
            original.enabled=false;
            Visual(box.transform,box.bounds,leaves,material,original.sortingLayerID,original.sortingOrder,false);
            platforms++;
        }
        foreach(var hazard in GameObject.Find("Level02Route").GetComponentsInChildren<Level02ThornHazard>(true))
        {
            RemoveVisual(hazard.transform);
            foreach(var renderer in hazard.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
            var box=hazard.GetComponent<BoxCollider2D>();
            Visual(hazard.transform,box.bounds,thorns,material,0,2,hazard.name.Contains("Ceiling"));
            hazards++;
        }
        foreach(var snapshot in snapshots)
            if(EditorJsonUtility.ToJson(snapshot.Key)!=snapshot.Value)
                throw new InvalidOperationException("Gameplay component changed: "+snapshot.Key.name);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved "+scene.path+"; leafy surfaces="+platforms+"; thorn strips="+hazards+"; unchanged gameplay components="+snapshots.Count;
    }

    private static Sprite Import(string filename,bool thorns)
    {
        var path=DirectoryPath+"/"+filename;
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        // Leaf tile covers four world units; thorn art fits the one-unit hazard depth.
        importer.spritePixelsPerUnit=thorns?texture.height:texture.width/4f;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.wrapMode=TextureWrapMode.Repeat;importer.alphaIsTransparency=true;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void RemoveVisual(Transform parent)
    {
        var old=parent.Find("GloamthorneTexture");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
    }

    private static void Visual(Transform parent,Bounds bounds,Sprite sprite,Material material,int layer,int order,bool flip)
    {
        var go=new GameObject("GloamthorneTexture");go.transform.SetParent(parent,false);
        go.transform.position=bounds.center;go.transform.rotation=Quaternion.identity;
        var scale=parent.lossyScale;go.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
        renderer.sortingLayerID=layer;renderer.sortingOrder=order;
        renderer.drawMode=SpriteDrawMode.Tiled;renderer.tileMode=SpriteTileMode.Continuous;
        renderer.size=new Vector2(bounds.size.x,bounds.size.y);renderer.flipY=flip;
    }

    public static string Preview()
    {
        var camera=Camera.main;var position=camera.transform.position;
        var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
        var render=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position=new Vector3(22,5,-10);camera.aspect=1280f/720;camera.targetTexture=render;camera.Render();
            RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("Temp");File.WriteAllBytes("Temp/Gloamthorne-textures-preview.png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;camera.aspect=aspect;camera.transform.position=position;RenderTexture.active=active;
            UnityEngine.Object.DestroyImmediate(image);render.Release();UnityEngine.Object.DestroyImmediate(render);
        }
        return "Temp/Gloamthorne-textures-preview.png";
    }
}
