// Run in a fresh Play session, then stop Play Mode. Runtime-only encounter isolation.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Enter Play Mode first.");
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
void Assert(bool condition, string message) { if (!condition) throw new System.Exception(message); }
void Call(object component, string method) { component.GetType().GetMethod(method, flags).Invoke(component, null); }
bool Changed(UnityEngine.Vector3[] a, UnityEngine.Vector3[] b) {
    for (int i = 0; i < a.Length; ++i) if (UnityEngine.Vector3.Distance(a[i], b[i]) > .00001f) return true;
    return false;
}
var enemies = UnityEngine.Object.FindObjectsByType<BlightEnemy>();
Assert(enemies.Length >= 3, "Expected at least the three original thorn-section enemies.");
var world = UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>();
Assert(world != null && world.Surfaces.Count == 21, "Course navigation is missing.");
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
var playerShape = player.GetComponent<UnityEngine.CapsuleCollider2D>();
var playerHealth = player.GetComponent<PlayerHealth>();
var oldTimeScale = UnityEngine.Time.timeScale;
foreach (var pickup in UnityEngine.Object.FindObjectsByType<AmuletPickup>()) pickup.GetComponent<UnityEngine.Collider2D>().enabled = false;
foreach (var enemy in enemies) {
    Assert(enemy.GetComponent<EnemyVisualAnimator>().IsReady, enemy.name + " animation failed to initialize.");
    enemy.enabled = false;
    enemy.GetComponent<UnityEngine.Rigidbody2D>().simulated = false;
    enemy.GetComponent<UnityEngine.BoxCollider2D>().enabled = false;
}
int attackChecks = 0, damageChecks = 0, deathChecks = 0, chaseChecks = 0;
try {
    foreach (var enemy in enemies) {
        var animator = enemy.GetComponent<EnemyVisualAnimator>();
        var body = enemy.GetComponent<UnityEngine.Rigidbody2D>();
        var shape = enemy.GetComponent<UnityEngine.BoxCollider2D>();
        var health = enemy.GetComponent<EnemyHealth>();
        shape.enabled = true; body.simulated = true;
        float height = shape.size.y * enemy.transform.lossyScale.y;
        float offset = shape.offset.y * enemy.transform.lossyScale.y;
        enemy.transform.position = new UnityEngine.Vector3(-4, 0 + height/2 - offset + .01f, 0);
        body.position = enemy.transform.position; body.linearVelocity = UnityEngine.Vector2.zero;
        player.transform.position = new UnityEngine.Vector3(-7, 0 + playerShape.size.y/2 - playerShape.offset.y + .01f, 0);
        playerBody.position = player.transform.position; playerBody.linearVelocity = UnityEngine.Vector2.zero;
        UnityEngine.Physics2D.SyncTransforms();
        enemy.enabled = true;
        typeof(BlightEnemy).GetField("nextAttack", flags).SetValue(enemy, 0f);
        Assert(enemy.GetComponent<EnemyNavigator2D>().IsGrounded, enemy.name + " cannot detect the floor.");
        if (enemy.Kind != BlightEnemy.BlightType.Spora) {
            player.transform.position = new UnityEngine.Vector3(-7, 100, 0);
            playerBody.position = player.transform.position;
            Call(enemy, "Update");
            Call(enemy, "FixedUpdate");
            Assert(enemy.State == BlightEnemy.BehaviourState.Patrol && UnityEngine.Mathf.Abs(body.linearVelocity.x) > .01f,
                enemy.name + " cannot patrol on a safe platform.");
            player.transform.position = new UnityEngine.Vector3(-7, playerShape.size.y/2-playerShape.offset.y+.01f, 0);
            playerBody.position = player.transform.position;
            UnityEngine.Physics2D.SyncTransforms();
            Call(enemy, "Update");
            Assert(enemy.State == BlightEnemy.BehaviourState.Chase, enemy.name + " did not react to the nearby player.");
            Call(enemy, "FixedUpdate");
            Assert(enemy.GetComponent<EnemyNavigator2D>().RoutesCalculated > 0, enemy.name + " did not calculate a pursuit route.");
            ++chaseChecks;
            player.transform.position = new UnityEngine.Vector3(-5.2f, player.transform.position.y, 0);
            playerBody.position = player.transform.position; UnityEngine.Physics2D.SyncTransforms();
        }
        Call(enemy, "Update");
        Assert(enemy.State == BlightEnemy.BehaviourState.Windup, enemy.name + " did not prepare its attack.");
        int attacks = enemy.AttacksPerformed, animatedAttacks = animator.AttackAnimationsPlayed;
        int projectiles = UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length;
        float playerHealthBefore = playerHealth.CurrentHealth;
        typeof(PlayerHealth).GetField("invulnerableUntil", flags).SetValue(playerHealth, 0f);
        typeof(BlightEnemy).GetField("stateUntil", flags).SetValue(enemy, UnityEngine.Time.time - .01f);
        Call(enemy, "Update"); Call(animator, "LateUpdate");
        Assert(enemy.AttacksPerformed == attacks + 1 && animator.AttackAnimationsPlayed == animatedAttacks + 1,
            enemy.name + " actual AI attack did not animate.");
        if (enemy.Kind == BlightEnemy.BlightType.Spora)
            Assert(UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length > projectiles, enemy.name + " did not fire spores.");
        else Assert(playerHealth.CurrentHealth < playerHealthBefore, enemy.name + " melee did not damage the player.");
        // Restore runtime health between cases so every enemy can target the same living player.
        typeof(PlayerHealth).GetField("<CurrentHealth>k__BackingField", flags).SetValue(playerHealth, playerHealth.MaximumHealth);
        ++attackChecks;
        int hits = animator.HitAnimationsPlayed;
        health.TakeDamage(1, UnityEngine.Vector2.right, 0);
        Call(animator, "LateUpdate");
        Assert(animator.HitAnimationsPlayed == hits + 1, enemy.name + " damage reaction did not animate.");
        var pose = animator.AnimatedMesh.vertices;
        float clock = animator.AnimationClock;
        UnityEngine.Time.timeScale = 0; Call(animator, "LateUpdate");
        Assert(animator.AnimationClock == clock && !Changed(pose, animator.AnimatedMesh.vertices), enemy.name + " continued animating while paused.");
        UnityEngine.Time.timeScale = oldTimeScale;
        ++damageChecks;
        health.TakeDamage(health.MaximumHealth, UnityEngine.Vector2.right, 0);
        typeof(EnemyVisualAnimator).GetField("deathAt", flags).SetValue(animator, UnityEngine.Time.time - .3f);
        Call(animator, "LateUpdate");
        Assert(health.IsDead && !body.simulated && !shape.enabled && animator.ParticlesEmitted > 0 && Changed(pose, animator.AnimatedMesh.vertices),
            enemy.name + " death reaction did not animate.");
        ++deathChecks;
    }
    return new { passed = true, initializedEnemies = enemies.Length, actualAttacksAnimated = attackChecks,
        damageAndPauseChecks = damageChecks, deathAnimations = deathChecks, pursuitRouteChecks = chaseChecks };
} finally { UnityEngine.Time.timeScale = oldTimeScale; }
