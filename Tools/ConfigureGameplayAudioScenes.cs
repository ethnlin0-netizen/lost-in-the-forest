// Repair missing scene audio infrastructure without changing existing music or ambience.
// Run with Unity CLI eval_file in Edit mode; safe to run again.
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Preserve unsaved scenes first.");
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioCatalog>("Assets/Audio/SFX/GameAudioCatalog.asset");
if(catalog==null)throw new System.Exception("Missing existing audio catalog.");
var cues=catalog.cues.ToList();
void AddCue(GameSound sound,GameSound source,float volume) {
    if(cues.Any(c=>c.sound==sound))return;
    var original=catalog.Find(source);
    cues.Add(new GameAudioCatalog.Cue{sound=sound,clips=original.clips.ToArray(),volume=volume,pitch=UnityEngine.Vector2.one,minimumInterval=.5f,priority=45,maximumConcurrent=1});
}
AddCue(GameSound.AmuletCollect,GameSound.CrystalTouch,.4f);
AddCue(GameSound.LevelRestart,GameSound.Resume,.3f);
if(cues.Count!=catalog.cues.Length){catalog.cues=cues.ToArray();UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);}
var results=new System.Collections.Generic.List<string>();
foreach(var path in UnityEditor.AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/GameLoop"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).OrderBy(p=>p)) {
    var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    bool wasLoaded=existing.IsValid()&&existing.isLoaded;
    var scene=wasLoaded?existing:UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    try {
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t=>t.gameObject).ToArray();
        bool changed=false;
        var controller=all.Select(g=>g.GetComponent<GameAudioController>()).FirstOrDefault(c=>c!=null);
        if(controller==null){
            var root=new UnityEngine.GameObject("GameAudio");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            controller=root.AddComponent<GameAudioController>();controller.catalog=catalog;controller.effectsVolume=.8f;
            controller.music=all.Where(g=>g.name=="LevelMusic"||g.name=="TitleMusic").Select(g=>g.GetComponent<UnityEngine.AudioSource>()).FirstOrDefault(a=>a!=null);
            if(controller.music!=null)controller.musicVolume=controller.music.volume;
            controller.ambience=all.Where(g=>g.name=="ForestAmbience").Select(g=>g.GetComponent<UnityEngine.AudioSource>()).FirstOrDefault(a=>a!=null);
            if(controller.ambience!=null)controller.ambienceVolume=controller.ambience.volume;
            changed=true;
        }
        foreach(var go in all){
            var movement=go.GetComponent<Movement>();var enemy=go.GetComponent<BlightEnemy>();var boss=go.GetComponent<BlightBoss>();var pet=go.GetComponent<BloomPetFollower>();var crystal=go.GetComponent<BossCrystalExit>();
            if(movement==null&&enemy==null&&boss==null&&pet==null&&crystal==null||go.GetComponent<GameplaySfx>()!=null)continue;
            var sfx=go.AddComponent<GameplaySfx>();
            sfx.role=movement!=null?GameplaySfx.Role.Player:boss!=null?GameplaySfx.Role.Boss:enemy!=null?(GameplaySfx.Role)((int)enemy.Kind+1):crystal!=null?GameplaySfx.Role.Crystal:
                go.name.Contains("Whisperseed")?GameplaySfx.Role.Whisperseed:go.name.Contains("Dryad")?GameplaySfx.Role.Dryad:GameplaySfx.Role.Petalborn;
            sfx.stepDistance=sfx.role==GameplaySfx.Role.Boss?2f:sfx.role==GameplaySfx.Role.Collusus?1.2f:1.7f;
            changed=true;
        }
        foreach(var button in all.Select(g=>g.GetComponent<UnityEngine.UI.Button>()).Where(b=>b!=null))
            if(button.GetComponent<MenuButtonSfx>()==null){button.gameObject.AddComponent<MenuButtonSfx>();changed=true;}
        if(changed){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Save failed: "+path);}
        results.Add(path+": "+(changed?"repaired":"already configured"));
    }finally{if(!wasLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
return results.ToArray();
