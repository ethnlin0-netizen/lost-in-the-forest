// Run with Unity eval_file in Play mode. Runtime-only changes; no assets are saved.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required.");
var paths = new[] {
    "PrismbloomLevel01", "GloamthorneLevel02", "Ashpetal Level 03", "Veinroot Level04",
    "Wraithglass Barrens Level 05", "BossStory", "PrismbloomLevel01", "PrismbloomLevel01",
    "PrismbloomLevel01", "GloamthorneLevel02", "Ashpetal Level 03", "Veinroot Level04",
    "Veinroot Level04", "GloamthorneLevel02", "Ashpetal Level 03", "PrismbloomLevel01Story", "TitleScreen"
};
var results = new System.Collections.Generic.List<string>();
var spawns = new System.Collections.Generic.Dictionary<BloomCompanionId,UnityEngine.Vector2>();
var beforeDeathFollowers = new System.Collections.Generic.HashSet<UnityEngine.EntityId>();
UnityEngine.EntityId beforeDeathPlayer = default;
UnityEngine.Vector3 playerSpawn = default;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var oldSimulation = UnityEngine.Physics2D.simulationMode;
bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
UnityEngine.Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
BloomCompanionSession.ResetCollected();
int index = 0; bool pending = false, done = false; double ready = 0, deadline = 0;
UnityEditor.EditorApplication.CallbackFunction tick = null;
UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode> onLoaded = null;
void Check(bool ok, string message) { if (!ok) throw new System.Exception(message); }
BloomPetFollower[] Pets(UnityEngine.SceneManagement.Scene s) => s.GetRootGameObjects()
    .SelectMany(root => root.GetComponentsInChildren<BloomPetFollower>()).Where(p => p.enabled).ToArray();
