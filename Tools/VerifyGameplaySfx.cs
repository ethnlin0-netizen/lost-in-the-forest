// Native-frame Play-mode checks using a temporary physics fixture, never saved.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var results=new System.Collections.Generic.List<string>();
var queue=new System.Collections.Generic.Queue<(float,System.Action)>();
var manager=GameAudioController.Instance;
var intervals=manager.catalog.cues.Select(c=>c.minimumInterval).ToArray();
var menu=UnityEngine.Object.FindAnyObjectByType<TitleMenu>();
var owned=new System.Collections.Generic.List<UnityEngine.GameObject>();
var originals=new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach(var m in UnityEngine.Object.FindObjectsByType<Movement>()){originals.Add(m.gameObject);m.gameObject.SetActive(false);}
foreach(var e in UnityEngine.Object.FindObjectsByType<BlightEnemy>()){originals.Add(e.gameObject);e.gameObject.SetActive(false);}
foreach(var b in UnityEngine.Object.FindObjectsByType<BloomPetFollower>()){originals.Add(b.gameObject);b.gameObject.SetActive(false);}
var oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
var oldFocus=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var floor=new UnityEngine.GameObject("SFX test ground");owned.Add(floor);floor.transform.position=new UnityEngine.Vector3(10000,-1.5f,0);floor.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(200,1);
UnityEngine.GameObject Spawn(string path,float x=10000,float y=0){var g=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),new UnityEngine.Vector3(x,y,0),UnityEngine.Quaternion.identity);owned.Add(g);return g;}
var player=Spawn("Assets/Prefabs/Player.prefab");var movement=player.GetComponent<Movement>();var health=player.GetComponent<PlayerHealth>();var combat=player.GetComponent<PlayerCombat>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Set(object obj,string field,object value){obj.GetType().GetField(field,flags).SetValue(obj,value);}
void Call(object obj,string method,params object[] args){obj.GetType().GetMethod(method,flags).Invoke(obj,args);}
void Keys(params UnityEngine.InputSystem.Key[] keys){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));}
void Check(bool value,string name){results.Add((value?"PASS: ":"FAIL: ")+name);}
void Later(float delay,System.Action action){queue.Enqueue((delay,action));}
BlightEnemy enemy=null;BlightBoss boss=null;UnityEngine.GameObject actor=null;int before=0;float beforeHealth=0,startX=0,peakY=0;int[] snapshot=null;UnityEngine.AudioSource warning=null;
Later(.6f,()=>{Check(movement.IsGrounded,"Fixture settles on a real platform");startX=player.transform.position.x;Keys(UnityEngine.InputSystem.Key.RightArrow);});
Later(.65f,()=>{Check(player.transform.position.x-startX>2f&&manager.Played(GameSound.Footstep)>0,"Native grounded movement produces distance-based footsteps");Keys();});
Later(.2f,()=>{before=manager.Played(GameSound.Jump);Keys(UnityEngine.InputSystem.Key.Z);});
Later(.14f,()=>{Check(manager.Played(GameSound.Jump)==before+1&&player.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity.y>0,"Normal jump plays once with upward physics");Keys();before=manager.Played(GameSound.DoubleJump);});
Later(.1f,()=>Keys(UnityEngine.InputSystem.Key.Z));
Later(.14f,()=>{Check(manager.Played(GameSound.DoubleJump)==before+1&&player.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity.y>0,"Wing-assisted double jump plays once");Keys();before=manager.Played(GameSound.Land)+manager.Played(GameSound.HardLand);peakY=player.transform.position.y;});
Later(1.5f,()=>{Check(movement.IsGrounded&&manager.Played(GameSound.Land)+manager.Played(GameSound.HardLand)==before+1,"Airborne-to-grounded landing produces one impact");before=manager.Played(GameSound.TailSweep);Keys(UnityEngine.InputSystem.Key.X);});
Later(.12f,()=>{Check(manager.Played(GameSound.TailSweep)==before+1&&combat.LastMeleeWasCharged,"Existing X tail sweep produces one whoosh");Keys();});
Later(.2f,()=>{before=manager.Played(GameSound.FireLaunch);Keys(UnityEngine.InputSystem.Key.F);});
Later(.12f,()=>{Check(manager.Played(GameSound.FireLaunch)==before+1&&UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Any(p=>p.GetComponent<GameplaySfx>()?.role==GameplaySfx.Role.Fireball),"Existing F creates a projectile and one launch sound");Keys();
 var wall=new UnityEngine.GameObject("SFX impact wall");owned.Add(wall);wall.transform.position=player.transform.position+UnityEngine.Vector3.right*5;wall.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(.5f,4);before=manager.Played(GameSound.FireImpact);});
