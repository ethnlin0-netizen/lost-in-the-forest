if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use temporary Boss Play mode.");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
void Call(object o,string m,params object[] args){o.GetType().GetMethod(m,flags).Invoke(o,args);}
void Set(object o,string f,object v){o.GetType().GetField(f,flags).SetValue(o,v);}
void Check(bool test,string message){if(!test)throw new System.Exception(message);}
var boss=UnityEngine.Object.FindAnyObjectByType<BlightBoss>();var melee=boss.GetComponent<CrimsonBossMelee>();var effects=boss.GetComponent<CrimsonBossEffects>();var player=UnityEngine.Object.FindAnyObjectByType<Movement>();var health=player.GetComponent<PlayerHealth>();var body=player.GetComponent<UnityEngine.Rigidbody2D>();var hull=player.GetComponent<UnityEngine.Collider2D>();var bb=boss.GetComponent<UnityEngine.Rigidbody2D>();
var scale=UnityEngine.Time.timeScale;
void Place(float x,float height=0){float offset=body.position.y-hull.bounds.min.y;body.position=new UnityEngine.Vector2(x,boss.Feet.y+offset+height);body.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
try{
 UnityEngine.Time.timeScale=1;bb.position=new UnityEngine.Vector2(0,-2.48f);bb.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();
 typeof(PlayerHealth).GetProperty("CurrentHealth").SetValue(health,1000f);Set(health,"invulnerableUntil",0f);
 Check(effects!=null&&effects.effectMaterial!=null&&melee!=null,"Scene components missing");
 Place(2.6f);float hp=health.CurrentHealth;Call(melee,"Update");Check(health.CurrentHealth==hp,"Swipe warning dealt damage");
 Set(melee,"warning",0f);Call(melee,"Update");Check(health.CurrentHealth==hp-14,"Swipe did not hit nearby player");
 hp=health.CurrentHealth;Set(health,"invulnerableUntil",0f);Call(melee,"Update");Check(health.CurrentHealth==hp,"Melee repeated without cooldown");
 Set(melee,"remaining",0f);Call(melee,"Update");Place(8);Set(melee,"warning",0f);Call(melee,"Update");Check(health.CurrentHealth==hp,"Swipe hit player after dodge");
 Place(-2.6f);Set(melee,"remaining",0f);Call(melee,"Update");Set(melee,"warning",0f);Call(melee,"Update");Check(health.CurrentHealth==hp-14,"Left-side melee failed");
 hp=health.CurrentHealth;Set(health,"invulnerableUntil",0f);Set(melee,"remaining",0f);Call(melee,"Update");Place(-2.6f,3);Set(melee,"warning",0f);Call(melee,"Update");Check(health.CurrentHealth==hp,"Swipe hit airborne player");
 Place(2.6f);Set(melee,"remaining",0f);Call(melee,"Update");UnityEngine.Time.timeScale=0;float warning=(float)typeof(CrimsonBossMelee).GetField("warning",flags).GetValue(melee);Call(melee,"Update");Check(warning==(float)typeof(CrimsonBossMelee).GetField("warning",flags).GetValue(melee),"Melee advanced during pause");UnityEngine.Time.timeScale=1;
 foreach(var attack in new[]{BlightBoss.AttackKind.Slam,BlightBoss.AttackKind.Lunge,BlightBoss.AttackKind.Spores,BlightBoss.AttackKind.Thorns})Call(effects,"Burst",attack);
 Check(UnityEngine.Object.FindObjectsByType<CrimsonBossRibbon>().Length>=8,"Distinct attack ribbons missing");
 var hazards=UnityEngine.Object.FindObjectsByType<BlightBossHazard>();foreach(var h in hazards)h.Cancel();
 boss.GetComponent<BlightBossHealth>().TakeDamage(10000,UnityEngine.Vector2.zero,0);Call(melee,"Update");Check(!(bool)typeof(CrimsonBossMelee).GetField("pending",flags).GetValue(melee),"Dead boss left melee pending");
 return "PASS: 14 damage swipes on either side; warning has no damage; cooldown prevents repeated hits; running/jumping dodge; pause freezes timing; death cancels pending melee; four distinct ribbon effects spawn.";
}finally{UnityEngine.Time.timeScale=scale;}
