// Play-mode coverage on every saved GameLoop gameplay scene. Only runtime fixtures
// and controlled actor positions are changed. Native frames execute enemy AI and
// player input; reflection invokes existing melee/interaction methods for isolation.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var paths=new[]{"PrismbloomLevel01Story","GloamthorneLevel02","Ashpetal Level 03","Veinroot Level04","Wraithglass Barrens Level 05","Boss"};
var results=new System.Collections.Generic.List<string>();
var queue=new System.Collections.Generic.Queue<(float,System.Action)>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var audit=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Temp/GameLoopAudioAudit.json"));
var oldBackground=UnityEngine.Application.runInBackground;
var oldSettings=UnityEngine.InputSystem.InputSystem.settings;
if(oldSettings==null){oldSettings=UnityEngine.ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputSettings>();oldSettings.hideFlags=UnityEngine.HideFlags.HideAndDontSave;UnityEngine.InputSystem.InputSystem.settings=oldSettings;}
var oldSettingsFlags=oldSettings.hideFlags;
// InputManager destroys replacement settings with exactly HideAndDontSave.
// Keep the original alive until it can be restored, without touching an asset.
oldSettings.hideFlags|=UnityEngine.HideFlags.HideInInspector|UnityEngine.HideFlags.DontUnloadUnusedAsset;
var testSettings=UnityEngine.Object.Instantiate(oldSettings);
testSettings.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
UnityEngine.InputSystem.InputSystem.settings=testSettings;
UnityEngine.Application.runInBackground=true;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
testSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
int index=0,before=0,oldSample=0;float oldHP=0;bool done=false;
GameAudioController manager=null;Movement player=null;PlayerHealth health=null;PlayerCombat combat=null;
UnityEngine.AudioSource music=null,deathVoice=null;UnityEngine.GameObject enemyObject=null;
UnityEngine.EntityId controllerId=default;string expectedDestination=null;
var observedRestartVoices=new System.Collections.Generic.HashSet<UnityEngine.EntityId>();
double deadline=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Keys(params UnityEngine.InputSystem.Key[] keys){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));}
void Call(object obj,string method,params object[] args){obj.GetType().GetMethod(method,flags).Invoke(obj,args);}
void Set(object obj,string field,object value){obj.GetType().GetField(field,flags).SetValue(obj,value);}
void Check(bool ok,string name){if(!ok)throw new System.Exception(paths[index]+": "+name);results.Add("PASS "+paths[index]+": "+name);}
void Later(float delay,System.Action action){queue.Enqueue((delay,action));}
void Finish(string error=null){
    done=true;UnityEditor.EditorApplication.update-=tick;
    try {
        if(keyboard.added){Keys();UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
        UnityEngine.InputSystem.InputSystem.settings=oldSettings;
        oldSettings.hideFlags=oldSettingsFlags;
        if(testSettings!=null)UnityEngine.Object.Destroy(testSettings);
    } catch(System.Exception cleanup){results.Add("FAIL cleanup: "+cleanup.Message);}
    UnityEngine.Application.runInBackground=oldBackground;
    if(error!=null)results.Add("FAIL "+error);else results.Add("PASS all "+paths.Length+" GameLoop gameplay scenes");
    System.IO.File.WriteAllLines("Temp/GameLoopAudioCoverageResults.txt",results);
}
void FreezeActors(){
    foreach(var e in UnityEngine.Object.FindObjectsByType<BlightEnemy>()){e.enabled=false;e.GetComponent<EnemyNavigator2D>().enabled=false;e.GetComponent<UnityEngine.Rigidbody2D>().simulated=false;}
    foreach(var b in UnityEngine.Object.FindObjectsByType<BlightBoss>()){b.enabled=false;b.GetComponent<UnityEngine.Rigidbody2D>().simulated=false;}
}
void CheckMusic(){
    var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var sources=UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>();
    var tracks=sources.Where(a=>a.name=="LevelMusic"||a.name=="TitleMusic").ToArray();
    Check(tracks.Length==1,"one music source after scene load");music=tracks[0];
    var saved=audit["scenes"].First(x=>(string)x["scene"]==s.path)["audio"].First(x=>(string)x["name"]==music.name);
    var owner=audit["scenes"].First(x=>(string)x["scene"]==s.path)["controllers"].FirstOrDefault(x=>(string)x["music"]==music.name);
    float expectedGain=owner!=null?(float)owner["musicVolume"]:(float)saved["volume"];
    Check(UnityEditor.AssetDatabase.GetAssetPath(music.clip)==(string)saved["clip"]&&UnityEngine.Mathf.Abs(music.volume-expectedGain)<.001f,"original music clip and gain preserved");
    Check(music.isPlaying&&music.loop,"music is playing and looping");
    Check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>().Count(l=>l.enabled)==1,"one enabled listener");
}
void ReloadCheck(){
    manager=GameAudioController.Instance;player=UnityEngine.Object.FindAnyObjectByType<Movement>();health=player.GetComponent<PlayerHealth>();combat=player.GetComponent<PlayerCombat>();
    Check(manager!=null&&manager.GetEntityId()!=controllerId,"reload creates a fresh scene audio controller");
    Check(health.CurrentHealth==health.MaximumHealth,"reload restores player HP");
    Check(manager.Played(GameSound.LevelRestart)==1,"one restart cue on the new level");
    Check(observedRestartVoices.Contains(manager.GetEntityId()),"restart cue was observed playing during its short lifetime");
    CheckMusic();FreezeActors();
}
void LoadCase(){
    StoryRunSession.MarkIntroductionPlayed();
    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/GameLoop/"+paths[index]+".unity");
    Later(.5f,()=>{
        manager=GameAudioController.Instance;player=UnityEngine.Object.FindAnyObjectByType<Movement>();health=player.GetComponent<PlayerHealth>();combat=player.GetComponent<PlayerCombat>();
        Check(manager!=null&&manager.catalog!=null,"controller and licensed sound catalog present");
        Check(UnityEngine.Object.FindObjectsByType<GameAudioController>().Length==1,"one scene audio controller");
        CheckMusic();oldSample=music.timeSamples;FreezeActors();
        var floor=new UnityEngine.GameObject("AudioCoverageRuntimeFloor");floor.transform.position=new UnityEngine.Vector3(10000,-1.5f,0);floor.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(100,1);
        player.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(10000,0);
        before=manager.Played(GameSound.Footstep);Keys(UnityEngine.InputSystem.Key.RightArrow);
    });
    Later(.8f,()=>{Check(manager.Played(GameSound.Footstep)>before,"native walking plays footsteps");Check(music.timeSamples!=oldSample,"music cursor advances during gameplay");Keys();before=manager.Played(GameSound.Jump);});
    Later(.1f,()=>Keys(UnityEngine.InputSystem.Key.Z));
    Later(.12f,()=>{Check(manager.Played(GameSound.Jump)==before+1,"native jump plays once");Keys();before=manager.Played(GameSound.DoubleJump);});
    Later(.1f,()=>Keys(UnityEngine.InputSystem.Key.Z));
    Later(.12f,()=>{Check(manager.Played(GameSound.DoubleJump)==before+1,"native double jump plays once");Keys();before=manager.Played(GameSound.Land)+manager.Played(GameSound.HardLand);});
    Later(1.6f,()=>{Check(manager.Played(GameSound.Land)+manager.Played(GameSound.HardLand)==before+1,"one landing impact");before=manager.Played(GameSound.TailSweep);Call(combat,"PerformMelee",false);});
    Later(.12f,()=>{Check(manager.Played(GameSound.TailSweep)==before+1,"basic melee emits one sweep");before=manager.Played(GameSound.TailSweep);Call(combat,"PerformMelee",true);});
    Later(.12f,()=>{Check(manager.Played(GameSound.TailSweep)==before+1,"charged tail sweep emits one sweep");before=manager.Played(GameSound.FireLaunch);Keys(UnityEngine.InputSystem.Key.F);});
    Later(.12f,()=>{Check(manager.Played(GameSound.FireLaunch)==before+1,"native ranged attack emits one launch");Keys();var wall=new UnityEngine.GameObject("AudioCoverageRuntimeWall");wall.transform.position=player.transform.position+UnityEngine.Vector3.right*5;wall.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(.5f,4);before=manager.Played(GameSound.FireImpact);});
    Later(.6f,()=>{Check(manager.Played(GameSound.FireImpact)==before+1,"physical projectile collision emits one impact");UnityEngine.Object.Destroy(UnityEngine.GameObject.Find("AudioCoverageRuntimeWall"));before=manager.Played(GameSound.PlayerHurt);oldHP=health.CurrentHealth;health.TakeDamage(1,UnityEngine.Vector2.zero,0);health.TakeDamage(1,UnityEngine.Vector2.zero,0);Check(health.CurrentHealth==oldHP-1&&manager.Played(GameSound.PlayerHurt)==before+1,"accepted damage sounds once; invulnerability rejects overlapping damage");});
    foreach(var kind in new[]{BlightEnemy.BlightType.Chomper,BlightEnemy.BlightType.Spora,BlightEnemy.BlightType.Collusus}){
        var captured=kind;bool present=false;int warnBefore=0,attackBefore=0;
        var warn=kind==BlightEnemy.BlightType.Chomper?GameSound.ChomperWarn:kind==BlightEnemy.BlightType.Spora?GameSound.SporaWarn:GameSound.RockWarn;
        var attack=kind==BlightEnemy.BlightType.Chomper?GameSound.ChomperBite:kind==BlightEnemy.BlightType.Spora?GameSound.SporaLaunch:GameSound.RockSlam;
        Later(.3f,()=>{
            var e=UnityEngine.Object.FindObjectsByType<BlightEnemy>().FirstOrDefault(b=>b.Kind==captured);present=e!=null;if(!present)return;enemyObject=e.gameObject;
            Set(e,"damage",0f);var body=e.GetComponent<UnityEngine.Rigidbody2D>();body.simulated=true;body.position=new UnityEngine.Vector2(player.transform.position.x+(captured==BlightEnemy.BlightType.Spora?4:1.35f),0);body.linearVelocity=UnityEngine.Vector2.zero;
            e.GetComponent<EnemyNavigator2D>().enabled=true;e.enabled=true;warnBefore=manager.Played(warn);attackBefore=manager.Played(attack);
        });
        Later(1.6f,()=>{
            if(!present)return;var e=enemyObject.GetComponent<BlightEnemy>();
            Check(manager.Played(warn)>warnBefore&&manager.Played(attack)>attackBefore,captured+" native AI windup and attack produce sound");
            var h=e.GetComponent<EnemyHealth>();var hit=captured==BlightEnemy.BlightType.Chomper?GameSound.ChomperHit:captured==BlightEnemy.BlightType.Spora?GameSound.SporaHit:GameSound.RockHit;
            before=manager.Played(hit);h.TakeDamage(1,UnityEngine.Vector2.zero,0);Check(manager.Played(hit)==before+1,captured+" damage feedback");
            var death=captured==BlightEnemy.BlightType.Chomper?GameSound.ChomperDeath:captured==BlightEnemy.BlightType.Spora?GameSound.SporaDeath:GameSound.RockDeath;
            before=manager.Played(death);h.TakeDamage(100000,UnityEngine.Vector2.zero,0);h.TakeDamage(100000,UnityEngine.Vector2.zero,0);Check(manager.Played(death)==before+1,captured+" duplicate death damage produces one cue");
        });
    }
    Later(.8f,()=>{controllerId=manager.GetEntityId();before=manager.Played(GameSound.PlayerDeath);health.TakeDamage(100000,UnityEngine.Vector2.zero,0);health.TakeDamage(100000,UnityEngine.Vector2.zero,0);Check(manager.Played(GameSound.PlayerDeath)==before+1,"one health-death cue");});
    Later(.95f,()=>ReloadCheck());
    Later(.5f,()=>{
        var hazard=UnityEngine.Object.FindAnyObjectByType<Level02ThornHazard>();if(hazard==null)return;
        controllerId=manager.GetEntityId();before=manager.Played(GameSound.PlayerDeath);
        for(int i=0;i<5;i++)Call(hazard,"OnTriggerStay2D",player.GetComponent<UnityEngine.Collider2D>());
        Check(manager.Played(GameSound.PlayerDeath)==before+1,"repeated lethal hazard callbacks emit one death tail");
    });
    Later(.5f,()=>{if(manager==null||manager.GetEntityId()!=GameAudioController.Instance.GetEntityId())ReloadCheck();});
    Later(.4f,()=>{
        var pickup=UnityEngine.Object.FindAnyObjectByType<AmuletPickup>();if(pickup==null)return;
        expectedDestination=StoryRunSession.Destination(pickup.nextScene);before=manager.Played(GameSound.AmuletCollect);
        for(int i=0;i<5;i++)Call(pickup,"OnTriggerEnter2D",player.GetComponent<UnityEngine.Collider2D>());
        Check(manager.Played(GameSound.AmuletCollect)==before+1,"repeated amulet contacts produce one collection/transition cue");
    });
    Later(.5f,()=>{
        if(expectedDestination!=null){Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith(expectedDestination.Replace(".unity","")+".unity")||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==expectedDestination,"amulet retains the original next-level destination");CheckMusic();expectedDestination=null;}
    });
    Later(.5f,()=>{index++;if(index==paths.Length)Finish();else LoadCase();});
    deadline=UnityEditor.EditorApplication.timeSinceStartup+queue.Peek().Item1;
}
tick=()=>{try{
    if(done)return;if(!UnityEditor.EditorApplication.isPlaying){Finish("Play mode stopped");return;}
    var current=GameAudioController.Instance;
    if(current!=null&&current.Played(GameSound.LevelRestart)>0&&current.GetComponentsInChildren<UnityEngine.AudioSource>().Any(a=>a.isPlaying&&a.clip!=null&&a.clip.name=="Resume"))
        observedRestartVoices.Add(current.GetEntityId());
    if(UnityEditor.EditorApplication.timeSinceStartup<deadline)return;
    var action=queue.Dequeue();action.Item2();
    System.IO.File.WriteAllLines("Temp/GameLoopAudioCoverageResults.txt",results);
    if(!done&&queue.Count>0)deadline=UnityEditor.EditorApplication.timeSinceStartup+queue.Peek().Item1;
}catch(System.Exception e){Finish(e.ToString());}};
System.IO.File.WriteAllText("Temp/GameLoopAudioCoverageResults.txt","RUNNING");
UnityEditor.EditorApplication.update+=tick;LoadCase();
return "Scheduled runtime audio coverage on all current gameplay scenes.";