Later(.5f,()=>{Check(manager.Played(GameSound.FireImpact)==before+1,"Swept fireball collision produces one impact");UnityEngine.Object.Destroy(owned.Last());beforeHealth=health.CurrentHealth;before=manager.Played(GameSound.PlayerHurt);health.TakeDamage(1,UnityEngine.Vector2.right,0);health.TakeDamage(5,UnityEngine.Vector2.right,0);Check(health.CurrentHealth==beforeHealth-1&&manager.Played(GameSound.PlayerHurt)==before+1,"Only accepted damage produces hurt feedback; invulnerable duplicate rejected");});

foreach(var kind in new[]{"Chomper","Spora","Collusus"}){
 var captured=kind;
 Later(.8f,()=>{actor=Spawn("Assets/Prefabs/Enemies/"+captured+".prefab",player.transform.position.x+(captured=="Spora"?4:1.35f));enemy=actor.GetComponent<BlightEnemy>();Set(enemy,"damage",0f);});
 Later(1.7f,()=>{
  var warn=captured=="Chomper"?GameSound.ChomperWarn:captured=="Spora"?GameSound.SporaWarn:GameSound.RockWarn;
  var hit=captured=="Chomper"?GameSound.ChomperBite:captured=="Spora"?GameSound.SporaLaunch:GameSound.RockSlam;
  Check(enemy.AttacksPerformed>0&&manager.Played(warn)>0&&manager.Played(hit)>0,captured+" detects player, winds up, then executes with audio");
  var h=actor.GetComponent<EnemyHealth>();var hitSound=captured=="Chomper"?GameSound.ChomperHit:captured=="Spora"?GameSound.SporaHit:GameSound.RockHit;
  int hits=manager.Played(hitSound);h.TakeDamage(1,UnityEngine.Vector2.right,0);Check(manager.Played(hitSound)==hits+1,captured+" accepted hit has creature-specific feedback");
  warning=manager.GetComponentsInChildren<UnityEngine.AudioSource>().FirstOrDefault(s=>s.isPlaying&&s.clip!=null&&s.clip.name.StartsWith(warn.ToString()));Check(warning==null,captured+" hit interruption stops its warning");
  var deathSound=captured=="Chomper"?GameSound.ChomperDeath:captured=="Spora"?GameSound.SporaDeath:GameSound.RockDeath;
  int deaths=manager.Played(deathSound);h.TakeDamage(100000,UnityEngine.Vector2.right,0);h.TakeDamage(100000,UnityEngine.Vector2.right,0);
  Check(h.IsDead&&!enemy.enabled&&manager.Played(deathSound)==deaths+1,captured+" death plays once and cancels AI");UnityEngine.Object.Destroy(actor);
  Check(manager.GetComponentsInChildren<UnityEngine.AudioSource>().Any(s=>s.isPlaying&&s.clip!=null&&s.clip.name.StartsWith(deathSound.ToString())),captured+" death voice belongs to controller rather than destroyed actor");
 });
}

foreach(var petName in new[]{"Whisperseed","Dryad","Petalborn"}){
 var captured=petName;
 Later(.8f,()=>{actor=Spawn("Assets/Prefabs/BloomPets/"+captured+"Pet.prefab",player.transform.position.x,player.transform.position.y);before=manager.Played((GameSound)System.Enum.Parse(typeof(GameSound),captured+"Recruit"));});
 Later(.2f,()=>{var pet=actor.GetComponent<BloomPetFollower>();Check(pet.IsRecruited&&manager.Played((GameSound)System.Enum.Parse(typeof(GameSound),captured+"Recruit"))==before+1,captured+" contact recruitment chimes once across Enter/Stay callbacks");UnityEngine.Object.Destroy(actor);});
}

