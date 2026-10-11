if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception("Preserve the current unsaved scene first.");
var names=new[]{"PrismbloomLevel01","GloamthorneLevel02","Ashpetal Level 03","Boss","TitleScreen"};
var ambienceNames=new[]{"Prismbloom","Gloamthorne","Ashpetal","Boss"};
var ambienceVolumes=new[]{.1f,.085f,.075f,.06f};
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioCatalog>("Assets/Audio/SFX/GameAudioCatalog.asset");
var results=new System.Collections.Generic.List<object>();
for(int i=0;i<names.Length;i++){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameLoop/"+names[i]+".unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t=>t.gameObject).ToArray();
 var root=all.FirstOrDefault(g=>g.name=="GameAudio");if(root==null)root=new UnityEngine.GameObject("GameAudio");
 var controller=root.GetComponent<GameAudioController>();if(controller==null)controller=root.AddComponent<GameAudioController>();
 controller.catalog=catalog;controller.effectsVolume=.8f;controller.musicVolume=.6f;
 controller.music=all.Where(g=>g.name=="LevelMusic").Select(g=>g.GetComponent<UnityEngine.AudioSource>()).FirstOrDefault();
 if(i<4){
  var bed=all.FirstOrDefault(g=>g.name=="ForestAmbience");if(bed==null)bed=new UnityEngine.GameObject("ForestAmbience");
  var source=bed.GetComponent<UnityEngine.AudioSource>();if(source==null)source=bed.AddComponent<UnityEngine.AudioSource>();
  source.clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Audio/Ambience/"+ambienceNames[i]+"_Ambience.wav");
  source.loop=true;source.playOnAwake=true;source.spatialBlend=0;source.dopplerLevel=0;source.pitch=1;source.priority=220;source.ignoreListenerPause=false;source.volume=ambienceVolumes[i];
  controller.ambience=source;controller.ambienceVolume=ambienceVolumes[i];
 }
 int actors=0;
 foreach(var go in all){
  var movement=go.GetComponent<Movement>();var enemy=go.GetComponent<BlightEnemy>();var boss=go.GetComponent<BlightBoss>();var pet=go.GetComponent<BloomPetFollower>();var crystal=go.GetComponent<BossCrystalExit>();
  if(movement==null&&enemy==null&&boss==null&&pet==null&&crystal==null)continue;
  var sfx=go.GetComponent<GameplaySfx>();if(sfx==null)sfx=go.AddComponent<GameplaySfx>();
  sfx.role=movement!=null?GameplaySfx.Role.Player:boss!=null?GameplaySfx.Role.Boss:enemy!=null?(GameplaySfx.Role)((int)enemy.Kind+1):crystal!=null?GameplaySfx.Role.Crystal:
   go.name.Contains("Whisperseed")?GameplaySfx.Role.Whisperseed:go.name.Contains("Dryad")?GameplaySfx.Role.Dryad:GameplaySfx.Role.Petalborn;
  sfx.stepDistance=sfx.role==GameplaySfx.Role.Boss?2f:sfx.role==GameplaySfx.Role.Collusus?1.2f:1.7f;
  if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(sfx))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(sfx);
  actors++;
 }
 foreach(var button in all.Select(g=>g.GetComponent<UnityEngine.UI.Button>()).Where(b=>b!=null))if(button.GetComponent<MenuButtonSfx>()==null)button.gameObject.AddComponent<MenuButtonSfx>();
 foreach(var label in all.Select(g=>g.GetComponent<TMPro.TMP_Text>()).Where(t=>t!=null&&t.text.Contains("Caleb Kimondo"))){
  label.text=TitleMenu.CreditsText;label.enableAutoSizing=true;label.fontSizeMin=10;label.ForceMeshUpdate(true);UnityEditor.EditorUtility.SetDirty(label);
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 results.Add(new{scene=names[i],actors,music=controller.music!=null,ambience=controller.ambience!=null});
}
UnityEditor.AssetDatabase.SaveAssets();
if(!string.IsNullOrEmpty(original))UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original,UnityEditor.SceneManagement.OpenSceneMode.Single);
return results.ToArray();
