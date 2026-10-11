// Run this statement block through the Unity CLI eval command in Edit mode.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play mode before styling the pickups.");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
    throw new System.IO.IOException("Open scenes could not be saved.");

const string texturePath="Assets/Art/Sprites/AmuletShards/AmuletShard.png";
UnityEditor.AssetDatabase.Refresh();
var importer=UnityEditor.AssetImporter.GetAtPath(texturePath) as UnityEditor.TextureImporter;
if(importer==null)throw new System.Exception("Missing shard texture importer.");
importer.textureType=UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit=1010f;
importer.filterMode=UnityEngine.FilterMode.Point;
importer.mipmapEnabled=false;
importer.alphaIsTransparency=true;
importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize=2048;
var settings=new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(settings);
settings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(settings);
importer.SaveAndReimport();

var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
if(provider==null)throw new System.Exception("Sprite data provider unavailable.");
provider.InitSpriteEditorDataProvider();
var capability=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
var required=UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite
    |UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect
    |UnityEditor.U2D.Sprites.EEditCapability.EditPivot
    |UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName;
if(capability==null||!capability.GetEditCapability().HasCapability(required))
    throw new System.Exception("Importer does not support shard sprite slicing.");
var existing=provider.GetSpriteRects().FirstOrDefault(r=>r.name=="AmuletShard");
var rect=new UnityEditor.SpriteRect
{
    name="AmuletShard",rect=new UnityEngine.Rect(128,136,1058,1010),
    alignment=(int)UnityEngine.SpriteAlignment.Center,pivot=new UnityEngine.Vector2(.5f,.5f),
    spriteID=existing!=null?existing.spriteID:UnityEngine.GUID.Generate()
};
var fileIds=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if(fileIds==null)throw new System.Exception("Sprite name/file ID provider unavailable.");
provider.SetSpriteRects(new[]{rect});
fileIds.SetNameFileIdPairs(new[]{new UnityEditor.SpriteNameFileIdPair(rect.name,rect.spriteID)});
provider.Apply();
importer.SaveAndReimport();
var sprite=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<UnityEngine.Sprite>().Single(s=>s.name=="AmuletShard");
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if(shader==null)throw new System.Exception("URP sprite shader missing.");
const string materialPath="Assets/Art/Sprites/AmuletShards/AmuletShard.mat";
var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if(material==null)
{
    material=new UnityEngine.Material(shader);
    UnityEditor.AssetDatabase.CreateAsset(material,materialPath);
}

var names=new[]{"PrismbloomLevel01","GloamthorneLevel02","Ashpetal Level 03","Veinroot Level04","Wraithglass Barrens Level 05","Boss","PrismbloomLevel01Story","BossStory"};
var levelNumbers=new[]{1,2,3,4,5,6,1,6};
var colorNames=new[]{"red","orange","yellow","green","blue","purple"};
var colors=new[]{
    new UnityEngine.Color(1f,.16f,.22f),new UnityEngine.Color(1f,.5f,.10f),
    new UnityEngine.Color(1f,.9f,.15f),new UnityEngine.Color(.2f,1f,.4f),
    new UnityEngine.Color(.2f,.55f,1f),new UnityEngine.Color(.78f,.3f,1f)
};
string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string backup="Temp/AmuletShards/"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
System.IO.Directory.CreateDirectory(backup);
var report=new System.Collections.Generic.List<string>();
try
{
    for(int i=0;i<names.Length;i++)
    {
        string path="Assets/Scenes/GameLoop/"+names[i]+".unity";
        System.IO.File.Copy(path,backup+"/"+names[i]+".unity");
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        var roots=scene.GetRootGameObjects();
        var protectedComponents=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Component>(true))
            .Where(c=>(c is UnityEngine.MonoBehaviour&&!(c is AmuletShardFloat))||c is UnityEngine.Collider2D||c is UnityEngine.Rigidbody2D||c is UnityEngine.Camera)
            .ToDictionary(c=>c,c=>UnityEditor.EditorJsonUtility.ToJson(c));
        var transforms=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true))
            .Where(t=>t.name!="AmuletShardVisual")
            .ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf));
        int colorIndex=levelNumbers[i]-1;
        UnityEngine.GameObject pickup=levelNumbers[i]<6
            ?roots.SelectMany(r=>r.GetComponentsInChildren<AmuletPickup>(true)).Single(p=>p.amuletNumber==levelNumbers[i]).gameObject
            :roots.SelectMany(r=>r.GetComponentsInChildren<BossCrystalExit>(true)).Single().gameObject;
        var old=pickup.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true).Single(r=>r.name!="AmuletShardVisual");
        var visual=pickup.transform.Find("AmuletShardVisual");
        if(visual==null)
        {
            visual=new UnityEngine.GameObject("AmuletShardVisual").transform;
            visual.SetParent(pickup.transform,false);
        }
        visual.position=old.bounds.center;
        visual.rotation=UnityEngine.Quaternion.identity;
        var scale=pickup.transform.lossyScale;
        float size=UnityEngine.Mathf.Max(old.bounds.size.x,old.bounds.size.y);
        visual.localScale=new UnityEngine.Vector3(size/scale.x,size/scale.y,1f/scale.z);
        var renderer=visual.GetComponent<UnityEngine.SpriteRenderer>();
        if(renderer==null)renderer=visual.gameObject.AddComponent<UnityEngine.SpriteRenderer>();
        renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.color=colors[colorIndex];
        renderer.sortingLayerID=old.sortingLayerID;renderer.sortingOrder=old.sortingOrder;
        if(visual.GetComponent<AmuletShardFloat>()==null)visual.gameObject.AddComponent<AmuletShardFloat>();
        old.enabled=false;
        foreach(var entry in transforms)
            if(entry.Key.localPosition!=entry.Value.Item1||entry.Key.localRotation!=entry.Value.Item2
                ||entry.Key.localScale!=entry.Value.Item3||entry.Key.gameObject.activeSelf!=entry.Value.Item4)
                throw new System.Exception("Existing transform or active state changed: "+entry.Key.name);
        foreach(var entry in protectedComponents)
            if(UnityEditor.EditorJsonUtility.ToJson(entry.Key)!=entry.Value)
                throw new System.Exception("Existing gameplay component changed: "+entry.Key.name);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        report.Add(names[i]+": "+colorNames[colorIndex]+" shard; preserved "+protectedComponents.Count+" gameplay components");
    }
    UnityEditor.AssetDatabase.SaveAssets();
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
}
System.IO.File.WriteAllLines("Temp/AmuletShards/StylingResults.txt",report);
return string.Join("; ",report);
