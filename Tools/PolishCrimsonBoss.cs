if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play before updating the boss.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Open Boss first.");
System.IO.Directory.CreateDirectory(".local-backups");UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,".local-backups/Boss-before-ability-polish-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
BlightBoss boss=null;foreach(var root in scene.GetRootGameObjects()){var b=root.GetComponentInChildren<BlightBoss>(true);if(b!=null)boss=b;}if(boss==null)throw new System.Exception("Boss missing.");
var so=new UnityEditor.SerializedObject(boss);
void Set(string name,float value){so.FindProperty(name).floatValue=value;}
Set("movementSpeed",2.1f);Set("phaseTwoMovementSpeed",2.8f);
Set("slamWindup",.6f);Set("slamRecovery",.7f);Set("slamCooldown",1.65f);Set("shockwaveSpeed",6.5f);
Set("lungeWindup",.5f);Set("lungeRecovery",.65f);Set("lungeCooldown",1.55f);Set("lungeSpeed",9f);
Set("projectileWindup",.65f);Set("projectileRecovery",.48f);Set("projectileCooldown",2f);Set("projectileSpeed",6.2f);Set("projectileSpread",18f);
Set("thornWindup",.7f);Set("thornRecovery",.65f);Set("thornCooldown",2.3f);Set("comboDelay",.35f);Set("phaseTransitionDuration",.95f);
so.ApplyModifiedPropertiesWithoutUndo();
var effects=boss.GetComponent<CrimsonBossEffects>();if(effects==null)effects=boss.gameObject.AddComponent<CrimsonBossEffects>();effects.effectMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Sprites/EnemyCharacters/CrimsonBoss-Unlit.mat");
if(boss.GetComponent<CrimsonBossMelee>()==null)boss.gameObject.AddComponent<CrimsonBossMelee>();
UnityEditor.EditorUtility.SetDirty(effects);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
return "Saved faster boss timings, scene-only attack effects and close-range melee swipes (14 damage, .22s warning, .85s cooldown).";
