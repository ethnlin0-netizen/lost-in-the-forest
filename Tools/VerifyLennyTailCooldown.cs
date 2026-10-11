if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use temporary Play mode.");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Check(bool value,string message){if(!value)throw new System.Exception(message);}
var originalScale=UnityEngine.Time.timeScale;UnityEngine.GameObject lenny=null,target=null;
try{
 UnityEngine.Time.timeScale=1;
 lenny=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab"),new UnityEngine.Vector3(300,20,0),UnityEngine.Quaternion.identity);
 var combat=lenny.GetComponent<PlayerCombat>();var animation=lenny.GetComponent<LennyAnimation>();var hull=lenny.GetComponent<UnityEngine.Collider2D>();
 var perform=typeof(PlayerCombat).GetMethod("PerformMelee",flags);var next=typeof(PlayerCombat).GetField("nextMeleeAt",flags);float reach=(float)typeof(PlayerCombat).GetField("tailSweepReach",flags).GetValue(combat);
 Check(reach==1.8f,"Existing tail hit range changed");
 target=new UnityEngine.GameObject("Tail range test target");target.transform.position=hull.bounds.center+UnityEngine.Vector3.right*(hull.bounds.extents.x+reach-.05f);target.AddComponent<UnityEngine.Rigidbody2D>().bodyType=UnityEngine.RigidbodyType2D.Kinematic;target.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(.1f,.2f);var health=target.AddComponent<EnemyHealth>();UnityEngine.Physics2D.SyncTransforms();
 var tail=(UnityEngine.Transform)typeof(LennyAnimation).GetField("tail",flags).GetValue(animation);var rest=tail.localScale;var hullBefore=UnityEditor.EditorJsonUtility.ToJson(hull);int events=0;combat.MeleePerformed+=(charged,direction,duration,distance)=>events++;
 perform.Invoke(combat,new object[]{true});Check(events==1&&health.CurrentHealth==30,"First strike failed at original range");float restingReach=(float)typeof(LennyAnimation).GetField("restingTailReach",flags).GetValue(animation);float expectedReach=hull.bounds.extents.x+reach;Check(UnityEngine.Mathf.Abs(tail.localScale.x/rest.x*restingReach+tail.localPosition.x-expectedReach)<.01f,"Tail does not extend to attack reach");Check(UnityEditor.EditorJsonUtility.ToJson(hull)==hullBefore,"Tail pose changed player hitbox");Check(combat.MeleeCooldownRemaining>.25f&&combat.MeleeCooldownRemaining<=.301f,"Expected 0.3-second cooldown");Check(lenny.transform.Find("Melee Tail Sweep")==null,"Placeholder box still on shared player prefab");
 for(int i=0;i<12;i++)perform.Invoke(combat,new object[]{true});Check(events==1&&health.CurrentHealth==30,"Spam bypassed cooldown");
 combat.enabled=false;combat.enabled=true;perform.Invoke(combat,new object[]{true});Check(events==1,"Disable/re-enable bypassed cooldown");
 next.SetValue(combat,UnityEngine.Time.time-.01f);UnityEngine.Time.timeScale=0;perform.Invoke(combat,new object[]{true});Check(events==1,"Attack allowed during pause");UnityEngine.Time.timeScale=1;
 perform.Invoke(combat,new object[]{true});Check(events==2&&health.IsDead,"Strike failed after cooldown");
 return "PASS: original 1.8-unit damage reach, full forward tail extension, placeholder removed, unchanged hull, 0.3-second cooldown, repeated presses ignored, pause/re-enable protected, attack available after cooldown.";
}finally{if(target!=null)UnityEngine.Object.DestroyImmediate(target);if(lenny!=null)UnityEngine.Object.DestroyImmediate(lenny);UnityEngine.Time.timeScale=originalScale;}
