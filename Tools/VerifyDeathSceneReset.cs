// Run through Unity CLI eval_file in Play mode. Loads saved levels and exercises
// real health deaths and hazard trigger callbacks across asynchronous reloads.
// Runtime-only mutations are discarded; no scenes or prefabs are saved.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var cases = new[] {
    ("Assets/Scenes/GameLoop/PrismbloomLevel01.unity", false, false),
    ("Assets/Scenes/GameLoop/GloamthorneLevel02.unity", false, false),
    ("Assets/Scenes/GameLoop/Ashpetal Level 03.unity", false, false),
    ("Assets/Scenes/GameLoop/Veinroot Level04.unity", false, false),
    ("Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity", false, false),
    ("Assets/Scenes/GameLoop/Boss.unity", false, false),
    ("Assets/Scenes/GameLoop/GloamthorneLevel02.unity", true, false),
    ("Assets/Scenes/GameLoop/Veinroot Level04.unity", true, false),
    ("Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity", true, false),
    ("Assets/Scenes/Level02.unity", true, false),
    ("Assets/Scenes/Level05.unity", true, false),
    ("Assets/Scenes/Level 06.unity", true, false),
    ("Assets/Scenes/EnemyAI.unity", false, true)
};
var results = new System.Collections.Generic.List<string>();
var snapshots = new System.Collections.Generic.Dictionary<string,(UnityEngine.Vector3 position,float health,UnityEngine.EntityId id)>();
int index = 0, stage = 0, reloads = 0;
UnityEngine.EntityId oldPlayerId = default;
UnityEngine.Vector3 playerSpawn = default;
float expectedPlayerHealth = 0;
double deadline = 0;
bool done = false;
bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
UnityEditor.EditorApplication.CallbackFunction tick = null;
UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode> onLoaded = null;
string Key(UnityEngine.Transform t) {
    string name = t.name + "[" + t.GetSiblingIndex() + "]";
    return t.parent != null ? Key(t.parent) + "/" + name : name;
}
void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
EnemyHealth[] Enemies(UnityEngine.SceneManagement.Scene s) {
    return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EnemyHealth>(true)).Where(e=>e.enabled).ToArray();
}
void FreezeEnemies(UnityEngine.SceneManagement.Scene s) {
    foreach (var root in s.GetRootGameObjects()) {
        foreach(var ai in root.GetComponentsInChildren<BlightEnemy>(true)) ai.enabled=false;
        foreach(var boss in root.GetComponentsInChildren<BlightBoss>(true)) boss.enabled=false;
    }
    foreach(var e in Enemies(s)) {
        var body=e.GetComponent<UnityEngine.Rigidbody2D>();
        if(body!=null) { body.linearVelocity=UnityEngine.Vector2.zero; body.constraints=UnityEngine.RigidbodyConstraints2D.FreezeAll; }
    }
}
void Finish(string error) {
    if(done) return;
    done=true;
    UnityEngine.SceneManagement.SceneManager.sceneLoaded-=onLoaded;
    UnityEditor.EditorApplication.update-=tick;
    UnityEngine.Application.runInBackground=oldBackground;
    UnityEngine.Time.timeScale=1;
    UnityEngine.AudioListener.pause=false;
    results.Add(error==null ? "PASS all 13 death/hazard reset cases" : "FAIL " + error);
    System.IO.File.WriteAllLines("Temp/DeathSceneResetResults.txt",results);
}
void LoadCase() {
    stage=0;reloads=0;deadline=UnityEditor.EditorApplication.timeSinceStartup+15;
    var path=cases[index].Item1;
    if(UnityEngine.Application.CanStreamedLevelBeLoaded(path))
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(path,UnityEngine.SceneManagement.LoadSceneMode.Single);
    else
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
}
onLoaded=(scene,mode)=>{
    try {
        if(done || scene.path!=cases[index].Item1) return;
        var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerHealth>(true)).First(h=>h.enabled);
        if(stage==0) {
            stage=1;
            playerSpawn=player.transform.position;
            expectedPlayerHealth=player.MaximumHealth;
            oldPlayerId=player.GetEntityId();
            snapshots.Clear();
            foreach(var e in Enemies(scene)) snapshots.Add(Key(e.transform),(e.transform.position,e.MaximumHealth,e.GetEntityId()));
            if(scene.path.Contains("/GameLoop/") || scene.name=="EnemyAI")
                Check(snapshots.Count>0,"No enemies in "+scene.path);
            FreezeEnemies(scene);
            var enemies=Enemies(scene);
            // Move and injure every enemy; kill one to verify defeated enemies return.
            for(int i=0;i<enemies.Length;i++) {
                enemies[i].transform.position+=new UnityEngine.Vector3(2+i,.25f,0);
                enemies[i].TakeDamage(i==0?100000:1,UnityEngine.Vector2.zero,0);
            }
            var marker=new UnityEngine.GameObject("DeathResetRuntimeOnlyMarker");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker,scene);
            var pickup=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AmuletPickup>(true)).FirstOrDefault();
            if(pickup!=null) pickup.gameObject.SetActive(false);
            if(cases[index].Item2) {
                var respawn=player.GetComponent<Level02ThornRespawn>();
                Check(respawn!=null,"Missing hazard receiver");
                // A full reset does not need the old teleport marker.
                respawn.entrance=null;
                var hazard=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Level02ThornHazard>(true)).First();
                var collider=player.GetComponent<UnityEngine.Collider2D>();
                for(int i=0;i<5;i++) typeof(Level02ThornHazard).GetMethod("OnTriggerStay2D",flags).Invoke(hazard,new object[]{collider});
            } else {
                player.TakeDamage(100000,UnityEngine.Vector2.zero,0);
                Check(player.IsDead,"Health death did not trigger");
                if(cases[index].Item3) { UnityEngine.Time.timeScale=0; UnityEngine.AudioListener.pause=true; }
            }
        } else if(stage==1) {
            reloads++;
            Check(player.GetEntityId()!=oldPlayerId,"Player was reused rather than recreated");
            Check(player.CurrentHealth==expectedPlayerHealth,"Player health did not reset");
            Check(UnityEngine.Vector3.Distance(player.transform.position,playerSpawn)<.001f,"Player spawn did not reset");
            var restored=Enemies(scene);
            Check(restored.Length==snapshots.Count,"Defeated enemy did not respawn");
            foreach(var e in restored) {
                Check(snapshots.TryGetValue(Key(e.transform),out var saved),"Enemy hierarchy changed");
                Check(e.GetEntityId()!=saved.id,"Enemy was reused");
                Check(UnityEngine.Vector3.Distance(e.transform.position,saved.position)<.001f,"Enemy position did not reset: "+e.name);
                Check(e.CurrentHealth==saved.health,"Enemy health did not reset: "+e.name);
            }
            Check(UnityEngine.GameObject.Find("DeathResetRuntimeOnlyMarker")==null,"Runtime objects survived scene reset");
            foreach(var root in scene.GetRootGameObjects())foreach(var pickup in root.GetComponentsInChildren<AmuletPickup>(true))
                Check(pickup.gameObject.activeSelf,"Collected pickup did not return");
            Check(UnityEngine.Time.timeScale==1 && !UnityEngine.AudioListener.pause,"Paused state survived restart");
            FreezeEnemies(scene);
            stage=2; deadline=UnityEditor.EditorApplication.timeSinceStartup+.6;
        } else if(stage==2) throw new System.Exception("Repeated hazard contacts caused duplicate reloads");
    } catch(System.Exception e) { Finish(e.ToString()); }
};
tick=()=>{
    try {
        if(done) return;
        if(!UnityEditor.EditorApplication.isPlaying) { Finish("Play mode stopped");return; }
        if(UnityEditor.EditorApplication.timeSinceStartup<deadline) return;
        if(stage!=2) { Finish("Reload timed out: "+cases[index].Item1+" stage="+stage);return; }
        Check(reloads==1,"Expected exactly one reload");
        results.Add("PASS "+cases[index].Item1+" "+(cases[index].Item2?"hazard":"health death")+
            (cases[index].Item3?" while paused":"")+": new player at spawn/full HP, authored enemies="+snapshots.Count+", saved enemy state restored, transient objects removed, pickups restored, one reload");
        index++;
        if(index==cases.Length) Finish(null); else LoadCase();
    } catch(System.Exception e) { Finish(e.ToString()); }
};
System.IO.File.WriteAllText("Temp/DeathSceneResetResults.txt","RUNNING death/hazard scene reset checks");
UnityEngine.SceneManagement.SceneManager.sceneLoaded+=onLoaded;
UnityEditor.EditorApplication.update+=tick;
LoadCase();
return "Scheduled 13 real scene reload checks; results in Temp/DeathSceneResetResults.txt";
