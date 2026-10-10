if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Start Play mode in EnemyAI first.");
var player = UnityEngine.GameObject.Find("Player");
var health = player.GetComponent<PlayerHealth>();
var movement = player.GetComponent<Movement>();
var combat = player.GetComponent<PlayerCombat>();
var body = player.GetComponent<UnityEngine.Rigidbody2D>();
var fill = player.transform.Find("HealthBar/Background/Inset/Fill").GetComponent<UnityEngine.RectTransform>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Call(object o,string method) { o.GetType().GetMethod(method,flags).Invoke(o,null); }
void Assert(bool ok,string message) { if(!ok) throw new System.Exception(message); }
float startingHealth = health.MaximumHealth;
Assert(health.CurrentHealth == startingHealth && fill.anchorMax.x == 1f,"Fresh player should have maximum HP and a full bar.");
foreach(var enemy in UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None)) enemy.enabled = false;
health.TakeDamage(-5,UnityEngine.Vector2.zero,0);
Assert(health.CurrentHealth == startingHealth,"Negative damage changed HP.");
health.TakeDamage(15,UnityEngine.Vector2.right,5);
Assert(health.CurrentHealth == startingHealth - 15f && UnityEngine.Mathf.Abs(fill.anchorMax.x-(startingHealth-15f)/startingHealth)<0.001f,"Damage/bar ratio failed.");
var settings = new UnityEditor.SerializedObject(health);
var bodyVisual = (UnityEngine.SpriteRenderer)settings.FindProperty("bodyVisual").objectReferenceValue;
var combatSettings = new UnityEditor.SerializedObject(combat);
var chargeIndicator = (UnityEngine.SpriteRenderer)combatSettings.FindProperty("chargeIndicator").objectReferenceValue;
chargeIndicator.color = UnityEngine.Color.yellow;
Call(health,"Update");
Assert(bodyVisual.color == UnityEngine.Color.red,"Player hit flash failed.");
Assert(chargeIndicator.color == UnityEngine.Color.yellow,"Health feedback overwrote the charge visual.");
typeof(Movement).GetField("horizontalInput",flags).SetValue(movement,0f);
Call(movement,"FixedUpdate"); Call(health,"FixedUpdate");
Assert(body.linearVelocity.x>0,"Movement erased player knockback.");
for(int i=0;i<60;i++){ Call(movement,"FixedUpdate"); Call(health,"FixedUpdate"); }
Assert(UnityEngine.Mathf.Abs(body.linearVelocity.x)<0.01f,"Knockback failed to decay.");
int changes=0; health.HealthChanged += () => changes++;
health.TakeDamage(10,UnityEngine.Vector2.left,5);
Assert(health.CurrentHealth==startingHealth-15f && changes==0,"Overlapping hit bypassed invulnerability.");
typeof(PlayerHealth).GetField("invulnerableUntil",flags).SetValue(health,UnityEngine.Time.time-1f);
health.TakeDamage(startingHealth * 2f,UnityEngine.Vector2.right,5);
Assert(health.CurrentHealth == 0f && fill.anchorMax.x == 0f,"Death should clamp HP/bar to zero.");
Assert(!movement.enabled && !combat.enabled && !body.simulated && body.linearVelocity == UnityEngine.Vector2.zero,"Death left movement/combat/physics active.");
health.TakeDamage(10,UnityEngine.Vector2.zero,0);
Assert(changes==1 && health.CurrentHealth == 0,"Dead player took another hit.");
foreach(var enemy in UnityEngine.Object.FindObjectsByType<BlightEnemy>(UnityEngine.FindObjectsSortMode.None)) {
    bool visible = (bool)typeof(BlightEnemy).GetMethod("CanSeePlayer",flags).Invoke(enemy,null);
    Assert(!visible,"Enemy still targets dead player.");
}
return "Player HP/bar, hit feedback, preserved charge visuals, knockback decay, death disabling and dead-target rejection passed. Stop Play mode to restore the scene.";
