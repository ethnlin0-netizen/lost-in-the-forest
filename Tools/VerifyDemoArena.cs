if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Start Play mode in EnemyAI first.");
var player=UnityEngine.GameObject.Find("Player"); var body=player.GetComponent<UnityEngine.Rigidbody2D>(); var movement=player.GetComponent<Movement>();
var camera=UnityEngine.Camera.main; var follow=camera.GetComponent<DemoCameraFollow>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var oldMode=UnityEngine.Physics2D.simulationMode;
var enemies=UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);foreach(var e in enemies)e.gameObject.SetActive(false);
var ground=UnityEngine.GameObject.Find("Ground").GetComponent<UnityEngine.BoxCollider2D>();
void Assert(bool ok,string message){if(!ok)throw new System.Exception(message);}
Assert(UnityEngine.Mathf.Abs(ground.bounds.size.x-64)<0.01f,"Ground should be 64 units wide.");
Assert(player.GetComponent<PlayerHealth>().MaximumHealth==300,"Player should have 300 max HP.");
Assert(follow!=null,"Missing following camera.");
var result=new System.Collections.Generic.List<string>();
try {
    UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;
    var platforms=UnityEngine.GameObject.Find("Platforms").transform;
    foreach(UnityEngine.Transform t in platforms) {
        var col=t.GetComponent<UnityEngine.BoxCollider2D>();
        float x=t.name=="Ground"?-28f:col.bounds.center.x;
        body.position=new UnityEngine.Vector2(x,col.bounds.max.y+1.2f);body.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();
        for(int i=0;i<100;i++){typeof(Movement).GetMethod("FixedUpdate",flags).Invoke(movement,null);UnityEngine.Physics2D.Simulate(0.02f);}
        typeof(Movement).GetMethod("FixedUpdate",flags).Invoke(movement,null);
        Assert(movement.IsGrounded && UnityEngine.Mathf.Abs(body.position.y-(col.bounds.max.y+0.8f))<0.08f,"Landing failed on "+t.name);
    }
    result.Add("All "+platforms.childCount+" demo ground/platform surfaces support the player.");
    var takeoff=UnityEngine.GameObject.Find("Platform 03 - Center Right").GetComponent<UnityEngine.BoxCollider2D>();
    var island=UnityEngine.GameObject.Find("Platform 05 - Upper Center").GetComponent<UnityEngine.BoxCollider2D>();
    typeof(Movement).GetField("horizontalInput",flags).SetValue(movement,0f);
    body.position=new UnityEngine.Vector2(island.bounds.min.x-0.6f,takeoff.bounds.max.y+0.81f);body.linearVelocity=UnityEngine.Vector2.zero;UnityEngine.Physics2D.SyncTransforms();
    for(int i=0;i<10;i++){typeof(Movement).GetMethod("FixedUpdate",flags).Invoke(movement,null);UnityEngine.Physics2D.Simulate(0.02f);}
    typeof(Movement).GetField("jumpRequested",flags).SetValue(movement,true);
    for(int i=0;i<17;i++){typeof(Movement).GetMethod("FixedUpdate",flags).Invoke(movement,null);UnityEngine.Physics2D.Simulate(0.02f);}
    typeof(Movement).GetField("jumpRequested",flags).SetValue(movement,true);
    for(int i=0;i<140;i++){
        float feet=player.GetComponent<UnityEngine.CapsuleCollider2D>().bounds.min.y;
        float input=feet>island.bounds.max.y+0.04f && body.position.x<island.bounds.min.x+0.8f?1f:0f;
        typeof(Movement).GetField("horizontalInput",flags).SetValue(movement,input);
        typeof(Movement).GetMethod("FixedUpdate",flags).Invoke(movement,null);UnityEngine.Physics2D.Simulate(0.02f);
    }
    Assert(movement.IsGrounded && UnityEngine.Mathf.Abs(body.position.y-island.bounds.max.y-0.8f)<0.08f,"Player cannot double jump onto the unreachable-enemy island: "+body.position);
    result.Add("Player can double jump from the center step onto the island enemies cannot reach.");
    foreach(float x in new float[]{-28,0,28}) {
        player.transform.position=new UnityEngine.Vector3(x,-2.7f,0);
        for(int i=0;i<500;i++)typeof(DemoCameraFollow).GetMethod("LateUpdate",flags).Invoke(follow,null);
        float half=camera.orthographicSize*camera.aspect;
        float expected=UnityEngine.Mathf.Clamp(x,-32+half,32-half);
        Assert(UnityEngine.Mathf.Abs(camera.transform.position.x-expected)<0.15f,"Camera did not follow/clamp at "+x);
        Assert(camera.transform.position.x-half>=-32.01f && camera.transform.position.x+half<=32.01f,"Camera showed beyond level edges.");
    }
    result.Add("Camera follows left, center and right positions and clamps to the ground edges.");
    var collususAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/Collusus.prefab");
    var settings=new UnityEditor.SerializedObject(collususAsset.GetComponent<BlightEnemy>());
    Assert(UnityEngine.Mathf.Abs(settings.FindProperty("movementSpeed").floatValue-1.5f)<0.01f,"Collusus speed should be 1.5.");
    Assert(UnityEngine.GameObject.Find("Demo Cover Wall").GetComponent<UnityEngine.BoxCollider2D>()!=null,"Missing cover collider.");
    result.Add("300 HP, slower Collusus and solid cover verified. Stop Play mode to restore scene state.");
    return result;
}finally{UnityEngine.Physics2D.simulationMode=oldMode;foreach(var e in enemies)if(e!=null)e.gameObject.SetActive(true);}
