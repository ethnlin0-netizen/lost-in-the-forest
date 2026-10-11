// Run as a Unity Editor eval statement block. Scene-specific rebuild; no shared prefab edits.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play mode before rebuilding the arena.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Open Boss first.");
System.IO.Directory.CreateDirectory(".local-backups");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,".local-backups/Boss-before-crimson-rebuild-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
UnityEditor.AssetDatabase.Refresh();
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if(shader==null)throw new System.Exception("URP 2D unlit shader missing.");
var material=new UnityEngine.Material(shader);
const string materialPath="Assets/Art/Sprites/EnemyCharacters/CrimsonBoss-Unlit.mat";
var existingMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if(existingMaterial==null){UnityEditor.AssetDatabase.CreateAsset(material,materialPath);}else{UnityEngine.Object.DestroyImmediate(material);material=existingMaterial;}
const string bgPath="Assets/Art/Sprites/DreadcrownForest-CrimsonArena.png";
UnityEditor.AssetDatabase.ImportAsset(bgPath,UnityEditor.ImportAssetOptions.ForceUpdate);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bgPath);
importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
importer.spritePixelsPerUnit=64;importer.mipmapEnabled=false;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
importer.filterMode=UnityEngine.FilterMode.Point;importer.maxTextureSize=2048;importer.SaveAndReimport();
var bgSprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(bgPath);
var stone=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/WraithglassTerrain/RedCrackedBricks.png");
var wood=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/WraithglassTerrain/CorruptedWood.png");
var white=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/PlaceholderSquare.png");
if(stone==null||wood==null)throw new System.Exception("Terrain sprites missing.");
BlightBoss boss=null;Movement player=null;UnityEngine.GameObject arena=null;GameAudioController audio=null;UnityEngine.Camera camera=null;
foreach(var root in scene.GetRootGameObjects()){
 if(root.name=="Boss Arena")arena=root;
 var b=root.GetComponentInChildren<BlightBoss>(true);if(b!=null)boss=b;
 var p=root.GetComponentInChildren<Movement>(true);if(p!=null)player=p;
 var a=root.GetComponentInChildren<GameAudioController>(true);if(a!=null)audio=a;
 var c=root.GetComponentInChildren<UnityEngine.Camera>(true);if(c!=null)camera=c;
}
if(boss==null||player==null||arena==null||camera==null||audio==null)throw new System.Exception("Required boss scene objects missing.");
if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(boss.gameObject))UnityEditor.PrefabUtility.UnpackPrefabInstance(UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(boss.gameObject),UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);
var bossSO=new UnityEditor.SerializedObject(boss);var visual=(UnityEngine.Transform)bossSO.FindProperty("visualRoot").objectReferenceValue;
var artwork=visual.Find("FusedBossArtwork");var chest=(UnityEngine.SpriteRenderer)bossSO.FindProperty("chestFragment").objectReferenceValue;
if(artwork==null)throw new System.Exception("Attach fused boss artwork first.");
var obsolete=new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach(UnityEngine.Transform child in visual)if(child!=artwork&&(chest==null||child!=chest.transform))obsolete.Add(child.gameObject);
foreach(var go in obsolete)UnityEngine.Object.DestroyImmediate(go);
bossSO.Update();bossSO.FindProperty("mouth").objectReferenceValue=null;bossSO.FindProperty("slamArm").objectReferenceValue=null;bossSO.FindProperty("mushroomCaps").objectReferenceValue=null;bossSO.FindProperty("corruptionVeins").arraySize=0;
bossSO.FindProperty("movementSpeed").floatValue=1.65f;bossSO.FindProperty("phaseTwoMovementSpeed").floatValue=2.15f;
bossSO.FindProperty("arenaLeft").floatValue=-13f;bossSO.FindProperty("arenaRight").floatValue=13f;
bossSO.FindProperty("detectionRange").floatValue=32f;bossSO.FindProperty("projectileRange").floatValue=25f;
bossSO.FindProperty("thornAttackEnabled").boolValue=true;bossSO.FindProperty("thornRange").floatValue=26f;
bossSO.FindProperty("slamDamage").floatValue=25f;bossSO.FindProperty("lungeDamage").floatValue=20f;bossSO.FindProperty("projectileDamage").floatValue=12f;
bossSO.ApplyModifiedPropertiesWithoutUndo();
var shape=boss.GetComponent<UnityEngine.BoxCollider2D>();shape.size=new UnityEngine.Vector2(3.0f,3.35f);shape.offset=new UnityEngine.Vector2(0,1.675f);
var bossBody=boss.GetComponent<UnityEngine.Rigidbody2D>();bossBody.constraints=UnityEngine.RigidbodyConstraints2D.FreezeRotation;bossBody.collisionDetectionMode=UnityEngine.CollisionDetectionMode2D.Continuous;
boss.transform.position=new UnityEngine.Vector3(6f,-2.48f,0f);
var fused=artwork.GetComponent<UnityEngine.SpriteRenderer>();fused.sharedMaterial=material;fused.sortingOrder=4;fused.flipX=true;
float height=3.8f;float factor=height/fused.sprite.bounds.size.y;artwork.localScale=UnityEngine.Vector3.one*factor;artwork.localRotation=UnityEngine.Quaternion.identity;
UnityEngine.Physics2D.SyncTransforms();artwork.position=new UnityEngine.Vector3(shape.bounds.center.x,shape.bounds.min.y+height/2f,0);
if(chest!=null){chest.transform.position=new UnityEngine.Vector3(fused.bounds.center.x+.35f,fused.bounds.min.y+1.83f,0);chest.sharedMaterial=material;}
var animator=boss.GetComponent<FusedBossAnimator>();if(animator==null)animator=boss.gameObject.AddComponent<FusedBossAnimator>();animator.artwork=artwork;
var healthSO=new UnityEditor.SerializedObject(boss.GetComponent<BlightBossHealth>());healthSO.FindProperty("deathDelay").floatValue=1.2f;healthSO.ApplyModifiedPropertiesWithoutUndo();
var sfx=boss.GetComponent<GameplaySfx>();if(sfx==null)sfx=boss.gameObject.AddComponent<GameplaySfx>();sfx.role=GameplaySfx.Role.Boss;sfx.stepDistance=1.35f;
// Remove the previous terrain/background and optional adds from this arena only.
var old=new System.Collections.Generic.List<UnityEngine.GameObject>();foreach(UnityEngine.Transform child in arena.transform)old.Add(child.gameObject);foreach(var go in old)UnityEngine.Object.DestroyImmediate(go);
foreach(var root in scene.GetRootGameObjects())foreach(var enemy in root.GetComponentsInChildren<BlightEnemy>(true))UnityEngine.Object.DestroyImmediate(enemy.gameObject);
UnityEngine.GameObject Make(string name,UnityEngine.Vector3 position,UnityEngine.Vector2 size,UnityEngine.Sprite sprite,UnityEngine.Color color,bool collision,bool oneWay){
 var go=new UnityEngine.GameObject(name);go.transform.SetParent(arena.transform,false);go.transform.position=position;
 if(sprite!=null){var r=go.AddComponent<UnityEngine.SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.drawMode=UnityEngine.SpriteDrawMode.Tiled;r.size=size;r.color=color;r.sortingOrder=1;}
 if(collision){var box=go.AddComponent<UnityEngine.BoxCollider2D>();box.size=size;if(oneWay){box.usedByEffector=true;var effector=go.AddComponent<UnityEngine.PlatformEffector2D>();effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=160f;}}
 return go;
}
var backdrop=Make("CrimsonForestBackdrop",new UnityEngine.Vector3(0,4,10),new UnityEngine.Vector2(50,28),bgSprite,UnityEngine.Color.white,false,false);
var bg=backdrop.GetComponent<UnityEngine.SpriteRenderer>();bg.drawMode=UnityEngine.SpriteDrawMode.Simple;backdrop.transform.localScale=new UnityEngine.Vector3(50/bgSprite.bounds.size.x,28/bgSprite.bounds.size.y,1);bg.sortingOrder=-100;bg.color=new UnityEngine.Color(.7f,.7f,.7f);
Make("CrimsonArenaGround",new UnityEngine.Vector3(0,-5.5f,0),new UnityEngine.Vector2(38,6),stone,new UnityEngine.Color(.6f,.24f,.29f),true,false);
Make("LeftBoundary",new UnityEngine.Vector3(-19.5f,2,0),new UnityEngine.Vector2(1,20),null,UnityEngine.Color.white,true,false);
Make("RightBoundary",new UnityEngine.Vector3(19.5f,2,0),new UnityEngine.Vector2(1,20),null,UnityEngine.Color.white,true,false);
// Open floor for pursuit and shockwaves; one-way side routes for deliberate escapes.
var positions=new[]{new UnityEngine.Vector3(-14,-1.31f,0),new UnityEngine.Vector3(-10.6f,.09f,0),new UnityEngine.Vector3(-6.9f,1.49f,0),new UnityEngine.Vector3(-3.2f,2.89f,0),new UnityEngine.Vector3(14,-1.31f,0),new UnityEngine.Vector3(10.6f,.09f,0),new UnityEngine.Vector3(6.9f,1.49f,0),new UnityEngine.Vector3(3.2f,2.89f,0),new UnityEngine.Vector3(0,4.29f,0)};
for(int i=0;i<positions.Length;i++){
 var platform=Make("CrimsonEscapeShelf_"+i,positions[i],new UnityEngine.Vector2(3.8f,.42f),stone,new UnityEngine.Color(.82f,.44f,.42f),true,true);
 var edge=Make("RootLedge_"+i,positions[i]+new UnityEngine.Vector3(0,.23f,-.02f),new UnityEngine.Vector2(3.95f,.2f),wood,new UnityEngine.Color(.78f,.16f,.25f),false,false);edge.GetComponent<UnityEngine.SpriteRenderer>().sortingOrder=2;
 Make("ScarletLandingEdge_"+i,positions[i]+new UnityEngine.Vector3(0,.21f,-.03f),new UnityEngine.Vector2(3.8f,.045f),white,new UnityEngine.Color(1f,.34f,.42f),false,false).GetComponent<UnityEngine.SpriteRenderer>().sortingOrder=3;
}
Make("ScarletFloorRoots",new UnityEngine.Vector3(0,-2.45f,-.01f),new UnityEngine.Vector2(38,.16f),wood,new UnityEngine.Color(.9f,.2f,.3f),false,false).GetComponent<UnityEngine.SpriteRenderer>().sortingOrder=2;
Make("FloorLandingEdge",new UnityEngine.Vector3(0,-2.48f,-.03f),new UnityEngine.Vector2(38,.045f),white,new UnityEngine.Color(.9f,.25f,.32f),false,false).GetComponent<UnityEngine.SpriteRenderer>().sortingOrder=3;
player.transform.position=new UnityEngine.Vector3(-15.5f,-1.35f,0);player.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity=UnityEngine.Vector2.zero;
camera.orthographic=true;camera.orthographicSize=11f;camera.transform.position=new UnityEngine.Vector3(0,2.8f,-10);camera.backgroundColor=new UnityEngine.Color(.1f,.01f,.025f);
// Clone the cue table for this arena; original scene catalogs keep their cues.
const string catalogPath="Assets/Audio/SFX/CrimsonBoss/CrimsonBossCatalog.asset";
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(catalogPath);
if(catalog==null){catalog=UnityEngine.Object.Instantiate(audio.catalog);catalog.name="CrimsonBossCatalog";UnityEditor.AssetDatabase.CreateAsset(catalog,catalogPath);}
var cues=new System.Collections.Generic.List<GameAudioCatalog.Cue>(catalog.cues);
foreach(var sound in new[]{GameSound.BossSlamWarn,GameSound.BossLungeWarn,GameSound.BossSporeWarn,GameSound.BossSlam,GameSound.BossShockwave,GameSound.BossLunge,GameSound.BossVolley,GameSound.BossHit,GameSound.BossPhase,GameSound.BossDeath,GameSound.BossThornWarn,GameSound.BossThorns}){
 string path="Assets/Audio/SFX/CrimsonBoss/"+sound+".wav";var ai=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);var settings=ai.defaultSampleSettings;settings.loadType=UnityEngine.AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=UnityEngine.AudioCompressionFormat.ADPCM;ai.defaultSampleSettings=settings;ai.forceToMono=true;ai.SaveAndReimport();
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(path);if(clip==null)throw new System.Exception("Missing boss SFX: "+sound);var cue=cues.Find(c=>c.sound==sound);if(cue==null){cue=new GameAudioCatalog.Cue{sound=sound};cues.Add(cue);}cue.clips=new[]{clip};cue.volume=.6f;cue.minimumInterval=.08f;cue.maximumConcurrent=2;cue.pitch=new UnityEngine.Vector2(.97f,1.03f);
}
catalog.cues=cues.ToArray();UnityEditor.EditorUtility.SetDirty(catalog);audio.catalog=catalog;
audio.music.clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Audio/Music/Originals/V2/Boss_Dreadcrown_Crowncrusher.wav");audio.music.loop=true;audio.music.playOnAwake=true;audio.musicVolume=.5f;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
return "Rebuilt crimson arena: 9 escape platforms, one floor, no transition ice or ordinary enemy adds. Fused boss has 4 attacks, procedural poses, original sound cues and boss music. Player controls/health/reward retained.";