Later(.7f,()=>{actor=Spawn("Assets/Prefabs/Enemies/Chomper.prefab",player.transform.position.x+1.25f);enemy=actor.GetComponent<BlightEnemy>();enemy.enabled=false;actor.GetComponent<UnityEngine.Rigidbody2D>().constraints=UnityEngine.RigidbodyConstraints2D.FreezeAll;
 var duplicate=new UnityEngine.GameObject("Additional target collider");duplicate.transform.SetParent(actor.transform,false);duplicate.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(.4f,.4f);
 beforeHealth=actor.GetComponent<EnemyHealth>().CurrentHealth;before=manager.Played(GameSound.ChomperHit);Call(combat,"PerformMelee",false);});
Later(.15f,()=>{Check(actor.GetComponent<EnemyHealth>().CurrentHealth==beforeHealth-15&&manager.Played(GameSound.ChomperHit)==before+1,"Basic melee damages multi-collider target once with one hit sound");beforeHealth=actor.GetComponent<EnemyHealth>().CurrentHealth;Call(combat,"PerformMelee",true);});
Later(.15f,()=>{Check(actor.GetComponent<EnemyHealth>().CurrentHealth==beforeHealth-30,"Charged tail damage remains unchanged");UnityEngine.Object.Destroy(actor);
 actor=Spawn("Assets/Prefabs/Enemies/Spora.prefab",player.transform.position.x+3);actor.GetComponent<BlightEnemy>().enabled=false;beforeHealth=actor.GetComponent<EnemyHealth>().CurrentHealth;Call(combat,"FireProjectile");});
Later(.5f,()=>{Check(actor.GetComponent<EnemyHealth>().CurrentHealth==beforeHealth-15,"Player projectile retains 15 damage while impact audio fires");UnityEngine.Object.Destroy(actor);});

Later(.8f,()=>{actor=Spawn("Assets/Prefabs/Bosses/BlightAmalgamBoss.prefab",player.transform.position.x+3);boss=actor.GetComponent<BlightBoss>();Set(boss,"arenaLeft",player.transform.position.x-20);Set(boss,"arenaRight",player.transform.position.x+20);Set(boss,"slamDamage",0f);Set(boss,"lungeDamage",0f);Set(boss,"shockwaveDamage",0f);Set(boss,"projectileDamage",0f);});
Later(2f,()=>{Check(boss.SlamsPerformed>0&&manager.Played(GameSound.BossSlamWarn)>0&&manager.Played(GameSound.BossSlam)>0&&manager.Played(GameSound.BossShockwave)>0,"Boss slam has distinct warning, execution and shockwave cues");Call(boss,"BeginAttack",BlightBoss.AttackKind.Lunge);});
Later(1.1f,()=>{Check(boss.LungesPerformed>0&&manager.Played(GameSound.BossLungeWarn)>0&&manager.Played(GameSound.BossLunge)>0,"Boss lunge warning and execution follow existing windup");Call(boss,"BeginAttack",BlightBoss.AttackKind.Spores);});
Later(1.3f,()=>{Check(boss.VolleysPerformed>0&&manager.Played(GameSound.BossSporeWarn)>0&&manager.Played(GameSound.BossVolley)>0,"Boss volley emits one launch cue for its three projectiles");
 var h=actor.GetComponent<BlightBossHealth>();before=manager.Played(GameSound.BossPhase);h.TakeDamage(h.MaximumHealth*.6f,UnityEngine.Vector2.right,0);Check(boss.PhaseTwo&&manager.Played(GameSound.BossPhase)==before+1,"Boss half-health phase emits one transition cue");h.TakeDamage(1,UnityEngine.Vector2.right,0);Check(manager.Played(GameSound.BossPhase)==before+1,"Repeated phase-two damage does not repeat transition");
 before=manager.Played(GameSound.BossDeath);h.TakeDamage(100000,UnityEngine.Vector2.right,0);h.TakeDamage(100000,UnityEngine.Vector2.right,0);Check(h.IsDead&&!boss.enabled&&manager.Played(GameSound.BossDeath)==before+1,"Boss defeat cue fires once and stops attacks/hazards");UnityEngine.Object.Destroy(actor);});

