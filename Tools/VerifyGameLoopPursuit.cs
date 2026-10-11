// Run in GloamthorneLevel02 Play mode. Uses the real level's platforms,
// integrated prefab variants, pursuit brain, and manually stepped Rigidbody2D.
if (!UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GloamthorneLevel02")
    throw new System.Exception("GloamthorneLevel02 Play mode required.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
var world = UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>();
var live = UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None);
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var pb = player.GetComponent<UnityEngine.Rigidbody2D>();
var oldPosition = pb.position;
var oldVelocity = pb.linearVelocity;
var oldConstraints = pb.constraints;
var oldMode = UnityEngine.Physics2D.simulationMode;
var actors = new System.Collections.Generic.List<UnityEngine.GameObject>();
var results = new System.Collections.Generic.List<string>();
void PlacePlayer(UnityEngine.Vector2 feet) {
    var c = player.GetComponent<UnityEngine.Collider2D>();
    pb.position += feet - new UnityEngine.Vector2(c.bounds.center.x, c.bounds.min.y);
    pb.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
}
try {
    foreach (var e in live) e.gameObject.SetActive(false);
    UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
    pb.constraints |= UnityEngine.RigidbodyConstraints2D.FreezePosition;
    world.RebuildSurfaces();
    var cases = new[] {
        (new UnityEngine.Vector2(33,6.5f), new UnityEngine.Vector2(40,9), "07_FinalBranch"),
        (new UnityEngine.Vector2(21,4), new UnityEngine.Vector2(27,6.5f), "05_ThornCrossing"),
        (new UnityEngine.Vector2(14,3), new UnityEngine.Vector2(21,4), "04_DecayingBranch"),
        (new UnityEngine.Vector2(40,9), new UnityEngine.Vector2(45,11.5f), "08_ItemSanctuary")
    };
    foreach (var name in new[] { "Chomper", "Collusus" }) {
        foreach (var test in cases) {
            PlacePlayer(new UnityEngine.Vector2(0,11));
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/IntegratedEnemies/" + name + ".prefab");
            var actor = UnityEngine.Object.Instantiate(prefab);
            actors.Add(actor);
            var ai = actor.GetComponent<BlightEnemy>();
            ai.enabled = false;
            var body = actor.GetComponent<UnityEngine.Rigidbody2D>();
            var shape = actor.GetComponent<UnityEngine.BoxCollider2D>();
            body.position += test.Item1 - new UnityEngine.Vector2(shape.bounds.center.x, shape.bounds.min.y) + UnityEngine.Vector2.up * .01f;
            UnityEngine.Physics2D.SyncTransforms();
            for (int i = 0; i < 20; i++) UnityEngine.Physics2D.Simulate(.02f);
            ai.GetType().GetField("nextAttack",flags).SetValue(ai,UnityEngine.Time.time + 10000f);
            PlacePlayer(test.Item2);
            var nav = actor.GetComponent<EnemyNavigator2D>();
            for (int i = 0; i < 1000; i++) {
                ai.GetType().GetMethod("Update",flags).Invoke(ai,null);
                ai.GetType().GetMethod("FixedUpdate",flags).Invoke(ai,null);
                UnityEngine.Physics2D.Simulate(.02f);
                foreach (var surface in world.Surfaces) {
                    var separation = UnityEngine.Physics2D.Distance(shape,surface);
                    Check(!separation.isOverlapped || separation.distance > -.06f,name + " penetrated " + surface.name);
                }
            }
            var support = world.SupportAt(nav.Feet);
            Check(nav.GoalReachable && nav.IsGrounded && support != null && support.name == test.Item3,
                name + " failed upward pursuit " + test.Item1 + " -> " + test.Item2 + ": feet=" + nav.Feet + " reachable=" + nav.GoalReachable + " jumps=" + nav.JumpsPerformed + " stuck=" + nav.StuckRecoveries);
            Check(nav.JumpsPerformed > 0,name + " never jumped.");
            float range = new UnityEditor.SerializedObject(ai).FindProperty("attackRange").floatValue;
            Check(UnityEngine.Mathf.Abs(nav.Feet.x-test.Item2.x) <= range+.25f,name + " stopped too far from the player.");
            Check(nav.StuckRecoveries == 0 && nav.RoutesCalculated < 15,name + " repeatedly retried its route.");
            var stop = nav.Feet;
            for (int i=0;i<100;i++) {
                ai.GetType().GetMethod("Update",flags).Invoke(ai,null);
                ai.GetType().GetMethod("FixedUpdate",flags).Invoke(ai,null);
                UnityEngine.Physics2D.Simulate(.02f);
            }
            Check(UnityEngine.Vector2.Distance(stop,nav.Feet)<.05f,name+" jittered after landing.");
            if(name=="Collusus") Check(new UnityEditor.SerializedObject(ai).FindProperty("movementSpeed").floatValue==1.5f,"Collusus ground speed changed.");
            results.Add("PASS " + name + " " + test.Item1 + " -> " + test.Item2 + ": real solid-platform jump, lands, approaches, stable arrival, no penetration or stuck loop; jumps="+nav.JumpsPerformed);
            UnityEngine.Object.DestroyImmediate(actor);
        }
    }
    return string.Join("\n",results);
} finally {
    foreach(var actor in actors) if(actor!=null) UnityEngine.Object.DestroyImmediate(actor);
    pb.constraints=oldConstraints; pb.position=oldPosition; pb.linearVelocity=oldVelocity;
    UnityEngine.Physics2D.simulationMode=oldMode;
    foreach(var e in live) if(e!=null) e.gameObject.SetActive(true);
    UnityEngine.Physics2D.SyncTransforms(); world.RebuildSurfaces();
}
