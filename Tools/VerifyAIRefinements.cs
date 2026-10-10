if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Call(object o,string method){o.GetType().GetMethod(method,flags).Invoke(o,null);}
void Set(object o,string field,object value){o.GetType().GetField(field,flags).SetValue(o,value);}
void Assert(bool condition,string message){if(!condition)throw new System.Exception(message);}
var world=UnityEngine.Object.FindFirstObjectByType<PlatformNavigation2D>();
var live=UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);
foreach(var e in live){
    var bounds=e.GetComponent<UnityEngine.BoxCollider2D>().bounds;
    foreach(var platform in world.Surfaces)
        Assert(!bounds.Intersects(platform.bounds),e.name+" spawn overlaps "+platform.name);
    e.gameObject.SetActive(false);
}
var player=UnityEngine.GameObject.Find("Player");var pb=player.GetComponent<UnityEngine.Rigidbody2D>();var constraints=pb.constraints;
var oldMode=UnityEngine.Physics2D.simulationMode;var temporary=new System.Collections.Generic.List<UnityEngine.GameObject>();var results=new System.Collections.Generic.List<string>();
void Player(float x){pb.position=new UnityEngine.Vector2(x,-2.69f);pb.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();}
UnityEngine.GameObject Spawn(string name,float x){
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+name+".prefab");
    var go=UnityEngine.Object.Instantiate(prefab,new UnityEngine.Vector3(x,-3.49f+prefab.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f,0),UnityEngine.Quaternion.identity);
    temporary.Add(go);go.GetComponent<BlightEnemy>().enabled=false;UnityEngine.Physics2D.SyncTransforms();
    for(int i=0;i<10;i++)UnityEngine.Physics2D.Simulate(0.02f);return go;
}
void Tick(BlightEnemy ai){Call(ai,"Update");Call(ai,"FixedUpdate");UnityEngine.Physics2D.Simulate(0.02f);}
try{
    UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;pb.constraints|=UnityEngine.RigidbodyConstraints2D.FreezePosition;Player(-30);
    var walker=Spawn("Chomper",-18);var nav=walker.GetComponent<EnemyNavigator2D>();var body=walker.GetComponent<UnityEngine.Rigidbody2D>();
    nav.SetDestination(new UnityEngine.Vector2(-3,-3.5f));int pauses=0;
    for(int i=0;i<220;i++){
        bool stillTravelling=UnityEngine.Mathf.Abs(nav.Feet.x+3)>0.35f;float vx=nav.Tick(0.02f,4.5f);
        if(stillTravelling && UnityEngine.Mathf.Abs(vx)<0.01f)pauses++;
        body.linearVelocity=new UnityEngine.Vector2(vx,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(0.02f);
    }
    Assert(pauses==0 && UnityEngine.Mathf.Abs(nav.Feet.x+3)<0.3f,"Walking still pauses between waypoints: "+pauses);
    int planned=nav.RoutesCalculated;
    for(int i=0;i<300;i++){float vx=nav.Tick(0.02f,4.5f);body.linearVelocity=new UnityEngine.Vector2(vx,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(0.02f);}
    Assert(nav.RoutesCalculated==planned,"Stationary destination repeatedly replanned.");
    body.position=new UnityEngine.Vector2(-5,-3.36f+walker.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f);body.linearVelocity=UnityEngine.Vector2.down;UnityEngine.Physics2D.SyncTransforms();
    Assert(!nav.IsGrounded,"Enemy counted as grounded above the floor.");
    body.position=new UnityEngine.Vector2(-5,-3.488f+walker.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f);body.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();
    Assert(nav.IsGrounded,"Standing enemy not grounded.");UnityEngine.Object.DestroyImmediate(walker);
    results.Add("Continuous 15-unit walk has zero waypoint pauses; stationary routes are cached; ground contact excludes hovering actors.");
    Player(-3);var pursuer=Spawn("Chomper",-8);var brain=pursuer.GetComponent<BlightEnemy>();var rb=pursuer.GetComponent<UnityEngine.Rigidbody2D>();
    Set(brain,"nextAttack",UnityEngine.Time.time+100f);Tick(brain);
    Assert(rb.linearVelocity.x>0 && rb.linearVelocity.x<4.5f,"Ground pursuit did not accelerate smoothly.");
    for(int i=0;i<100;i++)Tick(brain);
    float separation=pb.position.x-rb.position.x;
    Assert(separation>=1.27f && separation<=1.52f,"Melee stop distance failed: "+separation);
    foreach(float distance in new[]{1.48f,1.6f,1.52f,1.65f,1.45f}){
        Player(rb.position.x+distance);Tick(brain);Assert(UnityEngine.Mathf.Abs(rb.linearVelocity.x)<0.01f,"Small range changes caused movement jitter.");
    }
    Player(rb.position.x+2f);Tick(brain);Assert(rb.linearVelocity.x>0,"Pursuit did not resume outside the spacing buffer.");
    UnityEngine.Object.DestroyImmediate(pursuer);results.Add("Pursuit accelerates, holds fair melee spacing without range-boundary jitter, and resumes when the player moves away.");
    foreach(string name in new[]{"Chomper","Spora","Collusus"}){
        float distance=name=="Chomper"?1.2f:name=="Spora"?3f:2f;Player(-7+distance);
        var actor=Spawn(name,-7);var ai=actor.GetComponent<BlightEnemy>();Call(ai,"Update");
        Assert(ai.State==BlightEnemy.BehaviourState.Windup,name+" did not wind up.");
        var cue=(UnityEngine.GameObject)ai.GetType().GetField("attackVisual",flags).GetValue(ai);var sr=cue.GetComponentInChildren<UnityEngine.SpriteRenderer>();
        Assert(cue.activeSelf && sr.color.g>0.7f,name+" missing early windup cue.");
        Player(-7-distance);Call(ai,"Update");Assert((int)ai.GetType().GetField("facing",flags).GetValue(ai)==-1,name+" cannot track during early windup.");
        Player(-7+distance);var settings=new UnityEditor.SerializedObject(ai);float windup=settings.FindProperty("windup").floatValue;float fraction=settings.FindProperty("attackDirectionLockFraction").floatValue;
        Set(ai,"windupStarted",UnityEngine.Time.time-windup*fraction-0.01f);Call(ai,"Update");
        Assert((int)ai.GetType().GetField("facing",flags).GetValue(ai)==-1 && sr.color.g<0.5f,name+" late frame retargeted a committed attack or did not change cue.");
        int attacks=ai.AttacksPerformed;float hp=player.GetComponent<PlayerHealth>().CurrentHealth;Set(ai,"stateUntil",UnityEngine.Time.time-1f);Call(ai,"Update");
        Assert(ai.AttacksPerformed==attacks && player.GetComponent<PlayerHealth>().CurrentHealth==hp,name+" committed attack hit behind its aim.");
        Assert(ai.State==BlightEnemy.BehaviourState.Recovery && !cue.activeSelf,name+" recovery still advertises a damaging attack.");
        UnityEngine.Object.DestroyImmediate(actor);
    }
    results.Add("All three windups track early, lock without late retargeting, change yellow to orange, miss behind their aim, and clear cues during recovery.");
    Player(-5.8f);var reacting=Spawn("Chomper",-7);var reactAI=reacting.GetComponent<BlightEnemy>();Call(reactAI,"Update");Assert(reactAI.State==BlightEnemy.BehaviourState.Windup,"Hit-reaction setup failed.");
    var health=reacting.GetComponent<EnemyHealth>();health.TakeDamage(15,UnityEngine.Vector2.left,6);Call(reactAI,"Update");Call(reactAI,"FixedUpdate");UnityEngine.Physics2D.Simulate(0.02f);
    Assert(reactAI.State==BlightEnemy.BehaviourState.Recovery && reactAI.AttacksPerformed==0,"Hit did not interrupt windup.");
    Assert(reacting.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity.x<0,"AI erased charged knockback during hit reaction.");
    UnityEngine.Object.DestroyImmediate(reacting);results.Add("Player damage interrupts the pending attack while retaining the knockback impulse.");
    Player(-30);
    foreach(string name in new[]{"Chomper","Collusus"}){
        var actor=Spawn(name,-7);var ai=actor.GetComponent<BlightEnemy>();var rbPatrol=actor.GetComponent<UnityEngine.Rigidbody2D>();
        float radius=name=="Chomper"?3f:2f;int turns=0;int facing=1;
        for(int i=0;i<500;i++){
            Tick(ai);int now=(int)ai.GetType().GetField("facing",flags).GetValue(ai);if(now!=facing)turns++;facing=now;
            Assert(rbPatrol.position.x>=-7-radius-0.08f && rbPatrol.position.x<=-7+radius+0.08f,name+" wandered outside its patrol zone.");
        }
        Assert(turns>=2,name+" patrol never reversed.");UnityEngine.Object.DestroyImmediate(actor);
    }
    results.Add("Chomper and Collusus patrol within their local zones and reverse cleanly; all scene enemy spawns are clear of terrain.");
    return results;
}finally{pb.constraints=constraints;UnityEngine.Physics2D.simulationMode=oldMode;foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);foreach(var e in live)if(e!=null)e.gameObject.SetActive(true);}
