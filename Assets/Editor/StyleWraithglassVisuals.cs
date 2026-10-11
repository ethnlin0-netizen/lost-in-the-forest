using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StyleWraithglassVisuals
{
    private const string ScenePath="Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity";
    private const string Folder="Assets/Art/Sprites/WraithglassTerrain";
    private const string Prefix="WraithglassVisual_";

    [MenuItem("Tools/Lost In The Forest/Style Wraithglass Visuals")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("Temp/WraithglassVisuals");
        File.Copy(ScenePath,"Temp/WraithglassVisuals/Before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
        var components=new Dictionary<Component,string>();
        var transforms=new Dictionary<Transform,(Vector3,Quaternion,Vector3)>();
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(component is MonoBehaviour||component is Collider2D||component is Rigidbody2D||component is Camera)components[component]=EditorJsonUtility.ToJson(component);
            if(component is Transform t&&!t.name.StartsWith(Prefix))transforms[t]=(t.localPosition,t.localRotation,t.localScale);
        }
        AssetDatabase.Refresh();
        var wood=ImportSprite("CorruptedWood");
        var bricks=ImportSprite("RedCrackedBricks");
        var brickMaterial=Material("RedCrackedBricks",Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var spikeShader=Shader.Find("LostInTheForest/WraithglassSpikes");
        if(spikeShader==null||ShaderUtil.ShaderHasError(spikeShader))throw new InvalidOperationException("Wood thorn shader failed to compile.");
        var spikeMaterial=Material("WoodThorns",spikeShader);
        spikeMaterial.SetTexture("_MainTex",wood.texture);
        EditorUtility.SetDirty(spikeMaterial);
        var pool=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VeinrootHollow/VeinrootAcidPool.mat");
        var surface=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VeinrootHollow/VeinrootAcidSurface.mat");
        if(pool==null||surface==null)throw new InvalidOperationException("Veinroot acid materials are missing.");
        var route=GameObject.Find("Level05Route");var geometry=route.transform.Find("LevelGeometry");
        Physics2D.SyncTransforms();int platforms=0,spikes=0;
        foreach(var box in geometry.GetComponentsInChildren<BoxCollider2D>(true))
        {
            var old=box.GetComponent<SpriteRenderer>();if(old==null||box.isTrigger)continue;
            foreach(var child in box.transform.Cast<Transform>().Where(t=>t.name.StartsWith(Prefix)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var bounds=box.bounds;old.enabled=false;
            var go=new GameObject(Prefix+"Bricks");go.transform.SetParent(box.transform,false);go.transform.position=bounds.center;
            var scale=box.transform.lossyScale;go.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=bricks;renderer.sharedMaterial=brickMaterial;
            renderer.sortingLayerID=old.sortingLayerID;renderer.sortingOrder=old.sortingOrder;
            renderer.drawMode=SpriteDrawMode.Tiled;renderer.tileMode=SpriteTileMode.Continuous;renderer.size=bounds.size;
            platforms++;
        }
        foreach(var hazard in route.GetComponentsInChildren<Level02ThornHazard>(true))
            foreach(var renderer in hazard.GetComponentsInChildren<MeshRenderer>(true)){renderer.sharedMaterial=spikeMaterial;spikes++;}
        foreach(var renderer in route.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(renderer.name=="PurpleAcidWater"){renderer.sharedMaterial=pool;renderer.color=Color.white;}
            else if(renderer.name=="AcidSurface"){renderer.sharedMaterial=surface;renderer.color=Color.white;}
            else if(renderer.name.StartsWith("AcidRipple_")||renderer.name.EndsWith("_CorruptedEdge"))renderer.enabled=false;
        }
        foreach(var entry in transforms)
            if(entry.Key==null||entry.Key.localPosition!=entry.Value.Item1||entry.Key.localRotation!=entry.Value.Item2||entry.Key.localScale!=entry.Value.Item3)
                throw new InvalidOperationException("Existing transform changed: "+entry.Key?.name);
        foreach(var entry in components)
            if(entry.Key==null||EditorJsonUtility.ToJson(entry.Key)!=entry.Value)throw new InvalidOperationException("Gameplay component changed: "+entry.Key?.name);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved "+platforms+" red-cracked brick surfaces and "+spikes+" wood-textured thorns; preserved Veinroot acid and "+components.Count+" gameplay components.";
    }

    private static Sprite ImportSprite(string name)
    {
        string path=Folder+"/"+name+".png";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null)throw new InvalidOperationException("Missing texture: "+path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=texture.width/4f;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Repeat;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Material Material(string name,Shader shader)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}return material;
    }

    public static string Preview(float x,float y,string filename)
    {
        var camera=Camera.main;var position=camera.transform.position;var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
        var render=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position=new Vector3(x,y,-10);camera.aspect=1280f/720;camera.targetTexture=render;camera.Render();
            RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("Temp/"+filename,image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;camera.aspect=aspect;camera.transform.position=position;RenderTexture.active=active;
            UnityEngine.Object.DestroyImmediate(image);render.Release();UnityEngine.Object.DestroyImmediate(render);
        }
        return "Temp/"+filename;
    }
}
