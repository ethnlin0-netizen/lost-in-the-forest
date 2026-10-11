if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required.");
var results = new System.Collections.Generic.List<string>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Call(object o, string method, params object[] args) { o.GetType().GetMethod(method,flags).Invoke(o,args); }
void Set(object o, string field, object value) { o.GetType().GetField(field,flags).SetValue(o,value); }
void Assert(bool ok, string message) { if (!ok) throw new System.Exception(message); }
var player = UnityEngine.GameObject.Find("Player");
var movement = player.GetComponent<Movement>();
var combat = player.GetComponent<PlayerCombat>();
var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
var oldMode = UnityEngine.Physics2D.simulationMode;
var liveEnemies = UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);
foreach (var e in liveEnemies) e.gameObject.SetActive(false);
var temporary = new System.Collections.Generic.List<UnityEngine.GameObject>();
UnityEngine.InputSystem.Keyboard keyboard = null;
var playerHealth = player.GetComponent<PlayerHealth>();
Assert(playerHealth != null,"Missing player health component.");
float originalHealth = playerHealth.CurrentHealth;
float originalMaximumHealth = playerHealth.MaximumHealth;
// Keep combat fixtures in clear ground space rather than inside the low center platform.
const float combatOffset = -7f;
void PlacePlayer(float x) { playerBody.position = new UnityEngine.Vector2(x+combatOffset,-2.7f); playerBody.linearVelocity = UnityEngine.Vector2.zero; UnityEngine.Physics2D.SyncTransforms(); }
UnityEngine.GameObject Spawn(string name, float x=1f) {
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+name+".prefab");
    var go = UnityEngine.Object.Instantiate(asset,new UnityEngine.Vector3(x+combatOffset,-3.5f+asset.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f,0),UnityEngine.Quaternion.identity);
    temporary.Add(go); UnityEngine.Physics2D.SyncTransforms(); return go;
}
try {
    UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
    keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
    PlacePlayer(-10);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
    UnityEngine.InputSystem.InputSystem.Update(); Call(movement,"Update");
    for(int i=0;i<60;i++){ Call(movement,"FixedUpdate"); UnityEngine.Physics2D.Simulate(0.02f); }
    Call(movement,"FixedUpdate"); Assert(movement.IsGrounded,"Grounding failed.");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.RightArrow,UnityEngine.InputSystem.Key.Z));
    UnityEngine.InputSystem.InputSystem.Update(); Call(movement,"Update"); Call(movement,"FixedUpdate");
    Assert(UnityEngine.Mathf.Abs(playerBody.linearVelocity.x-6f)<0.01f && UnityEngine.Mathf.Abs(playerBody.linearVelocity.y-10f)<0.01f,"Movement or first jump changed.");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
    UnityEngine.InputSystem.InputSystem.Update(); Call(movement,"Update");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Z));
    UnityEngine.InputSystem.InputSystem.Update(); Call(movement,"Update"); Call(movement,"FixedUpdate");
    Assert(UnityEngine.Mathf.Abs(playerBody.linearVelocity.y-14f)<0.01f,"Double jump changed.");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
    UnityEngine.InputSystem.InputSystem.Update(); Call(movement,"Update");
    results.Add("Keyboard movement, 10-speed first jump and 14-speed double jump passed.");
    foreach(var name in new string[]{"Chomper","Spora","Collusus"}) {
        for(int attack=0;attack<3;attack++) {
            // The current player fires from Lenny's mouth. Place ranged targets
            // beyond that origin so the projectile travels into their collider.
            var enemy = Spawn(name,attack==2?3.5f:1f); var health = enemy.GetComponent<EnemyHealth>(); var ai = enemy.GetComponent<BlightEnemy>(); ai.enabled=false;
            PlacePlayer(0);
            var extra = new UnityEngine.GameObject("ExtraHitCollider"); extra.transform.SetParent(enemy.transform,false); extra.AddComponent<UnityEngine.BoxCollider2D>().size=UnityEngine.Vector2.one*0.4f;
            UnityEngine.Physics2D.SyncTransforms();
            float before = health.CurrentHealth;
            if(attack<2) Call(combat,"PerformMelee",attack==1);
            else {
                Call(combat,"FireProjectile");
                var projectiles=UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>(UnityEngine.FindObjectsSortMode.None);
                for(int step=0;step<30;step++) { foreach(var projectile in projectiles) if(projectile != null && projectile.gameObject.activeSelf) Call(projectile,"FixedUpdate"); UnityEngine.Physics2D.Simulate(0.02f); }
            }
            float expected=attack==1?30f:15f;
            Assert(UnityEngine.Mathf.Abs(health.CurrentHealth-(before-expected))<0.01f,name+" incorrect damage or duplicate hit: "+attack+" health="+health.CurrentHealth);
            var fill=enemy.transform.Find("HealthBar/Background/Inset/Fill").GetComponent<UnityEngine.RectTransform>();
            Assert(UnityEngine.Mathf.Abs(fill.anchorMax.x-health.CurrentHealth/health.MaximumHealth)<0.001f,"Health bar failed.");
            Call(health,"Update"); Assert(enemy.GetComponentsInChildren<UnityEngine.SpriteRenderer>()[0].color==UnityEngine.Color.white,"Hit feedback missing.");
            if(attack==1) {
                UnityEngine.Physics2D.Simulate(0.02f);
                float vx=enemy.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity.x;
                Assert(name=="Spora"?UnityEngine.Mathf.Abs(vx)<0.05f:vx>0.1f,"Knockback resistance failed: "+name);
            }
            health.TakeDamage(999,UnityEngine.Vector2.right,0);
            Assert(health.IsDead && !ai.enabled && ai.State==BlightEnemy.BehaviourState.Dead,"Death did not stop AI.");
            foreach(var c in enemy.GetComponentsInChildren<UnityEngine.Collider2D>()) Assert(!c.enabled,"Dead collider active.");
            Assert(!enemy.transform.Find("HealthBar").gameObject.activeSelf,"Dead HP bar visible.");
            int oldCount=ai.AttacksPerformed; Call(ai,"Update"); Assert(ai.AttacksPerformed==oldCount,"Dead enemy attacked.");
            UnityEngine.Object.DestroyImmediate(enemy);
        }
        results.Add(name+": melee, charged melee, projectile, duplicate colliders, health bar, hit flash, knockback and death passed.");
    }
    Set(playerHealth,"maximumHealth",1000f); typeof(PlayerHealth).GetProperty("CurrentHealth").SetValue(playerHealth,1000f);
    foreach(var name in new string[]{"Chomper","Spora","Collusus"}) {
        var enemy=Spawn(name); var ai=enemy.GetComponent<BlightEnemy>();
        Set(playerHealth,"invulnerableUntil",0f); // Independent outgoing-damage cases, outside the protection window.
        for(int i=0;i<10;i++)UnityEngine.Physics2D.Simulate(0.02f);
        PlacePlayer(name=="Spora"?4f:name=="Collusus"?2.5f:2.1f);
        float before=playerHealth.CurrentHealth;
        Call(ai,"Update"); Assert(ai.State==BlightEnemy.BehaviourState.Windup,name+" did not detect/wind up.");
        Set(ai,"stateUntil",UnityEngine.Time.time-1f); Call(ai,"Update");
        Assert(ai.AttacksPerformed==1 && ai.State==BlightEnemy.BehaviourState.Recovery,name+" attack/recovery failed.");
        if(name=="Spora") {
            var projectiles=UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>(UnityEngine.FindObjectsSortMode.None);
            Assert(projectiles.Length>0,"No spore projectile.");
            for(int step=0;step<60;step++){ foreach(var pr in projectiles) if(pr != null && pr.gameObject.activeSelf) Call(pr,"FixedUpdate"); UnityEngine.Physics2D.Simulate(0.02f); }
        }
        float expected=name=="Collusus"?25f:name=="Spora"?10f:12f;
        Assert(UnityEngine.Mathf.Abs(playerHealth.CurrentHealth-(before-expected))<0.01f,name+" outgoing damage interface failed.");
        var playerFill = player.transform.Find("HealthBar/Background/Inset/Fill").GetComponent<UnityEngine.RectTransform>();
        Assert(UnityEngine.Mathf.Abs(playerFill.anchorMax.x-playerHealth.CurrentHealth/playerHealth.MaximumHealth)<0.001f,"Player health bar did not update.");
        Set(ai,"stateUntil",UnityEngine.Time.time-1f); Call(ai,"Update"); Call(ai,"Update"); Assert(ai.State!=BlightEnemy.BehaviourState.Windup,"Cooldown failed.");
        Set(ai,"nextAttack",0f); Call(ai,"Update"); Assert(ai.State==BlightEnemy.BehaviourState.Windup,"Second windup failed.");
        PlacePlayer(20); Set(ai,"stateUntil",UnityEngine.Time.time-1f); Call(ai,"Update"); Assert(ai.AttacksPerformed==1,"Attack did not cancel when player escaped.");
        PlacePlayer(4);
        var wall=new UnityEngine.GameObject("TestWall"); temporary.Add(wall); wall.transform.position=new UnityEngine.Vector3(2+combatOffset,-2.5f,0); wall.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(0.4f,2);
        UnityEngine.Physics2D.SyncTransforms();
        bool canSee=(bool)typeof(BlightEnemy).GetMethod("CanSeePlayer",flags).Invoke(ai,null); Assert(!canSee,"Line of sight passed through wall.");
        if(name!="Spora") {
            float nearWall = 1.8f + combatOffset - enemy.GetComponent<UnityEngine.BoxCollider2D>().bounds.extents.x - 0.08f;
            enemy.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(nearWall,-2.8f); UnityEngine.Physics2D.SyncTransforms();
            Assert(!(bool)typeof(BlightEnemy).GetMethod("SafeToMove",flags).Invoke(ai,new object[]{1}),"Wall movement guard failed.");
            enemy.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(31.9f-enemy.GetComponent<UnityEngine.BoxCollider2D>().bounds.extents.x,-3.5f+enemy.GetComponent<UnityEngine.BoxCollider2D>().bounds.extents.y); UnityEngine.Physics2D.SyncTransforms();
            Assert(!(bool)typeof(BlightEnemy).GetMethod("SafeToMove",flags).Invoke(ai,new object[]{1}),"Platform edge guard failed.");
        }
        UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(enemy);
        results.Add(name+": detection, windup, attack, recovery, cooldown, cancellation and wall visibility passed.");
    }
    results.Add("All three enemy attacks damage the real PlayerHealth component and update its overhead bar.");
    PlacePlayer(0);
    var practice=new UnityEngine.GameObject("TestPracticeTarget"); temporary.Add(practice); practice.transform.position=new UnityEngine.Vector3(1+combatOffset,-2.7f,0);
    var practiceBody=practice.AddComponent<UnityEngine.Rigidbody2D>(); practiceBody.gravityScale=0;
    var sprite=practice.AddComponent<UnityEngine.SpriteRenderer>(); sprite.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/PlaceholderSquare.png"); sprite.color=UnityEngine.Color.red;
    practice.AddComponent<UnityEngine.BoxCollider2D>(); practice.AddComponent<PracticeTarget>(); UnityEngine.Physics2D.SyncTransforms();
    Call(combat,"PerformMelee",false); Assert(sprite.color==UnityEngine.Color.white,"Practice melee changed.");
    Call(combat,"PerformMelee",true); UnityEngine.Physics2D.Simulate(0.02f); Assert(sprite.color==UnityEngine.Color.yellow && practiceBody.linearVelocity.x>0,"Practice charged attack changed.");
    results.Add("Existing practice-target flash and charged knockback passed.");
    return results;
} finally {
    if(keyboard!=null) UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    if(playerHealth!=null) {
        Set(playerHealth,"maximumHealth",originalMaximumHealth);
        typeof(PlayerHealth).GetProperty("CurrentHealth").SetValue(playerHealth,originalHealth);
        Set(playerHealth,"knockbackSpeed",0f);
        Call(player.GetComponentInChildren<PlayerHealthBar>(),"Refresh");
    }
    UnityEngine.Physics2D.simulationMode=oldMode;
    foreach(var go in temporary) if(go!=null) UnityEngine.Object.DestroyImmediate(go);
    foreach(var e in liveEnemies) if(e!=null)e.gameObject.SetActive(true);
}