Later(1f,()=>{
 var wave=Spawn("Assets/Prefabs/Enemies/SporeProjectile.prefab",player.transform.position.x+3,player.transform.position.y);before=manager.Played(GameSound.SporeImpact);wave.GetComponent<PlaceholderProjectile>().Launch(UnityEngine.Vector2.left,5,8,null,0,player.transform);
});
Later(.7f,()=>Check(manager.Played(GameSound.SporeImpact)==before+1,"Visible spore swept collision produces one impact"));

Later(3f,()=>{snapshot=System.Enum.GetValues(typeof(GameSound)).Cast<GameSound>().Select(manager.Played).ToArray();menu.PauseGame();Check(menu.IsPaused&&UnityEngine.AudioListener.pause&&UnityEngine.Time.timeScale==0,"Existing pause system remains intact");
 Check(manager.Play(GameSound.Footstep,player)==0,"Paused gameplay rejects new sound requests");
 var b=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().First(b=>b.name=="ControlsButton");b.GetComponent<MenuButtonSfx>().OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
 Check(manager.GetComponentsInChildren<UnityEngine.AudioSource>().Any(s=>s.isPlaying&&s.ignoreListenerPause),"Menu and pause voices remain playable while listener is paused");});
Later(.35f,()=>{Check(manager.Played(GameSound.Pause)>0,"Pause feedback is emitted");menu.StartGame();Check(!menu.IsPaused&&UnityEngine.Time.timeScale>0&&!UnityEngine.AudioListener.pause&&manager.Played(GameSound.Resume)>0,"Resume restores gameplay and emits UI feedback");});

Later(1f,()=>{before=manager.Played(GameSound.PlayerDeath);Set(health,"deathRespawnDelay",30f);health.TakeDamage(100000,UnityEngine.Vector2.right,0);health.TakeDamage(100000,UnityEngine.Vector2.right,0);Check(health.IsDead&&!movement.enabled&&!combat.enabled&&manager.Played(GameSound.PlayerDeath)==before+1,"Player death plays once and keeps existing disabled movement/combat");});
Later(2f,()=>{
 // Exercise the pool itself, independent of the event hooks above.
 foreach(var cue in manager.catalog.cues)cue.minimumInterval=0;
 foreach(var id in new[]{GameSound.Footstep,GameSound.Land,GameSound.TailSweep,GameSound.FireLaunch,GameSound.FireImpact,GameSound.ChomperHit,GameSound.SporaHit,GameSound.RockHit})
  for(int i=0;i<3;i++)manager.Play(id,manager);
 Check(manager.ActiveVoices<=24,"Simultaneous effect voices are bounded to 24");
 int token=manager.Play(GameSound.BossSlamWarn,manager);Check(token>0,"High-priority boss warning can preempt a full decorative pool");manager.Cancel(token);
 Check(!manager.GetComponentsInChildren<UnityEngine.AudioSource>().Any(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="BossSlamWarn"),"Warning cancellation stops a reserved voice");
});

double deadline=UnityEditor.EditorApplication.timeSinceStartup+.01;
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Finish(){UnityEditor.EditorApplication.update-=tick;for(int i=0;i<intervals.Length;i++)manager.catalog.cues[i].minimumInterval=intervals[i];Keys();UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=oldFocus;UnityEngine.Application.runInBackground=oldBackground;foreach(var obj in owned)if(obj!=null)UnityEngine.Object.Destroy(obj);foreach(var obj in originals)if(obj!=null)obj.SetActive(true);System.IO.File.WriteAllLines("Temp/sfx-work/gameplay-test.txt",results);}
tick=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){results.Add("FAIL: Play mode exited early");Finish();return;}
 if(UnityEditor.EditorApplication.timeSinceStartup<deadline)return;
 if(queue.Count==0){Finish();return;}
 var next=queue.Dequeue();try{next.Item2();}catch(System.Exception ex){results.Add("FAIL: exception "+ex);}
 System.IO.File.WriteAllLines("Temp/sfx-work/gameplay-test.txt",results);
 deadline=UnityEditor.EditorApplication.timeSinceStartup+(queue.Count>0?queue.Peek().Item1:.01);
};
UnityEditor.EditorApplication.update+=tick;
return "Native-frame checks scheduled; Temp/sfx-work/gameplay-test.txt";
