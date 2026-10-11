// Native-frame boss audio checks on the current Boss scene, including story pause.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var results=new System.Collections.Generic.List<string>();
var queue=new System.Collections.Generic.Queue<(float,System.Action)>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
int index=0,deathSample=0,before=0;bool done=false;
GameAudioController manager=null;BlightBoss boss=null;Movement player=null;BossCrystalReward reward=null;
UnityEngine.AudioSource death=null;StoryEnding ending=null;
var oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
double deadline=0;UnityEditor.EditorApplication.CallbackFunction tick=null;
void Check(bool ok,string label){if(!ok)throw new System.Exception(label);results.Add("PASS Boss: "+label);}
void Set(object obj,string field,object value){obj.GetType().GetField(field,flags).SetValue(obj,value);}
void Later(float delay,System.Action action){queue.Enqueue((delay,action));}
void Finish(string error=null){done=true;UnityEditor.EditorApplication.update-=tick;UnityEngine.Application.runInBackground=oldBackground;if(error!=null)results.Add("FAIL "+error);else results.Add("PASS Boss scene and title return");System.IO.File.WriteAllLines("Temp/BossSceneAudioResults.txt",results);}
void Load(){
    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/GameLoop/Boss.unity");
    Later(.5f,()=>{
        manager=GameAudioController.Instance;boss=UnityEngine.Object.FindAnyObjectByType<BlightBoss>();player=UnityEngine.Object.FindAnyObjectByType<Movement>();reward=UnityEngine.Object.FindAnyObjectByType<BossCrystalReward>();ending=UnityEngine.Object.FindAnyObjectByType<StoryEnding>();
        foreach(var e in UnityEngine.Object.FindObjectsByType<BlightEnemy>()){e.enabled=false;e.GetComponent<EnemyNavigator2D>().enabled=false;e.GetComponent<UnityEngine.Rigidbody2D>().simulated=false;}
        Set(boss,"arenaLeft",9980f);Set(boss,"arenaRight",10020f);Set(boss,"slamDamage",0f);Set(boss,"lungeDamage",0f);Set(boss,"shockwaveDamage",0f);Set(boss,"projectileDamage",0f);Set(boss,"thornDamage",0f);
        var claw=boss.GetComponent<CrimsonBossMelee>();if(claw!=null)claw.damage=0;
        var floor=new UnityEngine.GameObject("BossAudioRuntimeFloor");floor.transform.position=new UnityEngine.Vector3(10000,-1.5f,0);floor.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(80,1);
        player.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(10000,0);boss.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(10003,1);
    });
    Later(2f,()=>{Check(manager.Played(GameSound.BossSlamWarn)>0&&manager.Played(GameSound.BossSlam)>0&&manager.Played(GameSound.BossShockwave)>0,"native slam windup, hit and shockwave cues");typeof(BlightBoss).GetMethod("BeginAttack",flags).Invoke(boss,new object[]{BlightBoss.AttackKind.Lunge});});
    Later(1.3f,()=>{Check(manager.Played(GameSound.BossLungeWarn)>0&&manager.Played(GameSound.BossLunge)>0,"lunge windup and execution cues");typeof(BlightBoss).GetMethod("BeginAttack",flags).Invoke(boss,new object[]{BlightBoss.AttackKind.Spores});});
    Later(1.3f,()=>{
        Check(manager.Played(GameSound.BossSporeWarn)>0&&manager.Played(GameSound.BossVolley)>0,"spore windup and volley cues");
        typeof(BlightBoss).GetMethod("BeginAttack",flags).Invoke(boss,new object[]{BlightBoss.AttackKind.Thorns});
    });
    Later(1.3f,()=>{
        Check(manager.Played(GameSound.BossThornWarn)>0&&manager.Played(GameSound.BossThorns)>0,"thorn windup and execution cues");
        var h=boss.GetComponent<BlightBossHealth>();before=manager.Played(GameSound.BossPhase);h.TakeDamage(h.MaximumHealth*.6f,UnityEngine.Vector2.zero,0);Check(manager.Played(GameSound.BossPhase)==before+1,"phase transition cue");
    });
    Later(1.6f,()=>{
        var h=boss.GetComponent<BlightBossHealth>();before=manager.Played(GameSound.BossDeath);h.TakeDamage(100000,UnityEngine.Vector2.zero,0);h.TakeDamage(100000,UnityEngine.Vector2.zero,0);
        Check(manager.Played(GameSound.BossDeath)==before+1,"one boss defeat cue");Check(reward.Revealed&&manager.Played(GameSound.CrystalAppear)==1,"one crystal appearance cue");
        death=manager.GetComponentsInChildren<UnityEngine.AudioSource>().FirstOrDefault(a=>a.isPlaying&&a.clip!=null&&a.clip.name=="BossDeath");Check(death!=null&&death.ignoreListenerPause,"defeat voice starts and can survive a cutscene pause");deathSample=death.timeSamples;
        if(ending!=null&&ending.waitForCrystal)typeof(BossCrystalExit).GetMethod("OnTriggerEnter2D",flags).Invoke(reward.crystal.GetComponent<BossCrystalExit>(),new object[]{player.GetComponent<UnityEngine.Collider2D>()});
        if(ending!=null)Check(ending.State==StoryEnding.EndingState.Waking&&UnityEngine.AudioListener.pause,"existing ending starts its story pause");
    });
    Later(.2f,()=>{Check(death.isPlaying&&death.timeSamples>deathSample,"boss defeat audio cursor advances after defeat, including story pause");});
    Later(2.5f,()=>{
        before=manager.Played(GameSound.CrystalTouch);
        if(ending==null){var body=player.GetComponent<UnityEngine.Rigidbody2D>();body.position=reward.crystal.transform.position;body.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
        else{Check(ending.State==StoryEnding.EndingState.AwaitingReturn,"existing celebration completes");ending.ReturnToTitle();ending.ReturnToTitle();}
    });
    Later(.5f,()=>{Check(manager.Played(GameSound.CrystalTouch)==before+1,"one return-to-title transition cue");Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="TitleScreen","original title transition preserved");var source=UnityEngine.GameObject.Find("TitleMusic").GetComponent<UnityEngine.AudioSource>();Check(source.isPlaying&&source.loop&&source.clip.name=="WraithglassBarrens_Loop"&&UnityEngine.Mathf.Abs(source.volume-.6f)<.001f,"title retains Wraithglass music at 60 percent");Check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>().Count(a=>a.loop)==1,"only title music loops after return");});
    Later(1.3f,()=>{Check(!UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>().Any(a=>a.name=="InteractionSoundTail"),"transition tail cleans itself up");index++;if(index==1)Finish();else Load();});
    deadline=UnityEditor.EditorApplication.timeSinceStartup+queue.Peek().Item1;
}
tick=()=>{try{if(done)return;if(!UnityEditor.EditorApplication.isPlaying){Finish("Play mode stopped");return;}if(UnityEditor.EditorApplication.timeSinceStartup<deadline)return;queue.Dequeue().Item2();System.IO.File.WriteAllLines("Temp/BossSceneAudioResults.txt",results);if(!done&&queue.Count>0)deadline=UnityEditor.EditorApplication.timeSinceStartup+queue.Peek().Item1;}catch(System.Exception e){Finish(e.ToString());}};
System.IO.File.WriteAllText("Temp/BossSceneAudioResults.txt","RUNNING");UnityEditor.EditorApplication.update+=tick;Load();return "Scheduled the current Boss scene and title playback checks";
