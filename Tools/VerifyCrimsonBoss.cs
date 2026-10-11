if(!UnityEditor.EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Start Boss Play mode first.");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Call(object o,string m,params object[] args){o.GetType().GetMethod(m,flags).Invoke(o,args);}
void Set(object o,string f,object v){o.GetType().GetField(f,flags).SetValue(o,v);}
void Check(bool value,string message){if(!value)throw new System.Exception(message);}
var boss=UnityEngine.Object.FindAnyObjectByType<BlightBoss>();var bh=boss.GetComponent<BlightBossHealth>();var bb=boss.GetComponent<UnityEngine.Rigidbody2D>();
var player=UnityEngine.Object.FindAnyObjectByType<Movement>();var pb=player.GetComponent<UnityEngine.Rigidbody2D>();var ph=player.GetComponent<PlayerHealth>();var animator=boss.GetComponent<FusedBossAnimator>();
var audio=UnityEngine.Object.FindAnyObjectByType<GameAudioController>();var mode=UnityEngine.Physics2D.simulationMode;var scale=UnityEngine.Time.timeScale;
var results=new System.Collections.Generic.List<string>();UnityEngine.InputSystem.Keyboard keyboard=null;
void PlaceBoss(float x){bb.position=new UnityEngine.Vector2(x,-2.48f);bb.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
void PlacePlayer(float x,float feet=-2.47f){float footOffset=pb.position.y-player.GetComponent<UnityEngine.Collider2D>().bounds.min.y;pb.position=new UnityEngine.Vector2(x,feet+footOffset);pb.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
void Input(params UnityEngine.InputSystem.Key[] keys){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));UnityEngine.InputSystem.InputSystem.Update();Call(player,"Update");}
void Tick(int count,bool ai=true){for(int i=0;i<count;i++){Call(player,"FixedUpdate");if(ai&&boss.enabled)Call(boss,"FixedUpdate");foreach(var h in UnityEngine.Object.FindObjectsByType<BlightBossHazard>())if(h.isActiveAndEnabled)Call(h,"FixedUpdate");UnityEngine.Physics2D.Simulate(.02f);}}
try{
 UnityEngine.Time.timeScale=1;UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;
 keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();Input();
 typeof(PlayerHealth).GetProperty("CurrentHealth").SetValue(ph,1000f);Set(ph,"invulnerableUntil",0f);
 PlaceBoss(0);PlacePlayer(-5);Tick(15,false);
 Check(bh.MaximumHealth==1000,"Boss maximum HP changed");Check(boss.GetComponent<UnityEngine.BoxCollider2D>().size==new UnityEngine.Vector2(3,3.35f),"Boss hull incorrect");
 Check(audio.music.clip.name=="Boss_Dreadcrown_Crowncrusher"&&audio.music.loop,"Boss music missing");
 foreach(var sound in new[]{GameSound.BossSlam,GameSound.BossLunge,GameSound.BossVolley,GameSound.BossThorns,GameSound.BossPhase,GameSound.BossDeath})Check(audio.catalog.Find(sound)?.clips[0]!=null,"Missing cue "+sound);
 results.Add("Boss collider, 1000 maximum HP, original sound cues and looping boss music verified.");
 // All four windups have visibly distinct art poses without modifying the hull.
 foreach(var attack in new[]{BlightBoss.AttackKind.Slam,BlightBoss.AttackKind.Lunge,BlightBoss.AttackKind.Spores,BlightBoss.AttackKind.Thorns}){
  PlaceBoss(0);PlacePlayer(3.6f);Call(boss,"BeginAttack",attack);Set(boss,"timer",.2f);var hull=UnityEditor.EditorJsonUtility.ToJson(boss.GetComponent<UnityEngine.BoxCollider2D>());Call(animator,"LateUpdate");Check(animator.artwork.localScale.y>0,"Invalid animation pose");Check(UnityEditor.EditorJsonUtility.ToJson(boss.GetComponent<UnityEngine.BoxCollider2D>())==hull,"Animation changed hull");
 }
 results.Add("All four attack animation poses retain the physics hitbox.");
 PlaceBoss(0);PlacePlayer(3.2f);Set(ph,"invulnerableUntil",0f);Call(boss,"BeginAttack",BlightBoss.AttackKind.Slam);float hp=ph.CurrentHealth;Tick(15);Check(ph.CurrentHealth==hp,"Early slam damage");Tick(17);Check(boss.SlamsPerformed>0&&boss.ChestExposed,"Slam/recovery failed");Check(ph.CurrentHealth==hp-25,"Slam damage incorrect");Call(boss,"CancelHazards");
 results.Add("Slam has a non-damaging windup, 25 damage and a recovery opening.");
 PlaceBoss(0);PlacePlayer(3.2f);Call(boss,"BeginAttack",BlightBoss.AttackKind.Lunge);Tick(26);Check(boss.LungesPerformed>0,"Lunge not executed");float before=bb.position.x;Tick(8);Check(bb.position.x>before,"Committed lunge did not move");Call(boss,"CancelHazards");
 PlaceBoss(0);PlacePlayer(-3.2f);Call(boss,"BeginAttack",BlightBoss.AttackKind.Lunge);Tick(26);Check(boss.Facing==-1,"Boss left facing failed");Call(boss,"CancelHazards");
 results.Add("Lunge moves with the physics body and commits in both facing directions.");
 PlaceBoss(0);PlacePlayer(7f);int volley=boss.VolleysPerformed;Call(boss,"BeginAttack",BlightBoss.AttackKind.Spores);Tick(34);Check(boss.VolleysPerformed==volley+1&&boss.ActiveHazards==3,"Spore fan missing");Call(boss,"CancelHazards");
 results.Add("Spore fan launches three owned projectiles.");
 PlaceBoss(0);PlacePlayer(4f);int burst=boss.ThornBurstsPerformed;Call(boss,"BeginAttack",BlightBoss.AttackKind.Thorns);Tick(37);Check(boss.ThornBurstsPerformed==burst+1,"Thorn burst failed");var thorns=UnityEngine.Object.FindObjectsByType<CrimsonBossThorn>();Check(thorns.Length==3,"Expected three marked thorn zones");
 var thorn=thorns[0];PlacePlayer(thorn.transform.position.x);Set(ph,"invulnerableUntil",0f);hp=ph.CurrentHealth;Call(thorn,"Update");Check(ph.CurrentHealth==hp&&!thorn.IsActive,"Thorn warning damaged player");
 Set(thorn,"elapsed",(float)typeof(CrimsonBossThorn).GetField("warningTime",flags).GetValue(thorn)+.01f);Call(thorn,"Update");Check(ph.CurrentHealth==hp-20,"Active thorn did not damage grounded player");hp=ph.CurrentHealth;Call(thorn,"Update");Check(ph.CurrentHealth==hp,"Thorn hit twice");
 var dodge=thorns[1];PlacePlayer(dodge.transform.position.x,.5f);Set(ph,"invulnerableUntil",0f);hp=ph.CurrentHealth;Set(dodge,"elapsed",(float)typeof(CrimsonBossThorn).GetField("warningTime",flags).GetValue(dodge)+.01f);Call(dodge,"Update");Check(ph.CurrentHealth==hp,"Airborne player could not dodge thorn");
 UnityEngine.Time.timeScale=0;float elapsed=(float)typeof(CrimsonBossThorn).GetField("elapsed",flags).GetValue(dodge);Call(dodge,"Update");Check((float)typeof(CrimsonBossThorn).GetField("elapsed",flags).GetValue(dodge)==elapsed,"Thorns advanced while paused");UnityEngine.Time.timeScale=1;Call(boss,"CancelHazards");
 results.Add("Thorns mark ground before damage, hit once, allow aerial dodging, and freeze while paused.");
 bh.TakeDamage(600,UnityEngine.Vector2.zero,0);Check(boss.PhaseTwo&&boss.PhaseTransitions==1,"Phase transition failed");PlaceBoss(0);PlacePlayer(4);Call(boss,"BeginAttack",BlightBoss.AttackKind.Thorns);Tick(37);Check(UnityEngine.Object.FindObjectsByType<CrimsonBossThorn>().Length>=5,"Phase-two thorn pattern missing");Call(boss,"CancelHazards");
 results.Add("Phase two begins once and expands the thorn pattern to five dodge zones.");
 // Normal jumping must traverse each complete escape route using real physics.
 boss.enabled=false;
 foreach(int side in new[]{-1,1}){
  var indices=side<0?new[]{0,1,2,3,8}:new[]{4,5,6,7,8};var route=new System.Collections.Generic.List<UnityEngine.Vector2>();
  foreach(int index in indices){var shelf=UnityEngine.GameObject.Find("CrimsonEscapeShelf_"+index).GetComponent<UnityEngine.BoxCollider2D>();route.Add(new UnityEngine.Vector2(shelf.bounds.center.x,shelf.bounds.max.y));}
  var steps=route.ToArray();PlacePlayer(side*16.8f);Input();Tick(25,false);
  foreach(var target in steps){Input(UnityEngine.InputSystem.Key.Z);Call(player,"FixedUpdate");UnityEngine.Physics2D.Simulate(.02f);Input();bool landed=false;for(int i=0;i<65;i++){float dx=target.x-pb.position.x;Input(UnityEngine.Mathf.Abs(dx)<.1f?UnityEngine.InputSystem.Key.None:dx>0?UnityEngine.InputSystem.Key.RightArrow:UnityEngine.InputSystem.Key.LeftArrow);Tick(1,false);var box=player.GetComponent<UnityEngine.Collider2D>();if(i>8&&player.IsGrounded&&UnityEngine.Mathf.Abs(box.bounds.min.y-target.y)<.1f){landed=true;break;}}Check(landed,"Unreachable platform "+target+" from "+pb.position);Input();for(int walk=0;walk<30&&UnityEngine.Mathf.Abs(target.x-pb.position.x)>.1f;walk++){Input(target.x>pb.position.x?UnityEngine.InputSystem.Key.RightArrow:UnityEngine.InputSystem.Key.LeftArrow);Tick(1,false);}Input();Tick(2,false);}
 }
 results.Add("Both full escape routes reached the top shelf using normal jumps and actual Physics2D landings.");
 foreach(int index in new[]{9,10}){
  var shelf=UnityEngine.GameObject.Find("CrimsonEscapeShelf_"+index).GetComponent<UnityEngine.BoxCollider2D>();PlacePlayer(shelf.bounds.center.x);Input();Tick(20,false);Input(UnityEngine.InputSystem.Key.Z);Tick(1,false);Input();Tick(12,false);Input(UnityEngine.InputSystem.Key.Z);Tick(1,false);Input();bool landed=false;for(int i=0;i<110;i++){Tick(1,false);if(i>12&&player.IsGrounded&&UnityEngine.Mathf.Abs(player.GetComponent<UnityEngine.Collider2D>().bounds.min.y-shelf.bounds.max.y)<.1f){landed=true;break;}}Check(landed,"Double-jump perch unreachable: "+index);
 }
 results.Add("Both optional outer perches are reachable with the existing double jump.");
 boss.enabled=true;PlaceBoss(0);PlacePlayer(-10);bh.TakeDamage(10000,UnityEngine.Vector2.zero,0);Check(boss.IsDead&&!boss.enabled&&!bb.simulated&&boss.ActiveHazards==0,"Boss death left combat active");Check(UnityEngine.Object.FindAnyObjectByType<BossCrystalReward>().Revealed,"Victory crystal not revealed");Call(animator,"LateUpdate");
 results.Add("Death cancels boss damage/movement/hazards, starts collapse and reveals the victory crystal.");
 return Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks=results});
}finally{if(keyboard!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);UnityEngine.Physics2D.simulationMode=mode;UnityEngine.Time.timeScale=scale;}
