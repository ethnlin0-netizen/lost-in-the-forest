// Run with Unity CLI eval_file while BloomPreview is in Play mode.
// Physics and input integration checks; stopping Play mode restores the scene.
if (!UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/BloomPreview.unity")
    throw new System.Exception("Start Play mode in BloomPreview first.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, flags).Invoke(o, args);
void Set(object o, string field, object value) => o.GetType().GetField(field, flags).SetValue(o, value);
void Assert(bool ok, string message) { if (!ok) throw new System.Exception(message); }
var results = new System.Collections.Generic.List<string>();
var player = UnityEngine.GameObject.Find("Player");
var movement = player.GetComponent<Movement>();
var combat = player.GetComponent<PlayerCombat>();
var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
var playerShape = player.GetComponent<UnityEngine.CapsuleCollider2D>();
var pets = UnityEngine.Object.FindObjectsByType<BloomPetFollower>(UnityEngine.FindObjectsSortMode.None);
Assert(pets.Length == 3, "Expected three Bloom pets.");
var oldMode = UnityEngine.Physics2D.simulationMode;
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldBackgroundInput = inputSettings.backgroundBehavior;
var oldEditorInput = inputSettings.editorInputBehaviorInPlayMode;
var gameInputUpdate = typeof(UnityEngine.InputSystem.InputSystem).GetMethod("Update",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null,
    new[] { typeof(UnityEngine.InputSystem.LowLevel.InputUpdateType) }, null);
var temporary = new System.Collections.Generic.List<UnityEngine.GameObject>();
UnityEngine.InputSystem.Keyboard keyboard = null;
void Input(params UnityEngine.InputSystem.Key[] keys)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
    // An eval runs on an Editor callback; an unfocused default update otherwise
    // processes Editor input and misses game wasPressedThisFrame transitions.
    keyboard.MakeCurrent();
    gameInputUpdate.Invoke(null, new object[] { UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic });
    Call(movement, "Update");
}
void PlacePlayer(float x, float feet = -2.47f)
{
    playerBody.position = new UnityEngine.Vector2(x, feet + 0.8f);
    playerBody.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
    Call(movement, "FixedUpdate");
}
void Tick(int steps)
{
    for (int i = 0; i < steps; i++)
    {
        Call(movement, "FixedUpdate");
        var groupToStep = player.GetComponent<BloomPetGroup>();
        if (groupToStep != null) Call(groupToStep, "FixedUpdate");
        foreach (var pet in pets) if (pet != null && pet.isActiveAndEnabled) Call(pet, "FixedUpdate");
        UnityEngine.Physics2D.Simulate(0.02f);
    }
}
try
{
    UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
    inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
    inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
    Input(); PlacePlayer(-7f); Tick(20);
    Assert(movement.IsGrounded, "Player did not land on the preview ground.");
    foreach (var pet in pets)
    {
        Assert(!pet.IsRecruited, "Pet recruited without player contact.");
        var enemy = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/Chomper.prefab"), pet.transform.position + UnityEngine.Vector3.up * 0.5f, UnityEngine.Quaternion.identity);
        temporary.Add(enemy); enemy.GetComponent<BlightEnemy>().enabled = false;
        UnityEngine.Physics2D.SyncTransforms(); UnityEngine.Physics2D.Simulate(0.02f);
        Assert(!pet.IsRecruited, "Enemy contact recruited a pet.");
        UnityEngine.Object.DestroyImmediate(enemy);
        var missile = new UnityEngine.GameObject("Recruitment rejection projectile"); temporary.Add(missile);
        missile.transform.position = pet.transform.position + UnityEngine.Vector3.up * 0.6f;
        var projectile = missile.AddComponent<PlaceholderProjectile>();
        missile.GetComponent<UnityEngine.Rigidbody2D>().gravityScale = 0f;
        projectile.Launch(UnityEngine.Vector2.right, 0f, 1f, playerBody);
        UnityEngine.Physics2D.SyncTransforms(); UnityEngine.Physics2D.Simulate(0.02f);
        Assert(!pet.IsRecruited, "Player-owned projectile recruited a pet.");
        UnityEngine.Object.DestroyImmediate(missile);
    }
    results.Add("All three pets remain idle and reject contact from a real enemy and a player-owned projectile.");
    var extra = new UnityEngine.GameObject("Extra player collider"); temporary.Add(extra);
    extra.transform.SetParent(player.transform, false); extra.AddComponent<UnityEngine.CircleCollider2D>().isTrigger = true;
    var starts = new System.Collections.Generic.Dictionary<BloomPetFollower, UnityEngine.Vector2>();
    foreach (var pet in pets) starts[pet] = pet.GetComponent<UnityEngine.Rigidbody2D>().position;
    Input(UnityEngine.InputSystem.Key.RightArrow); Tick(95); Input(); Tick(5);
    var group = player.GetComponent<BloomPetGroup>();
    Assert(group != null && group.Count == 3, "Walking through the three Blooms did not recruit all of them.");
    var slots = new System.Collections.Generic.HashSet<int>();
    foreach (var pet in pets)
    {
        Assert(pet.Owner == movement && slots.Add(pet.FollowSlot), "Wrong owner or duplicate follow slot.");
        Assert(!pet.transform.IsChildOf(player.transform), "Pet was parented to the player.");
        Assert(pet.GetComponent<UnityEngine.BoxCollider2D>().isTrigger, "Pet can block actors.");
        Assert(UnityEngine.Mathf.Abs(pet.GetComponent<UnityEngine.Rigidbody2D>().position.x - starts[pet].x) > 0.05f, "Recruited pet did not follow physically: " + pet.name);
    }
    Assert(group.Count == 3, "Multiple player colliders duplicated recruitment.");
    results.Add("Actual player walking recruits all three with distinct slots; duplicate colliders do not re-register them; pets follow without parenting or pushing.");

    PlacePlayer(7); Tick(200);
    foreach (var pet in pets)
    {
        Assert(UnityEngine.Mathf.Abs(pet.Feet.x - pet.Destination.x) <= 0.18f, "Pet did not reach its follow position: " + pet.name);
        Assert(UnityEngine.Mathf.Abs(pet.Feet.y + 2.475f) < 0.04f, "Pet lost ground contact.");
    }
    var settled = new System.Collections.Generic.Dictionary<BloomPetFollower, float>();
    foreach (var pet in pets) settled[pet] = pet.Feet.x;
    Tick(200);
    foreach (var pet in pets) Assert(UnityEngine.Mathf.Abs(pet.Feet.x - settled[pet]) < 0.001f, "Stationary pet jittered.");
    PlacePlayer(7.12f); Tick(30);
    foreach (var pet in pets) Assert(UnityEngine.Mathf.Abs(pet.Feet.x - settled[pet]) < 0.001f, "Small player shift caused pet jitter.");
    results.Add("All three settle at separate follow positions, remain grounded and stay still for 200 physics steps and a small player shift.");
    Input(UnityEngine.InputSystem.Key.LeftArrow); Tick(45); Input(); Tick(220);
    foreach (var pet in pets)
    {
        Assert(pet.Destination.x > playerBody.position.x, "Turn did not switch the trailing side.");
        Assert(UnityEngine.Mathf.Abs(pet.Feet.x - pet.Destination.x) < 0.18f, "Pet failed to settle after turning.");
        bool expectedFlip = pet.Feet.x < settled[pet];
        if (expectedFlip) Assert(pet.GetComponentInChildren<UnityEngine.SpriteRenderer>().flipX, "Pet walking left did not face left.");
    }
    results.Add("Changing player direction moves the trailing group to the other side and flips walking visuals.");

    var first = pets[0];
    var firstBody = first.GetComponent<UnityEngine.Rigidbody2D>();
    var wall = UnityEngine.GameObject.Find("Bloom Wall").GetComponent<UnityEngine.BoxCollider2D>();
    var wallSize = wall.size;
    wall.size = new UnityEngine.Vector2(0.5f, 30f);
    UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>().RebuildSurfaces();
    firstBody.position = new UnityEngine.Vector2(7.5f, -2.475f); firstBody.linearVelocity = UnityEngine.Vector2.zero;
    PlacePlayer(10f); Input(UnityEngine.InputSystem.Key.LeftArrow); Call(movement, "Update"); Input();
    Tick(220);
    Assert(first.Feet.x <= 8.33f && first.Feet.x >= 7.5f, "Pet passed through the preview wall or failed to approach it.");
    results.Add("A follower approaches solid cover and stops before its collider crosses the wall.");
    wall.size = wallSize;
    UnityEngine.Object.FindAnyObjectByType<PlatformNavigation2D>().RebuildSurfaces();

    firstBody.position = new UnityEngine.Vector2(-9f, -2.475f); firstBody.linearVelocity = UnityEngine.Vector2.zero;
    PlacePlayer(-10.4f); Input(UnityEngine.InputSystem.Key.RightArrow); Input(); Tick(220);
    Assert(first.Feet.x > -10.6f && first.Feet.y > -2.52f, "Pet walked off the left platform edge.");
    results.Add("A destination beyond the floor edge leaves the pet safely on the platform.");
    foreach (var pet in pets)
    {
        pet.GetComponent<UnityEngine.Rigidbody2D>().position = new UnityEngine.Vector2(-5f - pet.FollowSlot, -2.475f);
        pet.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity = UnityEngine.Vector2.zero;
        pet.GetComponent<EnemyNavigator2D>().Interrupt(); Set(pet, "fallSpeed", 0f);
    }
    PlacePlayer(1f, 0.25f); Input(); Tick(450);
    foreach (var pet in pets)
    {
        Assert(pet.IsGrounded && pet.Feet.y > 0.22f && pet.Feet.y < 0.3f,
            "Pet failed to jump onto raised platform: " + pet.name + " feet=" + pet.Feet);
        Assert(pet.GetComponent<EnemyNavigator2D>().JumpsPerformed > 0, "Pet did not use a validated jump route.");
    }
    results.Add("All three physically jump onto the raised platform and land at separate compressed follow positions.");
    PlacePlayer(-7f); Tick(450);
    foreach (var pet in pets)
    {
        Assert(pet.IsGrounded && pet.Feet.y < -2.45f, "Pet did not drop back to the ground.");
        Assert(UnityEngine.Mathf.Abs(pet.Feet.x - pet.Destination.x) < .18f,
            "Pet did not reach its lower follow position.");
    }
    // The current navigator can reach lower terrain with a walk, jump, or drop.
    // Verify the physical result rather than requiring one particular link kind.
    results.Add("All three safely reach their separate lower follow positions and land back on the ground.");

    PlacePlayer(-7f); Input(); Tick(5);
    Input(UnityEngine.InputSystem.Key.RightArrow, UnityEngine.InputSystem.Key.Z); Call(movement, "FixedUpdate");
    Assert(UnityEngine.Mathf.Abs(playerBody.linearVelocity.x - 6f) < 0.01f && UnityEngine.Mathf.Abs(playerBody.linearVelocity.y - 10f) < 0.01f, "Player run/jump changed.");
    Call(group, "FixedUpdate");
    foreach (var pet in pets) Call(pet, "FixedUpdate");
    UnityEngine.Physics2D.Simulate(0.02f); Input(); Tick(20);
    foreach (var pet in pets) Assert(pet.Feet.y > -1.5f, "Pet did not mirror the player's first jump: " + pet.name);
    Input(UnityEngine.InputSystem.Key.Z); Call(movement, "FixedUpdate");
    Assert(UnityEngine.Mathf.Abs(playerBody.linearVelocity.y - 14f) < 0.01f, "Player double jump changed.");
    Call(group, "FixedUpdate");
    foreach (var pet in pets) Call(pet, "FixedUpdate");
    UnityEngine.Physics2D.Simulate(0.02f); Input(); Tick(20);
    foreach (var pet in pets) Assert(pet.Feet.y > 0f, "Pet did not follow the player's wing jump: " + pet.name);
    Input(UnityEngine.InputSystem.Key.Z); Call(movement, "FixedUpdate");
    Assert(playerBody.linearVelocity.y < 14f, "Player received an extra airborne jump from a pet collider.");
    Input(); results.Add("Input-system arrow movement, normal jump and wing double jump retain 6/10/14 speeds; pets do not grant extra jumps.");
    Tick(200);
    foreach (var pet in pets) Assert(pet.IsGrounded && pet.Feet.y < -2.45f, "Jump-following pet did not fall and land safely.");
    results.Add("All three mirror both player jump impulses with staggered takeoffs, then fall and land safely.");

    var ceilingObject = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/BloomPets/WhisperseedPet.prefab"),
        new UnityEngine.Vector3(0f, -2.475f, 0f), UnityEngine.Quaternion.identity);
    temporary.Add(ceilingObject);
    var ceilingPet = ceilingObject.GetComponent<BloomPetFollower>();
    Set(ceilingPet, "fallSpeed", 18f);
    for (int i = 0; i < 90; i++)
    {
        Call(ceilingPet, "FixedUpdate"); UnityEngine.Physics2D.Simulate(.02f);
        Assert(ceilingObject.GetComponent<UnityEngine.BoxCollider2D>().bounds.max.y < -.23f,
            "Pet jumped through the raised platform underside.");
    }
    Assert(ceilingPet.IsGrounded, "Pet did not land after ceiling contact.");
    UnityEngine.Object.DestroyImmediate(ceilingObject);
    results.Add("An upward pet hull is stopped by the solid platform underside and falls safely back to ground.");

    // Check the existing combat against its existing practice-target component.
    foreach (var pet in pets) pet.GetComponent<UnityEngine.Rigidbody2D>().position = new UnityEngine.Vector2(1f + pet.FollowSlot, -2.475f);
    PlacePlayer(-6f); Input(UnityEngine.InputSystem.Key.RightArrow); Input();
    var target = new UnityEngine.GameObject("Bloom combat practice target"); temporary.Add(target);
    target.transform.position = new UnityEngine.Vector3(-4.9f, -1.67f, 0f);
    var sr = target.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Sprites/PlaceholderSquare.png"); sr.color = UnityEngine.Color.red;
    var targetBody = target.AddComponent<UnityEngine.Rigidbody2D>(); targetBody.gravityScale = 0f;
    var box = target.AddComponent<UnityEngine.BoxCollider2D>(); box.size = new UnityEngine.Vector2(0.5f, 1f);
    var practice = target.AddComponent<PracticeTarget>(); UnityEngine.Physics2D.SyncTransforms();
    Call(combat, "PerformMelee", false);
    Assert(!combat.LastMeleeWasCharged && sr.color == UnityEngine.Color.white, "Basic melee no longer hits a practice target.");
    Input(UnityEngine.InputSystem.Key.X); Call(combat, "Update"); Input(); Call(combat, "Update");
    UnityEngine.Physics2D.Simulate(0.02f);
    Assert(combat.LastMeleeWasCharged && sr.color == UnityEngine.Color.yellow && targetBody.linearVelocity.x > 0f, "Full-strength tail sweep or knockback failed.");
    targetBody.position = new UnityEngine.Vector2(-2f, -1.67f); targetBody.linearVelocity = UnityEngine.Vector2.zero; targetBody.constraints = UnityEngine.RigidbodyConstraints2D.FreezeAll;
    pets[0].GetComponent<UnityEngine.Rigidbody2D>().position = new UnityEngine.Vector2(-4f, -2.475f); UnityEngine.Physics2D.SyncTransforms();
    Input(UnityEngine.InputSystem.Key.F); Call(combat, "Update"); Input();
    var shots = UnityEngine.Object.FindObjectsByType<PlaceholderProjectile>(UnityEngine.FindObjectsSortMode.None);
    Assert(shots.Length > 0, "Player did not fire a projectile.");
    sr.color = UnityEngine.Color.red;
    for (int i = 0; i < 30; i++)
    {
        foreach (var shot in shots) if (shot != null && shot.gameObject.activeSelf) Call(shot, "FixedUpdate");
        UnityEngine.Physics2D.Simulate(0.02f);
    }
    Assert(sr.color == UnityEngine.Color.white, "Pet blocked a player projectile from reaching its practice target.");
    results.Add("The existing basic-melee method, Input-system X tail sweep/knockback and F projectile hits work; the projectile passes through a pet.");
    var report = Newtonsoft.Json.JsonConvert.SerializeObject(new {passed = true, checks = results, simulation = "Play mode, actual Physics2D.Simulate with Input System keyboard events and manually stepped component callbacks"});
    System.IO.File.WriteAllText("Temp/BloomFollowerPhysicsResults.json", report);
    return report;
}
finally
{
    if (keyboard != null) UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.Physics2D.simulationMode = oldMode;
    inputSettings.backgroundBehavior = oldBackgroundInput;
    inputSettings.editorInputBehaviorInPlayMode = oldEditorInput;
    foreach (var go in temporary) if (go != null) UnityEngine.Object.DestroyImmediate(go);
}
