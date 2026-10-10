if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Call(object o,string name){o.GetType().GetMethod(name,flags).Invoke(o,null);}
void Set(object o,string name,object value){o.GetType().GetField(name,flags).SetValue(o,value);}
void Assert(bool value,string message){if(!value)throw new System.Exception(message);}
var player=UnityEngine.GameObject.Find("Player");var pb=player.GetComponent<UnityEngine.Rigidbody2D>();var ph=player.GetComponent<PlayerHealth>();
var live=UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);foreach(var e in live)e.gameObject.SetActive(false);
var oldMode=UnityEngine.Physics2D.simulationMode;var temporary=new System.Collections.Generic.List<UnityEngine.GameObject>();var results=new System.Collections.Generic.List<string>();
void Player(UnityEngine.Vector2 feet){pb.position=feet+UnityEngine.Vector2.up*0.81f;pb.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
UnityEngine.GameObject Enemy(string name,float x){
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+name+".prefab");var go=UnityEngine.Object.Instantiate(prefab);temporary.Add(go);
    var b=go.GetComponent<UnityEngine.Rigidbody2D>();b.position=new UnityEngine.Vector2(x,-3.49f+go.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f);UnityEngine.Physics2D.SyncTransforms();return go;
}
try{
    UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;Player(new UnityEngine.Vector2(-30,-3.5f));
    var chomper=Enemy("Chomper",-18);var ai=chomper.GetComponent<BlightEnemy>();var cb=chomper.GetComponent<UnityEngine.Rigidbody2D>();var nav=chomper.GetComponent<EnemyNavigator2D>();
    var left=UnityEngine.GameObject.Find("Platform 01 - Left Step").GetComponent<UnityEngine.BoxCollider2D>();
    for(int i=0;i<10;i++)UnityEngine.Physics2D.Simulate(0.02f);
    Player(new UnityEngine.Vector2(-23,left.bounds.max.y));pb.constraints|=UnityEngine.RigidbodyConstraints2D.FreezePosition;
    for(int i=0;i<400;i++){Call(ai,"Update");Call(ai,"FixedUpdate");UnityEngine.Physics2D.Simulate(0.02f);}
    Assert(nav.JumpsPerformed>0 && UnityEngine.Object.FindFirstObjectByType<PlatformNavigation2D>().SupportAt(nav.Feet)==left,"AI did not pursue player onto a different platform.");
    Assert(ai.State==BlightEnemy.BehaviourState.Windup,"Pursuit did not transition into attack windup.");
    UnityEngine.Object.DestroyImmediate(chomper);results.Add("Real Chomper controller detects a player on another platform, follows a route, then winds up in range.");
    var right=UnityEngine.GameObject.Find("Navigation Step - Collusus Upper").GetComponent<UnityEngine.BoxCollider2D>();
    Player(new UnityEngine.Vector2(24.3f,right.bounds.max.y));
    var rock=Enemy("Collusus",28);var rockAI=rock.GetComponent<BlightEnemy>();var rockNav=rock.GetComponent<EnemyNavigator2D>();
    for(int i=0;i<700;i++){Call(rockAI,"Update");Call(rockAI,"FixedUpdate");UnityEngine.Physics2D.Simulate(0.02f);}
    Assert(rockNav.JumpsPerformed>0 && UnityEngine.Object.FindFirstObjectByType<PlatformNavigation2D>().SupportAt(rockNav.Feet)==right && rockAI.State==BlightEnemy.BehaviourState.Windup,"Collusus controller did not navigate into slam range.");
    UnityEngine.Object.DestroyImmediate(rock);results.Add("Real Collusus controller follows a player onto the right step, then winds up its slam.");
    pb.constraints=UnityEngine.RigidbodyConstraints2D.FreezeRotation;
    foreach(string name in new[]{"Chomper","Spora","Collusus"}){
        Player(new UnityEngine.Vector2(3,-3.5f));var enemy=Enemy(name,1);var brain=enemy.GetComponent<BlightEnemy>();
        for(int i=0;i<10;i++)UnityEngine.Physics2D.Simulate(0.02f);
        Player(new UnityEngine.Vector2(name=="Chomper"?2.2f:3f,-3.5f));Set(ph,"invulnerableUntil",0f);
        Call(brain,"Update");Assert(brain.State==BlightEnemy.BehaviourState.Windup,name+" no windup.");
        var cue=(UnityEngine.GameObject)brain.GetType().GetField("attackVisual",flags).GetValue(brain);
        Assert(cue!=null && cue.activeSelf,name+" no visible windup cue.");
        int attacks=brain.AttacksPerformed;Set(brain,"windupStarted",UnityEngine.Time.time-10f);Call(brain,"Update");
        Assert((bool)brain.GetType().GetField("attackDirectionLocked",flags).GetValue(brain),name+" did not lock direction.");
        int locked=(int)brain.GetType().GetField("facing",flags).GetValue(brain);
        Player(new UnityEngine.Vector2(0,-3.5f));float before=ph.CurrentHealth;
        Set(brain,"stateUntil",UnityEngine.Time.time-1f);Call(brain,"Update");
        Assert(ph.CurrentHealth==before && brain.AttacksPerformed==attacks && (int)brain.GetType().GetField("facing",flags).GetValue(brain)==locked,name+" attack tracked the player after lock.");
        Assert(brain.State==BlightEnemy.BehaviourState.Recovery,name+" no recovery.");
        Call(brain,"Update");Assert(brain.State==BlightEnemy.BehaviourState.Recovery,name+" recovery vanished immediately.");
        UnityEngine.Object.DestroyImmediate(enemy);
    }
    results.Add("All three attacks show windup, lock direction, miss a player who dodges behind, and retain recovery.");
    Player(new UnityEngine.Vector2(4,-3.5f));var spora=Enemy("Spora",1);var sa=spora.GetComponent<BlightEnemy>();
    var wall=new UnityEngine.GameObject("Fair-combat wall");temporary.Add(wall);wall.transform.position=new UnityEngine.Vector3(2.5f,-2,0);wall.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(0.5f,3);
    UnityEngine.Physics2D.SyncTransforms();Call(sa,"Update");Assert(sa.State!=BlightEnemy.BehaviourState.Windup,"Spora fired through cover.");UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(spora);
    results.Add("Spora cannot start firing through a solid wall.");
    float hp=ph.CurrentHealth;Set(ph,"invulnerableUntil",0f);ph.TakeDamage(12,UnityEngine.Vector2.right,1);ph.TakeDamage(25,UnityEngine.Vector2.left,5);ph.TakeDamage(10,UnityEngine.Vector2.left,0);
    Assert(ph.CurrentHealth==hp-12f && ph.IsInvulnerable,"Overlapping bites/slams/spores stacked damage.");
    Call(ph,"Update");var hs=new UnityEditor.SerializedObject(ph);var sr=(UnityEngine.SpriteRenderer)hs.FindProperty("bodyVisual").objectReferenceValue;
    Assert(sr.color==UnityEngine.Color.red,"Damage protection had no immediate feedback.");
    Set(ph,"invulnerableUntil",UnityEngine.Time.time-1f);ph.TakeDamage(10,UnityEngine.Vector2.zero,0);Assert(ph.CurrentHealth==hp-22f,"Damage did not resume after protection expired.");
    results.Add("Overlapping damage applies one hit, shows feedback, and resumes after invulnerability expires.");
    return results;
}finally{pb.constraints=UnityEngine.RigidbodyConstraints2D.FreezeRotation;UnityEngine.Physics2D.simulationMode=oldMode;foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);foreach(var enemy in live)if(enemy!=null)enemy.gameObject.SetActive(true);}
