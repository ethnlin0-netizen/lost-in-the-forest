if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required.");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Assert(bool condition,string message){if(!condition)throw new System.Exception(message);}
var results=new System.Collections.Generic.List<string>();
var world=UnityEngine.Object.FindFirstObjectByType<PlatformNavigation2D>();
var live=UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);
foreach(var enemy in live)enemy.gameObject.SetActive(false);
var player=UnityEngine.GameObject.Find("Player");var pb=player.GetComponent<UnityEngine.Rigidbody2D>();var previousSimulation=UnityEngine.Physics2D.simulationMode;
var temporary=new System.Collections.Generic.List<UnityEngine.GameObject>();
UnityEngine.GameObject Spawn(string name,UnityEngine.Vector2 feet){
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+name+".prefab");
    var actor=UnityEngine.Object.Instantiate(prefab);temporary.Add(actor);actor.GetComponent<BlightEnemy>().enabled=false;
    var body=actor.GetComponent<UnityEngine.Rigidbody2D>();body.position=feet+UnityEngine.Vector2.up*(actor.GetComponent<UnityEngine.BoxCollider2D>().size.y*0.5f+0.01f);
    UnityEngine.Physics2D.SyncTransforms();for(int i=0;i<30;i++)UnityEngine.Physics2D.Simulate(0.02f);return actor;
}
void Run(UnityEngine.GameObject actor,UnityEngine.Vector2 goal,float speed,int steps){
    var nav=actor.GetComponent<EnemyNavigator2D>();var body=actor.GetComponent<UnityEngine.Rigidbody2D>();nav.SetDestination(goal);
    for(int i=0;i<steps;i++){float vx=nav.Tick(0.02f,speed);body.linearVelocity=new UnityEngine.Vector2(vx,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(0.02f);}
}
try{
    UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;pb.position=new UnityEngine.Vector2(-30,-2.7f);pb.linearVelocity=UnityEngine.Vector2.zero;world.RebuildSurfaces();
    var left=UnityEngine.GameObject.Find("Platform 01 - Left Step").GetComponent<UnityEngine.BoxCollider2D>();
    var next=UnityEngine.GameObject.Find("Platform 02 - Center Left").GetComponent<UnityEngine.BoxCollider2D>();
    var upper=UnityEngine.GameObject.Find("Platform 06 - Upper Left").GetComponent<UnityEngine.BoxCollider2D>();
    var chomper=Spawn("Chomper",new UnityEngine.Vector2(-18,-3.5f));var cn=chomper.GetComponent<EnemyNavigator2D>();
    Run(chomper,new UnityEngine.Vector2(-23,left.bounds.max.y),4.5f,700);
    Assert(world.SupportAt(cn.Feet)==left && cn.JumpsPerformed>0,"Chomper failed first jump: "+cn.Feet+" reachable="+cn.GoalReachable+" jumps="+cn.JumpsPerformed+" links="+cn.RemainingLinks);
    Run(chomper,new UnityEngine.Vector2(-15,next.bounds.max.y),4.5f,800);
    Assert(world.SupportAt(cn.Feet)==next,"Chomper failed second platform: "+cn.Feet);
    Run(chomper,new UnityEngine.Vector2(-10,upper.bounds.max.y),4.5f,800);
    Assert(world.SupportAt(cn.Feet)==upper,"Chomper failed upper platform: "+cn.Feet);
    results.Add("Chomper physically climbs all three left steps with validated jumps.");
    int drops=cn.DropsPerformed;Run(chomper,new UnityEngine.Vector2(-5,-3.5f),4.5f,900);
    Assert(cn.IsGrounded && UnityEngine.Mathf.Abs(cn.Feet.x+5)<0.4f && cn.DropsPerformed>drops,"Safe drop failed: "+cn.Feet+" drops="+cn.DropsPerformed);
    results.Add("Chomper safely drops back to the ground and walks to the destination.");
    var island=UnityEngine.GameObject.Find("Platform 05 - Upper Center").GetComponent<UnityEngine.BoxCollider2D>();
    int before=cn.JumpsPerformed;Run(chomper,new UnityEngine.Vector2(3,island.bounds.max.y),4.5f,1000);
    Assert(!cn.GoalReachable && cn.IsGrounded,"Unreachable island was treated as reachable.");
    var wait=cn.Feet;before=cn.JumpsPerformed;Run(chomper,new UnityEngine.Vector2(3,island.bounds.max.y),4.5f,300);
    Assert(UnityEngine.Vector2.Distance(wait,cn.Feet)<0.15f && cn.JumpsPerformed==before,"Unreachable target caused jitter/repeated jumping.");
    results.Add("Unreachable island selects a reachable fallback and waits without repeated jumps.");
    UnityEngine.Object.DestroyImmediate(chomper);
    var collusus=Spawn("Collusus",new UnityEngine.Vector2(28,-3.5f));var rn=collusus.GetComponent<EnemyNavigator2D>();
    var rightUpper=UnityEngine.GameObject.Find("Navigation Step - Collusus Upper").GetComponent<UnityEngine.BoxCollider2D>();
    var rightLower=UnityEngine.GameObject.Find("Platform 04 - Right Step").GetComponent<UnityEngine.BoxCollider2D>();
    Run(collusus,new UnityEngine.Vector2(24.3f,rightUpper.bounds.max.y),1.5f,1000);
    Assert(world.SupportAt(rn.Feet)==rightUpper && rn.JumpsPerformed>0,"Collusus failed upper step: "+rn.Feet+" reachable="+rn.GoalReachable+" jumps="+rn.JumpsPerformed+" links="+rn.RemainingLinks);
    Run(collusus,new UnityEngine.Vector2(20,rightLower.bounds.max.y),1.5f,1000);
    Assert(world.SupportAt(rn.Feet)==rightLower,"Collusus failed descending route: "+rn.Feet);
    Run(collusus,new UnityEngine.Vector2(16,-3.5f),1.5f,1000);
    Assert(rn.IsGrounded && UnityEngine.Mathf.Abs(rn.Feet.x-16)<0.4f,"Collusus failed return to ground: "+rn.Feet);
    results.Add("Collusus climbs and descends the right steps while keeping ground chase speed at 1.5.");
    UnityEngine.Object.DestroyImmediate(collusus);
    var around=Spawn("Chomper",new UnityEngine.Vector2(8,-3.5f));var an=around.GetComponent<EnemyNavigator2D>();
    Run(around,new UnityEngine.Vector2(15,-3.5f),4.5f,1200);
    Assert(an.IsGrounded && UnityEngine.Mathf.Abs(an.Feet.x-15)<0.4f,"Cover route failed: "+an.Feet+" reachable="+an.GoalReachable);
    results.Add("Chomper routes over the solid cover using reachable stepping surfaces.");
    // Add a new wall after the route exists; revision changes must invalidate cached links.
    an.Stop();around.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(15,-2.84f);UnityEngine.Physics2D.SyncTransforms();
    Run(around,new UnityEngine.Vector2(19,rightLower.bounds.max.y),4.5f,5);
    var blocker=new UnityEngine.GameObject("Navigation regression blocker");temporary.Add(blocker);blocker.transform.SetParent(world.transform);blocker.transform.position=new UnityEngine.Vector3(17,-0.5f,0);blocker.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(0.5f,6f);
    UnityEngine.Physics2D.SyncTransforms();world.RebuildSurfaces();Run(around,new UnityEngine.Vector2(19,rightLower.bounds.max.y),4.5f,500);
    Assert(an.Feet.x<16.8f && !an.GoalReachable,"Invalidated route passed through new wall: "+an.Feet+" reachable="+an.GoalReachable);
    UnityEngine.Object.DestroyImmediate(blocker);world.RebuildSurfaces();Run(around,new UnityEngine.Vector2(19,rightLower.bounds.max.y),4.5f,800);
    Assert(UnityEngine.Mathf.Abs(an.Feet.x-19)<0.4f,"Route did not recover after wall removal: "+an.Feet);
    results.Add("Geometry changes invalidate blocked routes; pursuit recovers after the obstruction is removed.");
    an.Stop();around.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(14,-2.84f);UnityEngine.Physics2D.SyncTransforms();
    var actorBody=around.GetComponent<UnityEngine.Rigidbody2D>();var constraints=actorBody.constraints;
    actorBody.constraints=constraints|UnityEngine.RigidbodyConstraints2D.FreezePositionX;
    int stuckBefore=an.StuckRecoveries;Run(around,new UnityEngine.Vector2(16,-3.5f),4.5f,150);
    Assert(an.StuckRecoveries>stuckBefore,"Stuck enemy did not invalidate its route.");
    actorBody.constraints=constraints;Run(around,new UnityEngine.Vector2(16,-3.5f),4.5f,300);
    Assert(UnityEngine.Mathf.Abs(an.Feet.x-16)<0.4f,"Stuck enemy did not resume pursuit.");
    results.Add("An immobilized enemy detects lack of progress and resumes after it is released.");
    Assert(an.RoutesCalculated<250,"Routes recalculated too frequently.");
    var enemyHealth=around.GetComponent<EnemyHealth>();enemyHealth.TakeDamage(999,UnityEngine.Vector2.zero,0);
    Assert(!an.IsTraversing && an.RemainingLinks==0 && !around.GetComponent<BlightEnemy>().enabled,"Death left navigation active.");
    results.Add("Route refresh is throttled and death cancels traversal immediately.");
    return results;
}finally{UnityEngine.Physics2D.simulationMode=previousSimulation;foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);foreach(var enemy in live)if(enemy!=null)enemy.gameObject.SetActive(true);world.RebuildSurfaces();}


