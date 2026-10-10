// Run as an Editor eval statement block with EnemyAI in Play Mode. Stop Play to restore the scene.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("EnemyAI Play Mode required.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Call(object target,string name) { target.GetType().GetMethod(name,flags).Invoke(target,null); }
void Set(object target,string name,object value) { target.GetType().GetField(name,flags).SetValue(target,value); }
void Assert(bool condition,string message) { if (!condition) throw new System.Exception(message); }
float Difference(UnityEngine.Vector3[] a,UnityEngine.Vector3[] b) {
    float value=0; for(int i=0;i<a.Length;i++) value=UnityEngine.Mathf.Max(value,(a[i]-b[i]).magnitude); return value;
}
var originals = UnityEngine.Object.FindObjectsByType<BlightEnemy>();
foreach(var enemy in originals) enemy.gameObject.SetActive(false);
var oldPlayer = UnityEngine.Object.FindAnyObjectByType<Movement>();
oldPlayer.gameObject.SetActive(false);
var player = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab"));
var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
playerBody.constraints = UnityEngine.RigidbodyConstraints2D.FreezeAll;
player.GetComponent<PlayerCombat>().enabled=false;
var playerHealth = player.GetComponent<PlayerHealth>();
var previousSimulation = UnityEngine.Physics2D.simulationMode;
var temporary = new System.Collections.Generic.List<UnityEngine.GameObject>();
var reports = new System.Collections.Generic.List<string>();
void PlayerAt(float x) {
    playerBody.position=new UnityEngine.Vector2(x,-2.69f);
    Set(playerHealth,"invulnerableUntil",float.NegativeInfinity);
    UnityEngine.Physics2D.SyncTransforms();
}
UnityEngine.GameObject Spawn(string name) {
    var go=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+name+".prefab"));
    temporary.Add(go);
    var collider=go.GetComponent<UnityEngine.BoxCollider2D>();
    go.GetComponent<UnityEngine.Rigidbody2D>().position=new UnityEngine.Vector2(1,-3.5f+collider.size.y*.5f+.01f);
    UnityEngine.Physics2D.SyncTransforms();
    for(int frame=0;frame<10;frame++) UnityEngine.Physics2D.Simulate(.02f);
    return go;
}
try {
    UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;
    foreach(string name in new[]{"Chomper","Spora","Collusus"}) {
        PlayerAt(name=="Chomper"?2.2f:2.5f);
        var go=Spawn(name); var ai=go.GetComponent<BlightEnemy>(); var animation=go.GetComponent<EnemyVisualAnimator>();
        var health=go.GetComponent<EnemyHealth>(); var body=go.GetComponent<UnityEngine.Rigidbody2D>();
        Assert(animation.IsReady,"Rig initialization failed: "+name);
        var idle=animation.AnimatedMesh.vertices;
        for(int frame=0;frame<30;frame++) Call(animation,"LateUpdate");
        Assert(Difference(idle,animation.AnimatedMesh.vertices)>.00001f,"Idle pose does not animate: "+name);
        if(name!="Spora") {
            body.linearVelocity=new UnityEngine.Vector2(1,0); var before=animation.AnimatedMesh.vertices;
            for(int frame=0;frame<30;frame++) Call(animation,"LateUpdate");
            Assert(Difference(before,animation.AnimatedMesh.vertices)>.002f,"Walking pose does not animate: "+name);
            body.linearVelocity=UnityEngine.Vector2.zero;
        }
        Call(ai,"Update");
        Assert(ai.State==BlightEnemy.BehaviourState.Windup,"Real AI did not enter windup: "+name);
        var settings=new UnityEditor.SerializedObject(ai);float duration=settings.FindProperty("windup").floatValue;
        Set(ai,"windupStarted",UnityEngine.Time.time-duration*.8f);
        var windupBefore=animation.AnimatedMesh.vertices;
        for(int frame=0;frame<30;frame++) Call(animation,"LateUpdate");
        Assert(Difference(windupBefore,animation.AnimatedMesh.vertices)>.01f,"Windup pose missing: "+name);
        int attacks=ai.AttacksPerformed;
        int particles=animation.ParticlesEmitted;
        int projectiles=UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length;
        Set(ai,"stateUntil",UnityEngine.Time.time-1f);Call(ai,"Update");Call(animation,"LateUpdate");
        Assert(ai.AttacksPerformed==attacks+1 && animation.AttackAnimationsPlayed==1,"Strike not linked to real attack: "+name);
        Assert(ai.State==BlightEnemy.BehaviourState.Recovery,"Attack did not retain recovery: "+name);
        if(name=="Spora") {
            Assert(UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length==projectiles+1,"Spora animation did not accompany projectile launch.");
            Assert(animation.ParticlesEmitted>particles,"Spore particles missing.");
        }
        if(name=="Collusus") {
            Assert(animation.ParticlesEmitted>particles,"Slam dust missing.");
            Set(animation,"clock",.08f);Call(animation,"LateUpdate");
            var properties=(UnityEngine.MaterialPropertyBlock)typeof(EnemyVisualAnimator).GetField("properties",flags).GetValue(animation);
            Assert(properties.GetFloat("_Blink")>.5f,"Stone eye blink missing.");
            var facingRoot=(UnityEngine.Transform)settings.FindProperty("visuals").objectReferenceValue;
            var rootScale=facingRoot.localScale;
            // Shared vertex positions must remain connected outside the death breakup.
            var vertices=animation.AnimatedMesh.vertices;
            var rest=(UnityEngine.Vector3[])typeof(EnemyVisualAnimator).GetField("resting",flags).GetValue(animation);
            var positions=new System.Collections.Generic.Dictionary<UnityEngine.Vector3,UnityEngine.Vector3>();
            for(int i=0;i<vertices.Length;i++){
                if(positions.TryGetValue(rest[i],out var previous))Assert((previous-vertices[i]).sqrMagnitude<.000001f,"Stone limb seam in live pose.");
                else positions.Add(rest[i],vertices[i]);
            }
        }
        health.TakeDamage(5,UnityEngine.Vector2.right,0);Call(ai,"Update");Call(animation,"LateUpdate");
        Assert(animation.HitAnimationsPlayed==1 && ai.State==BlightEnemy.BehaviourState.Recovery,"Hit reaction failed: "+name);
        float clock=animation.AnimationClock;var pausedPose=animation.AnimatedMesh.vertices;int emitted=animation.ParticlesEmitted;
        UnityEngine.Time.timeScale=0;Call(animation,"LateUpdate");
        Assert(animation.AnimationClock==clock && Difference(pausedPose,animation.AnimatedMesh.vertices)==0 && emitted==animation.ParticlesEmitted,"Animation advances while paused: "+name);
        UnityEngine.Time.timeScale=1;
        Set(animation,"strikeAt",float.NegativeInfinity);Set(animation,"windupBlend",0f);
        health.TakeDamage(health.MaximumHealth,UnityEngine.Vector2.right,0);
        Assert(health.IsDead && !body.simulated && !go.GetComponent<UnityEngine.BoxCollider2D>().enabled,"Death leaves active gameplay: "+name);
        Assert(animation.DeathDuration>=1.5f && animation.ParticlesEmitted>emitted,"Complete death duration or particles missing: "+name);
        Set(animation,"deathAt",UnityEngine.Time.time-animation.DeathDuration*.45f);Call(animation,"LateUpdate");
        Assert(Difference(pausedPose,animation.AnimatedMesh.vertices)>.1f,"Death deformation missing: "+name);
        foreach(var vertex in animation.AnimatedMesh.vertices)Assert(!float.IsNaN(vertex.x)&&!float.IsNaN(vertex.y),"Invalid geometry: "+name);
        UnityEngine.Object.DestroyImmediate(go);
        // An attack dodged after the direction lock must not play a successful strike.
        PlayerAt(name=="Chomper"?2.2f:2.5f);go=Spawn(name);ai=go.GetComponent<BlightEnemy>();animation=go.GetComponent<EnemyVisualAnimator>();
        Call(ai,"Update");Set(ai,"windupStarted",UnityEngine.Time.time-duration);Call(ai,"Update");
        PlayerAt(0);Set(ai,"stateUntil",UnityEngine.Time.time-1f);Call(ai,"Update");Call(animation,"LateUpdate");
        Assert(ai.AttacksPerformed==0 && animation.AttackAnimationsPlayed==0,"Cancelled attack played a strike: "+name);
        UnityEngine.Object.DestroyImmediate(go);
        reports.Add(name+": idle, movement where applicable, real windup/attack/recovery, hit, pause, death and cancelled strike passed.");
    }
    return string.Join("\n",reports);
} finally {
    UnityEngine.Time.timeScale=1;
    UnityEngine.Physics2D.simulationMode=previousSimulation;
    foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);
    UnityEngine.Object.DestroyImmediate(player);
    oldPlayer.gameObject.SetActive(true);
    foreach(var enemy in originals)if(enemy!=null)enemy.gameObject.SetActive(true);
}
