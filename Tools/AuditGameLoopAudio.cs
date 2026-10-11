// Read-only saved-scene and prefab audit; run with Unity CLI eval_file in Edit mode.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
var reports = new System.Collections.Generic.List<object>();
foreach (var path in UnityEditor.AssetDatabase.FindAssets("t:Scene", new[]{"Assets/Scenes/GameLoop"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).OrderBy(p=>p)) {
    var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
    try {
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t=>t.gameObject).ToArray();
        var controllers=all.Select(g=>g.GetComponent<GameAudioController>()).Where(c=>c!=null).ToArray();
        var missing=new System.Collections.Generic.List<string>();
        foreach(var g in all) {
            if(g.GetComponent<Movement>()!=null||g.GetComponent<BlightEnemy>()!=null||g.GetComponent<BlightBoss>()!=null||g.GetComponent<BloomPetFollower>()!=null||g.GetComponent<BossCrystalExit>()!=null||g.GetComponent<PlaceholderProjectile>()!=null||g.GetComponent<BlightBossHazard>()!=null)
                if(g.GetComponent<GameplaySfx>()==null)missing.Add(g.name);
        }
        reports.Add(new {scene=path, controllers=controllers.Select(c=>new {c.name,c.enabled,c.effectsVolume,c.musicVolume,c.ambienceVolume,catalog=UnityEditor.AssetDatabase.GetAssetPath(c.catalog),music=c.music==null?null:c.music.name}).ToArray(),
            audio=all.Select(g=>g.GetComponent<UnityEngine.AudioSource>()).Where(a=>a!=null).Select(a=>new {a.name,clip=UnityEditor.AssetDatabase.GetAssetPath(a.clip),a.volume,a.loop,a.playOnAwake,a.enabled}).ToArray(),
            actors=all.Count(g=>g.GetComponent<GameplaySfx>()!=null),missing=missing.ToArray(),
            playerHP=all.Select(g=>g.GetComponent<PlayerHealth>()).Where(h=>h!=null).Select(h=>h.MaximumHealth).ToArray(),
            enemies=all.Count(g=>g.GetComponent<BlightEnemy>()!=null),bosses=all.Count(g=>g.GetComponent<BlightBoss>()!=null),blooms=all.Count(g=>g.GetComponent<BloomPetFollower>()!=null),pickups=all.Count(g=>g.GetComponent<AmuletPickup>()!=null),hazards=all.Count(g=>g.GetComponent<Level02ThornHazard>()!=null)});
    } finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioCatalog>("Assets/Audio/SFX/GameAudioCatalog.asset");
var output=Newtonsoft.Json.JsonConvert.SerializeObject(new{scenes=reports,catalog=catalog.cues.Select(c=>new{sound=c.sound.ToString(),c.volume,c.minimumInterval,c.maximumConcurrent,clips=c.clips.Select(a=>a==null?null:UnityEditor.AssetDatabase.GetAssetPath(a)).ToArray()}).ToArray()},Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Temp/GameLoopAudioAudit.json",output);
return output;
