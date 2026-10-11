// Run with unity command eval_file in EnemyAI Play mode. This exercises the
// complete pursuit brain plus Rigidbody2D simulation, not just graph reachability.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Assert(bool condition, string message) { if (!condition) throw new System.Exception(message); }
var world = UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>();
var live = UnityEngine.Object.FindObjectsByType<BlightEnemy>();
foreach (var enemy in live) enemy.gameObject.SetActive(false);
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
var oldConstraints = playerBody.constraints;
var oldPosition = playerBody.position;
var oldVelocity = playerBody.linearVelocity;
var oldMode = UnityEngine.Physics2D.simulationMode;
var actors = new System.Collections.Generic.List<UnityEngine.GameObject>();
var results = new System.Collections.Generic.List<string>();
void PlacePlayer(UnityEngine.Vector2 feet) {
    var shape = player.GetComponent<UnityEngine.Collider2D>();
    playerBody.position += feet - new UnityEngine.Vector2(shape.bounds.center.x, shape.bounds.min.y);
    playerBody.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
}
try {
    UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
    playerBody.constraints |= UnityEngine.RigidbodyConstraints2D.FreezePosition;
    world.RebuildSurfaces();
    var cases = new[] {
        ("Chomper", new UnityEngine.Vector2(3, 2.4f), new UnityEngine.Vector2(3, -2.1f), true),
        ("Collusus", new UnityEngine.Vector2(3, 2.4f), new UnityEngine.Vector2(3, -2.1f), true),
        ("Chomper", new UnityEngine.Vector2(-15, -.8f), new UnityEngine.Vector2(-15, -3.5f), true),
        ("Collusus", new UnityEngine.Vector2(-15, -.8f), new UnityEngine.Vector2(-15, -3.5f), true),
        ("Chomper", new UnityEngine.Vector2(-23, -2.1f), new UnityEngine.Vector2(-15, -.8f), false),
        ("Collusus", new UnityEngine.Vector2(20, -2.9f), new UnityEngine.Vector2(24.3f, -2.6f), false)
    };
    foreach (var test in cases) {
        PlacePlayer(new UnityEngine.Vector2(-30, -3.5f));
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/" + test.Item1 + ".prefab");
        var actor = UnityEngine.Object.Instantiate(prefab);
        actors.Add(actor);
        var ai = actor.GetComponent<BlightEnemy>();
        ai.enabled = false;
        var body = actor.GetComponent<UnityEngine.Rigidbody2D>();
        var shape = actor.GetComponent<UnityEngine.BoxCollider2D>();
        body.position = test.Item2 + UnityEngine.Vector2.up * (shape.size.y * .5f + .01f);
        UnityEngine.Physics2D.SyncTransforms();
        for (int i = 0; i < 15; i++) UnityEngine.Physics2D.Simulate(.02f);
        // Keep this test about pursuit; existing combat checks exercise windups.
        ai.GetType().GetField("nextAttack", flags).SetValue(ai, UnityEngine.Time.time + 10000f);
        PlacePlayer(test.Item3);
        var nav = actor.GetComponent<EnemyNavigator2D>();
        for (int i = 0; i < 1000; i++) {
            ai.GetType().GetMethod("Update", flags).Invoke(ai, null);
            ai.GetType().GetMethod("FixedUpdate", flags).Invoke(ai, null);
            UnityEngine.Physics2D.Simulate(.02f);
            foreach (var surface in world.Surfaces) {
                var separation = UnityEngine.Physics2D.Distance(shape, surface);
                Assert(!separation.isOverlapped || separation.distance > -.06f, test.Item1 + " penetrated " + surface.name);
            }
        }
        Assert(nav.GoalReachable && nav.IsGrounded && world.SupportAt(nav.Feet) == world.SupportAt(test.Item3), test.Item1 + " did not reach the player's platform: " + nav.Feet);
        float range = new UnityEditor.SerializedObject(ai).FindProperty("attackRange").floatValue;
        Assert(UnityEngine.Mathf.Abs(nav.Feet.x - test.Item3.x) <= range + .25f, test.Item1 + " failed to approach after landing.");
        Assert(test.Item4 ? nav.DropsPerformed + nav.JumpsPerformed > 0 : nav.JumpsPerformed > 0, test.Item1 + " did not perform the expected traversal.");
        Assert(nav.StuckRecoveries == 0 && nav.RoutesCalculated < 10, test.Item1 + " got stuck or churned routes.");
        var wait = nav.Feet;
        for (int i = 0; i < 100; i++) {
            ai.GetType().GetMethod("Update", flags).Invoke(ai, null);
            ai.GetType().GetMethod("FixedUpdate", flags).Invoke(ai, null);
            UnityEngine.Physics2D.Simulate(.02f);
        }
        Assert(UnityEngine.Vector2.Distance(nav.Feet, wait) < .05f, "Arrival jitter: " + test.Item1);
        if (test.Item1 == "Collusus") Assert(new UnityEditor.SerializedObject(ai).FindProperty("movementSpeed").floatValue == 1.5f, "Collusus speed changed.");
        results.Add("PASS " + test.Item1 + " " + test.Item2 + " -> " + test.Item3 + ": lands, approaches, holds spacing, no penetration/stuck loop; jumps=" + nav.JumpsPerformed + " drops=" + nav.DropsPerformed);
        UnityEngine.Object.DestroyImmediate(actor);
    }
    return results.ToArray();
} finally {
    foreach (var actor in actors) if (actor != null) UnityEngine.Object.DestroyImmediate(actor);
    playerBody.constraints = oldConstraints;
    playerBody.position = oldPosition;
    playerBody.linearVelocity = oldVelocity;
    UnityEngine.Physics2D.simulationMode = oldMode;
    foreach (var enemy in live) if (enemy != null) enemy.gameObject.SetActive(true);
    UnityEngine.Physics2D.SyncTransforms();
}
