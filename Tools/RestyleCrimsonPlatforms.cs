if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play mode before updating platforms.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Open Boss first.");
System.IO.Directory.CreateDirectory(".local-backups");UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,".local-backups/Boss-before-platform-restyle-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
const string assetPath="Assets/Art/Sprites/CrimsonArena-PaleStone.png";
UnityEditor.AssetDatabase.ImportAsset(assetPath,UnityEditor.ImportAssetOptions.ForceUpdate);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(assetPath);
importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;importer.spritePixelsPerUnit=256;
importer.mipmapEnabled=false;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.filterMode=UnityEngine.FilterMode.Point;
var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(assetPath);
var white=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Sprites/EnemyCharacters/CrimsonBoss-Unlit.mat");
UnityEngine.GameObject arena=null;BlightBoss boss=null;foreach(var root in scene.GetRootGameObjects()){if(root.name=="Boss Arena")arena=root;var b=root.GetComponentInChildren<BlightBoss>(true);if(b!=null)boss=b;}
if(arena==null||boss==null||sprite==null||material==null)throw new System.Exception("Arena assets missing.");
var old=new System.Collections.Generic.List<UnityEngine.GameObject>();foreach(UnityEngine.Transform child in arena.transform)if(child.name.StartsWith("CrimsonEscapeShelf_")||child.name.StartsWith("RootLedge_")||child.name.StartsWith("ScarletLandingEdge_"))old.Add(child.gameObject);foreach(var go in old)UnityEngine.Object.DestroyImmediate(go);
// Fixed authored positions: irregular appearance with tested traversal connections.
var positions=new[]{new UnityEngine.Vector2(-15.8f,-1.31f),new UnityEngine.Vector2(-11.2f,-.01f),new UnityEngine.Vector2(-6.6f,1.24f),new UnityEngine.Vector2(-2.9f,2.49f),new UnityEngine.Vector2(15.1f,-1.46f),new UnityEngine.Vector2(10.7f,-.16f),new UnityEngine.Vector2(7.3f,1.09f),new UnityEngine.Vector2(4.8f,2.49f),new UnityEngine.Vector2(1.3f,3.79f),new UnityEngine.Vector2(-16.2f,1f),new UnityEngine.Vector2(15.7f,2.1f)};
var widths=new[]{3.2f,3.5f,4.2f,3f,3.8f,4.4f,2.9f,3.7f,4.4f,2.6f,3.3f};
for(int i=0;i<positions.Length;i++){
 var platform=new UnityEngine.GameObject("CrimsonEscapeShelf_"+i);platform.transform.SetParent(arena.transform,false);platform.transform.position=new UnityEngine.Vector3(positions[i].x,positions[i].y,0);
 var visual=platform.AddComponent<UnityEngine.SpriteRenderer>();visual.sprite=sprite;visual.sharedMaterial=material;visual.drawMode=UnityEngine.SpriteDrawMode.Tiled;visual.size=new UnityEngine.Vector2(widths[i],.42f);visual.color=new UnityEngine.Color(.87f,.81f,.72f);visual.sortingOrder=2;
 var box=platform.AddComponent<UnityEngine.BoxCollider2D>();box.size=visual.size;box.usedByEffector=true;var effector=platform.AddComponent<UnityEngine.PlatformEffector2D>();effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=160;
 var edge=new UnityEngine.GameObject("PaleLandingEdge_"+i);edge.transform.SetParent(platform.transform,false);edge.transform.localPosition=new UnityEngine.Vector3(0,.21f,-.02f);var rim=edge.AddComponent<UnityEngine.SpriteRenderer>();rim.sprite=white;rim.sharedMaterial=material;rim.drawMode=UnityEngine.SpriteDrawMode.Tiled;rim.size=new UnityEngine.Vector2(widths[i],.035f);rim.color=new UnityEngine.Color(1f,.88f,.67f);rim.sortingOrder=3;
}
// The gem is a separate placeholder overlay, not part of the boss hitbox.
var so=new UnityEditor.SerializedObject(boss);var gem=(UnityEngine.SpriteRenderer)so.FindProperty("chestFragment").objectReferenceValue;
so.FindProperty("chestFragment").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();if(gem!=null)UnityEngine.Object.DestroyImmediate(gem.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
return "Saved 11 pale-stone shelves with varied widths and asymmetric positions; removed ChestAmulet gem overlay. Floor, boss AI, hitbox and attacks retained.";
