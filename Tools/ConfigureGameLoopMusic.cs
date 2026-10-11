if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var sceneNames=new[]{"PrismbloomLevel01","GloamthorneLevel02","Ashpetal Level 03","Boss"};
var loopNames=new[]{"VeinrootHollow","GloamthornCanopy","AshpetalForest","DreadcrownForest"};
var results=new System.Collections.Generic.List<string>();
for(int i=0;i<sceneNames.Length;i++){
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Audio/Music/Loops/"+loopNames[i]+"_Loop.wav");if(clip==null)throw new System.Exception("Music missing: "+loopNames[i]);
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameLoop/"+sceneNames[i]+".unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
 var roots=scene.GetRootGameObjects();var root=roots.FirstOrDefault(g=>g.name=="LevelMusic");if(root==null)root=new UnityEngine.GameObject("LevelMusic");
 var source=root.GetComponent<UnityEngine.AudioSource>();if(source==null)source=root.AddComponent<UnityEngine.AudioSource>();
 source.clip=clip;source.playOnAwake=true;source.loop=true;source.spatialBlend=0;source.volume=.6f;source.pitch=1;source.mute=false;source.priority=128;source.ignoreListenerPause=false;source.dopplerLevel=0;source.enabled=true;
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);results.Add(sceneNames[i]+": "+clip.name);
}
if(!string.IsNullOrEmpty(original))UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original,UnityEditor.SceneManagement.OpenSceneMode.Single);
return results.ToArray();
