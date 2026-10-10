// Unity Pipeline eval statement block. Only edits the new background asset and scene background.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/PrismbloomLevel01.unity") throw new System.InvalidOperationException("Open PrismbloomLevel01 first.");
var root = UnityEngine.GameObject.Find("Prismbloom Background");
if (root == null) throw new System.InvalidOperationException("Background root missing.");
var snapshots = new System.Collections.Generic.Dictionary<UnityEngine.Component, string>();
foreach (var entity in scene.GetRootGameObjects())
    if (entity != root)
        foreach (var component in entity.GetComponentsInChildren<UnityEngine.Component>(true))
            if (component != null) snapshots[component] = UnityEditor.EditorJsonUtility.ToJson(component);
const string path = "Assets/Art/Sprites/PrismbloomForest-Panorama.png";
UnityEditor.AssetDatabase.ImportAsset(path);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit = 32;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.mipmapEnabled = false;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize = 4096;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
var settings = new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(settings);
settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(settings);
importer.SaveAndReimport();
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
provider.InitSpriteEditorDataProvider();
var editor = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (editor == null) throw new System.InvalidOperationException("No sprite edit capability provider.");
var capability = editor.GetEditCapability();
foreach (var required in new [] { UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName, UnityEditor.U2D.Sprites.EEditCapability.EditPivot })
    if (!capability.HasCapability(required)) throw new System.InvalidOperationException("Sprite importer cannot " + required);
var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
if (texture.width != texture.height * 3) throw new System.InvalidOperationException("Panorama must be exactly 3:1.");
string[] names = { "Prismbloom_Entrance", "Prismbloom_Midgrove", "Prismbloom_Summit" };
var existing = provider.GetSpriteRects();
var rects = new UnityEditor.SpriteRect[3];
var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for (int i = 0; i < 3; ++i)
{
    var id = UnityEngine.GUID.Generate();
    foreach (var old in existing) if (old.name == names[i]) id = old.spriteID;
    rects[i] = new UnityEditor.SpriteRect { name = names[i], spriteID = id,
        rect = new UnityEngine.Rect(i * texture.height, 0, texture.height, texture.height),
        alignment = UnityEngine.SpriteAlignment.Center, pivot = new UnityEngine.Vector2(.5f, .5f) };
    pairs.Add(new UnityEditor.SpriteNameFileIdPair(names[i], id));
}
var nameProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (nameProvider == null) throw new System.InvalidOperationException("Sprite name/file ID provider missing.");
nameProvider.SetNameFileIdPairs(pairs);
provider.SetSpriteRects(rects);
provider.Apply();
importer.SaveAndReimport();
var sprites = new System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>();
foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
    if (asset is UnityEngine.Sprite sprite) sprites[sprite.name] = sprite;
if (sprites.Count != 3) throw new System.InvalidOperationException("Expected three imported sections.");
var material = root.GetComponentInChildren<UnityEngine.SpriteRenderer>().sharedMaterial;
while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
root.transform.position = UnityEngine.Vector3.zero;
root.transform.rotation = UnityEngine.Quaternion.identity;
root.transform.localScale = UnityEngine.Vector3.one;
var sections = new UnityEngine.SpriteRenderer[3];
for (int i = 0; i < 3; ++i)
{
    var section = new UnityEngine.GameObject(names[i]);
    section.transform.SetParent(root.transform, false);
    section.transform.localPosition = new UnityEngine.Vector3(-4.5f + i * 24, 9, 10);
    var renderer = section.AddComponent<UnityEngine.SpriteRenderer>();
    renderer.sprite = sprites[names[i]];
    section.transform.localScale = UnityEngine.Vector3.one * (24 / renderer.sprite.bounds.size.y);
    renderer.sharedMaterial = material;
    renderer.sortingOrder = -1000;
    sections[i] = renderer;
}
for (int i = 1; i < 3; ++i)
    if (UnityEngine.Mathf.Abs(sections[i-1].bounds.max.x - sections[i].bounds.min.x) > .0001f)
        throw new System.InvalidOperationException("Background join gap.");
var bounds = sections[0].bounds;
foreach (var renderer in sections) bounds.Encapsulate(renderer.bounds);
var follow = UnityEngine.Object.FindAnyObjectByType<Level01Camera>();
var camera = follow.GetComponent<UnityEngine.Camera>();
int checks = 0;
foreach (float aspect in new [] { 16f/9, camera.aspect, 2.4f })
    foreach (float x in new [] { follow.minimum.x, (follow.minimum.x + follow.maximum.x)/2, follow.maximum.x })
        foreach (float y in new [] { follow.minimum.y, (follow.minimum.y + follow.maximum.y)/2, follow.maximum.y })
        {
            var half = new UnityEngine.Vector3(camera.orthographicSize * aspect, camera.orthographicSize, 0);
            var center = new UnityEngine.Vector3(x, y, 10);
            if (!bounds.Contains(center-half) || !bounds.Contains(center+half))
                throw new System.InvalidOperationException("Camera can see beyond background.");
            ++checks;
        }
foreach (var snapshot in snapshots)
    if (UnityEditor.EditorJsonUtility.ToJson(snapshot.Key) != snapshot.Value)
        throw new System.InvalidOperationException("Gameplay component changed: " + snapshot.Key.name);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new { saved = scene.path, sections = 3, worldSize = bounds.size, cameraCoverageChecks = checks,
    unchangedGameplayComponents = snapshots.Count, seamlessJoins = true, fixedInWorld = true };