void Finish(string error) {
    if (done) return; done = true;
    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onLoaded;
    UnityEditor.EditorApplication.update -= tick;
    UnityEngine.Application.runInBackground = oldBackground;
    UnityEngine.Physics2D.simulationMode = oldSimulation;
    UnityEngine.Time.timeScale = 1f; UnityEngine.AudioListener.pause = false;
    results.Add(error == null ? "PASS all recruitment, transition, duplicate, health/hazard companion respawn, and story cases" : "FAIL " + error);
    System.IO.File.WriteAllLines("Temp/BloomCompanionRunResults.txt", results);
}
void LoadCase() {
    pending = false; deadline = UnityEditor.EditorApplication.timeSinceStartup + 20;
    UnityEngine.Time.timeScale = 1;
    if (index == 15) { BloomCompanionSession.ResetCollected(); StoryRunSession.BeginNewGame(true); }
    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/GameLoop/" + paths[index] + ".unity",
        UnityEngine.SceneManagement.LoadSceneMode.Single);
}
void FreezeEnemies(UnityEngine.SceneManagement.Scene scene) {
    foreach (var root in scene.GetRootGameObjects()) {
        foreach (var ai in root.GetComponentsInChildren<BlightEnemy>()) ai.enabled = false;
        foreach (var ai in root.GetComponentsInChildren<BlightBoss>()) ai.enabled = false;
    }
}
void Recruit(BloomPetFollower pet, Movement player) {
    Check(!pet.IsRecruited, "Expected an uncollected pickup: " + pet.name);
    var start = pet.Feet;
    if (spawns.TryGetValue(pet.CompanionId, out var saved))
        Check(UnityEngine.Vector2.Distance(start, saved) < .08f, "Pickup did not return to saved position: " + pet.name);
    else spawns[pet.CompanionId] = start;
    Check(pet.CompanionPrefab != null && UnityEditor.EditorUtility.IsPersistent(pet.CompanionPrefab), "Source is not a prefab asset");
    var playerBody = player.GetComponent<UnityEngine.Rigidbody2D>();
    var shape = player.GetComponent<UnityEngine.Collider2D>();
    float offset = playerBody.position.y - shape.bounds.min.y;
    playerBody.position = start + UnityEngine.Vector2.up * offset;
    playerBody.linearVelocity = UnityEngine.Vector2.zero;
    UnityEngine.Physics2D.SyncTransforms();
    UnityEngine.Physics2D.Simulate(.02f); UnityEngine.Physics2D.Simulate(.02f);
    Check(pet.IsRecruited && pet.Owner == player && BloomCompanionSession.IsCollected(pet.CompanionId), "Real player contact did not recruit " + pet.name);
    var count = BloomCompanionSession.Count;
    UnityEngine.Physics2D.Simulate(.02f);
    Check(BloomCompanionSession.Count == count, "Repeated contact duplicated recruitment");
}
void CheckGroup(UnityEngine.SceneManagement.Scene scene, Movement player, int count) {
    Check(BloomCompanionSession.Count == count, "Wrong run recruitment count at " + paths[index]);
    var recruited = Pets(scene).Where(p => p.IsRecruited).ToArray();
    Check(recruited.Length == count, "Missing or duplicate followers at " + paths[index]);
    Check(recruited.Select(p => p.CompanionId).Distinct().Count() == count, "Duplicate identity");
    Check(recruited.Select(p => p.FollowSlot).Distinct().Count() == count, "Duplicate follow slots");
    foreach (var pet in recruited) {
        Check(pet.Owner == player && pet.FollowSlot >= 0, "Wrong owner or slot");
        Check(pet.gameObject.scene == scene, "Follower retained the previous scene");
        Check(pet.CompanionPrefab != null && UnityEditor.EditorUtility.IsPersistent(pet.CompanionPrefab), "Prefab source was lost after transition");
        foreach (var other in recruited) if (pet != other)
            Check(UnityEngine.Vector2.Distance(pet.Feet, other.Feet) > .3f, "Arrival followers overlap");
    }
    if (count > 0) Check(player.GetComponent<BloomPetGroup>().Count == count, "Player group has duplicate registrations");
}
onLoaded = (scene, mode) => {
    if (done || scene.name != paths[index]) return;
    FreezeEnemies(scene);
    pending = true; ready = UnityEditor.EditorApplication.timeSinceStartup + .2;
};
tick = () => {
    try {
        if (done) return;
        if (!UnityEditor.EditorApplication.isPlaying) { Finish("Play mode stopped"); return; }
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        if (!pending || now < ready) { if (now > deadline) Finish("Scene load timed out at " + index); return; }
        pending = false;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (index == 16) { Check(BloomCompanionSession.Count == 0, "Returning to title retained recruitment"); Finish(null); return; }
        var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Movement>()).Single();
        var health = player.GetComponent<PlayerHealth>();
        UnityEngine.Time.timeScale = 0;
        if (index <= 2) {
            CheckGroup(scene, player, index);
            var pet = Pets(scene).Single(p => !p.IsRecruited);
            Recruit(pet, player); CheckGroup(scene, player, index + 1);
            results.Add("PASS " + paths[index] + ": touch recruits " + pet.CompanionId + "; persistent source and distinct slots");
        } else if (index <= 7) {
            CheckGroup(scene, player, 3);
            Check(Pets(scene).Length == 3, "Collected pickup duplicated on revisiting its source scene");
            // Repeated scene callbacks must be idempotent, including authored pickups already collected.
            typeof(BloomCompanionSession).GetMethod("RestoreFollowers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { scene, UnityEngine.SceneManagement.LoadSceneMode.Single });
            CheckGroup(scene, player, 3);
            results.Add("PASS " + paths[index] + ": all three persist; repeated callback and revisits create no duplicates");
            if (index == 7) {
                beforeDeathFollowers.Clear();
                foreach (var pet in Pets(scene)) beforeDeathFollowers.Add(pet.GetEntityId());
                beforeDeathPlayer = player.GetEntityId(); playerSpawn = player.transform.position;
                index++; deadline = now + 15;
                health.TakeDamage(health.MaximumHealth, UnityEngine.Vector2.right, 0);
                Check(health.IsDead && BloomCompanionSession.Count == 3, "Health death lost recruited companions");
                return;
            }
        } else if (index == 8 || index == 12) {
            Check(!health.IsDead && health.CurrentHealth == health.MaximumHealth, "Death did not restore player health");
            CheckGroup(scene, player, 3);
            Check(Pets(scene).Length == 3, "Respawn duplicated the original pickup or follower group");
            Check(player.GetEntityId() != beforeDeathPlayer && UnityEngine.Vector3.Distance(player.transform.position, playerSpawn) < .01f,
                "Player did not restart at the entrance");
            foreach (var pet in Pets(scene)) {
                Check(!beforeDeathFollowers.Contains(pet.GetEntityId()), "Follower from the dead scene survived reload");
                Check(UnityEngine.Mathf.Abs(pet.Feet.x - player.transform.position.x) < 5f,
                    "Companion did not respawn beside Lenny");
            }
            results.Add("PASS " + (index == 8 ? "health death" : "lethal acid") + ": actual scene restart respawns all three beside the new Lenny, still recruited, without duplicates");
        } else if (index == 9 || index == 10) {
            CheckGroup(scene, player, 3);
            Check(Pets(scene).Length == 3, "Previously collected pickup reappeared as an extra companion");
            results.Add("PASS " + paths[index] + ": recruitment survives death and the authored companion joins the existing group once");
        } else if (index == 11) {
            CheckGroup(scene, player, 3);
            var hazard = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Level02ThornHazard>()).First();
            beforeDeathFollowers.Clear();
            foreach (var pet in Pets(scene)) beforeDeathFollowers.Add(pet.GetEntityId());
            beforeDeathPlayer = player.GetEntityId(); playerSpawn = player.transform.position;
            index++; deadline = now + 15;
            typeof(Level02ThornHazard).GetMethod("OnTriggerEnter2D", flags).Invoke(hazard, new object[] { player.GetComponent<UnityEngine.Collider2D>() });
            Check(BloomCompanionSession.Count == 3, "Lethal hazard lost recruited companions");
            return;
        } else if (index == 13 || index == 14) {
            CheckGroup(scene, player, 3);
            Check(Pets(scene).Length == 3, "Hazard respawn made a collected pickup available twice");
            results.Add("PASS " + paths[index] + ": earlier-level companions remain recruited after acid death, without duplicate pickups");
        } else if (index == 15) {
            var intro = UnityEngine.Object.FindAnyObjectByType<StoryIntroduction>();
            Check(intro.State == StoryIntroduction.IntroductionState.Dialogue && !player.enabled, "Story introduction did not pause controls");
            intro.DismissDialogue();
            typeof(StoryIntroduction).GetMethod("AdvanceWalk", flags).Invoke(intro, new object[] { 1.1f });
            Check(intro.State == StoryIntroduction.IntroductionState.Complete && player.enabled, "Story did not restore controls");
            var pet = Pets(scene).Single();
            Check(pet.enabled, "Story did not restore the companion component");
            Recruit(pet, player); CheckGroup(scene, player, 1);
            results.Add("PASS Level 1 story copy: same Petalborn placement; introduction pauses/resumes; touch recruitment works afterward");
        }
        index++; LoadCase();
    } catch (System.Exception error) { Finish("case " + index + ": " + error); }
};
System.IO.File.WriteAllText("Temp/BloomCompanionRunResults.txt", "RUNNING companion integration checks");
UnityEngine.SceneManagement.SceneManager.sceneLoaded += onLoaded;
UnityEditor.EditorApplication.update += tick;
LoadCase();
return "Scheduled 17 scene cases; results in Temp/BloomCompanionRunResults.txt";
