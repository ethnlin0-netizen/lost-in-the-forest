if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play mode first.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Open the updated Boss scene first.");
System.IO.Directory.CreateDirectory(".local-backups");UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,".local-backups/Boss-before-ending-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
BlightBossHealth boss=null;Movement player=null;StoryEnding ending=null;
foreach(var root in scene.GetRootGameObjects()){var b=root.GetComponentInChildren<BlightBossHealth>(true);if(b!=null)boss=b;var p=root.GetComponentInChildren<Movement>(true);if(p!=null)player=p;var e=root.GetComponentInChildren<StoryEnding>(true);if(e!=null)ending=e;}
if(boss==null||player==null)throw new System.Exception("Boss/player missing.");
if(ending==null){
 var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(StoryRunSession.EndingScenePath);
 try{
  UnityEngine.GameObject source=null;foreach(var root in preview.GetRootGameObjects())if(root.GetComponentInChildren<StoryEnding>(true)!=null)source=root;
  if(source==null)throw new System.Exception("Teammates' story hierarchy missing.");
  var clone=UnityEngine.Object.Instantiate(source);clone.name=source.name;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);ending=clone.GetComponentInChildren<StoryEnding>(true);
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
}
ending.boss=boss;ending.player=player;
var so=new UnityEditor.SerializedObject(ending);so.FindProperty("waitForCrystal").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
if(ending.dialogue==null||ending.dialogue.canvas==null||ending.dialogue.sleepingTK==null||ending.dialogue.celebratingLenny==null)throw new System.Exception("Cutscene UI references missing.");
ending.dialogue.Hide();UnityEditor.EditorUtility.SetDirty(ending);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Connected unchanged teammates' ending UI to the updated boss/player. Ending waits for victory crystal collection.";
