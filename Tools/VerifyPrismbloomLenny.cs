// Run in a fresh Play session in PrismbloomLevel01; stop Play afterward.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Enter Play Mode first.");
var player = UnityEngine.Object.FindAnyObjectByType<Movement>();
var animator = player.GetComponent<LennyVisualAnimator>();
var combat = player.GetComponent<PlayerCombat>();
var health = player.GetComponent<PlayerHealth>();
var body = player.GetComponent<UnityEngine.Rigidbody2D>();
var camera = UnityEngine.Object.FindAnyObjectByType<Level01Camera>();
var background = UnityEngine.GameObject.Find("Prismbloom Background").GetComponentInChildren<UnityEngine.SpriteRenderer>();
var backgroundBounds = background.bounds;
foreach (var section in background.transform.parent.GetComponentsInChildren<UnityEngine.SpriteRenderer>()) backgroundBounds.Encapsulate(section.bounds);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
void Tick() { typeof(LennyVisualAnimator).GetMethod("LateUpdate", flags).Invoke(animator, null); }
void Assert(bool condition, string message) { if (!condition) throw new System.Exception(message); }
bool Changed(UnityEngine.Vector3[] a, UnityEngine.Vector3[] b)
{
    for (int i = 0; i < a.Length; i++) if (UnityEngine.Vector3.Distance(a[i], b[i]) > .00001f) return true;
    return false;
}
Assert(animator != null && animator.IsReady, "Lenny's animation did not initialize.");
Assert(camera.target == player.transform, "Camera lost the player reference.");
var backgroundPosition = background.transform.position;
var backgroundScale = background.transform.localScale;
var originalPosition = player.transform.position;
var originalCameraPosition = camera.transform.position;
float oldTimeScale = UnityEngine.Time.timeScale;
var originalVelocity = body.linearVelocity;
try
{
    int coverageChecks = 0;
    var renderCamera = camera.GetComponent<UnityEngine.Camera>();
    float oldAspect = renderCamera.aspect;
    try
    {
        foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
        {
            renderCamera.aspect = aspect;
            foreach (var position in new[] { new UnityEngine.Vector3(1, 1.5f, 0), new UnityEngine.Vector3(18, 10, 0), new UnityEngine.Vector3(39, 16, 0) })
            {
                player.transform.position = position;
                typeof(Level01Camera).GetMethod("LateUpdate", flags).Invoke(camera, null);
                var low = renderCamera.WorldToViewportPoint(backgroundBounds.min);
                var high = renderCamera.WorldToViewportPoint(backgroundBounds.max);
                Assert(low.x <= 0 && low.y <= 0 && high.x >= 1 && high.y >= 1, "Background does not cover the course.");
                Assert(background.transform.position == backgroundPosition && background.transform.localScale == backgroundScale,
                    "Background follows the camera.");
                coverageChecks++;
            }
        }
    }
    finally { renderCamera.aspect = oldAspect; }
    Assert(UnityEngine.Object.FindAnyObjectByType<BiomeBackgroundController>() == null, "Camera-following background controller remains.");
    player.transform.position = originalPosition;
    var idle = animator.AnimatedMesh.vertices;
    Tick(); Assert(Changed(idle, animator.AnimatedMesh.vertices), "Idle pose did not change.");
    typeof(Movement).GetField("<IsGrounded>k__BackingField", flags).SetValue(player, true);
    body.linearVelocity = new UnityEngine.Vector2(6, 0);
    var beforeWalk = animator.AnimatedMesh.vertices;
    Tick(); Assert(Changed(beforeWalk, animator.AnimatedMesh.vertices), "Walking pose did not change.");
    typeof(Movement).GetField("<FacingDirection>k__BackingField", flags).SetValue(player, -1);
    Tick(); Assert(player.transform.Find("Lenny Animated Artwork").localScale.x == -1, "Left-facing artwork did not flip.");
    typeof(PlayerCombat).GetMethod("PerformMelee", flags).Invoke(combat, new object[] { false });
    Assert(animator.MeleeAnimationsPlayed == 1, "Basic melee was not animated.");
    typeof(LennyVisualAnimator).GetField("attackAt", flags).SetValue(animator, UnityEngine.Time.time - .1f);
    Tick(); var basicPose = animator.AnimatedMesh.vertices;
    typeof(PlayerCombat).GetMethod("PerformMelee", flags).Invoke(combat, new object[] { true });
    typeof(LennyVisualAnimator).GetField("attackAt", flags).SetValue(animator, UnityEngine.Time.time - .1f);
    Tick(); Assert(animator.MeleeAnimationsPlayed == 2 && Changed(basicPose, animator.AnimatedMesh.vertices), "Strong melee lacks a distinct tail pose.");
    int projectileCount = UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length;
    typeof(PlayerCombat).GetMethod("FireProjectile", flags).Invoke(combat, null);
    Assert(animator.RangedAnimationsPlayed == 1 && UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>().Length > projectileCount,
        "Actual projectile firing did not trigger the ranged animation.");
    health.TakeDamage(1, UnityEngine.Vector2.zero, 0);
    Assert(animator.HitAnimationsPlayed == 1 && health.CurrentHealth == health.MaximumHealth - 1, "Damage reaction failed.");
    player.transform.position = originalPosition + UnityEngine.Vector3.up * 100;
    UnityEngine.Physics2D.SyncTransforms();
    body.linearVelocity = new UnityEngine.Vector2(0, -1);
    typeof(Movement).GetField("<IsGrounded>k__BackingField", flags).SetValue(player, false);
    typeof(LennyVisualAnimator).GetField("previouslyGrounded", flags).SetValue(animator, false);
    typeof(LennyVisualAnimator).GetField("lastVelocityY", flags).SetValue(animator, -1f);
    typeof(Movement).GetField("airJumpAvailable", flags).SetValue(player, true);
    typeof(Movement).GetField("jumpRequested", flags).SetValue(player, true);
    int doubleJumpsBefore = animator.DoubleJumpAnimationsPlayed;
    typeof(Movement).GetMethod("FixedUpdate", flags).Invoke(player, null);
    Tick(); Assert(body.linearVelocity.y > 8 && animator.DoubleJumpAnimationsPlayed == doubleJumpsBefore + 1, "Actual double jump did not flap the wings.");
    float clock = animator.AnimationClock;
    var pose = animator.AnimatedMesh.vertices;
    UnityEngine.Time.timeScale = 0; Tick();
    Assert(animator.AnimationClock == clock && !Changed(pose, animator.AnimatedMesh.vertices), "Animation continued while paused.");
    foreach (var vertex in animator.AnimatedMesh.vertices)
        Assert(!float.IsNaN(vertex.x) && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.x) && !float.IsInfinity(vertex.y), "Invalid mesh geometry.");
    return new { passed = true, worldBackgroundCoverageChecks = coverageChecks,
        animationChecks = "idle, walk, facing, basic/strong melee, actual ranged projectile, damage, actual double jump, pause and finite geometry" };
}
finally
{
    UnityEngine.Time.timeScale = oldTimeScale;
    player.transform.position = originalPosition;
    body.linearVelocity = originalVelocity;
    camera.transform.position = originalCameraPosition;
}
