if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var manager=GameAudioController.Instance;var results=new System.Collections.Generic.List<string>();
var original=UnityEngine.Object.FindAnyObjectByType<Movement>();original.gameObject.SetActive(false);
var originals=UnityEngine.Object.FindObjectsByType<BlightEnemy>();foreach(var e in originals)e.gameObject.SetActive(false);
var floor=new UnityEngine.GameObject("SFX lifetime fixture");floor.transform.position=new UnityEngine.Vector3(10000,-1.5f,0);floor.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(20,1);
var player=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab"),new UnityEngine.Vector3(10000,0,0),UnityEngine.Quaternion.identity);
var enemy=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/Chomper.prefab"),new UnityEngine.Vector3(10001.3f,0,0),UnityEngine.Quaternion.identity);
var health=enemy.GetComponent<EnemyHealth>();var ai=enemy.GetComponent<BlightEnemy>();
var menu=UnityEngine.Object.FindAnyObjectByType<TitleMenu>();
int phase=0,sample=0,initialImpact=manager.Played(GameSound.FireImpact);double start=UnityEditor.EditorApplication.timeSinceStartup,deadline=start;UnityEngine.AudioSource source=null;
var oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Check(bool value,string label){if(!value)throw new System.Exception(label);results.Add("PASS: "+label);}
void Finish(string error=null){UnityEditor.EditorApplication.update-=tick;if(menu.IsPaused)menu.StartGame();UnityEngine.Application.runInBackground=oldBackground;if(player!=null)UnityEngine.Object.Destroy(player);if(enemy!=null)UnityEngine.Object.Destroy(enemy);UnityEngine.Object.Destroy(floor);original.gameObject.SetActive(true);foreach(var e in originals)if(e!=null)e.gameObject.SetActive(true);if(error!=null)results.Add("FAIL: "+error);System.IO.File.WriteAllLines("Temp/sfx-work/lifetime-test.txt",results);}
tick=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying){Finish("Exited Play mode");return;}
 if(UnityEditor.EditorApplication.timeSinceStartup<deadline)return;
 if(phase==0){
  if(ai.State!=BlightEnemy.BehaviourState.Windup){if(UnityEditor.EditorApplication.timeSinceStartup-start>3)throw new System.Exception("Enemy did not enter windup");return;}
  source=manager.GetComponentsInChildren<UnityEngine.AudioSource>().FirstOrDefault(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="ChomperWarn");
  Check(source!=null,"Actual Chomper windup starts a playing warning voice");health.TakeDamage(1,UnityEngine.Vector2.right,0);
  Check(!source.isPlaying||source.clip==null||source.clip.name!="ChomperWarn","Accepted hit cancels an active windup warning immediately (voice may be reused for the hit)");phase=1;deadline=UnityEditor.EditorApplication.timeSinceStartup+.2;
 }else if(phase==1){
  Check(ai.AttacksPerformed==0,"Interrupted windup did not execute its bite");health.TakeDamage(100000,UnityEngine.Vector2.right,0);
  source=manager.GetComponentsInChildren<UnityEngine.AudioSource>().FirstOrDefault(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="ChomperDeath");Check(source!=null,"Death starts its detached voice");sample=source.timeSamples;UnityEngine.Object.Destroy(enemy);phase=2;deadline=UnityEditor.EditorApplication.timeSinceStartup+.15;
 }else if(phase==2){
  Check(enemy==null&&source.isPlaying&&source.timeSamples>sample,"Death cursor keeps advancing after actual actor destruction");
  var projectile=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<PlaceholderProjectile>("Assets/Prefabs/PlaceholderProjectile.prefab"),new UnityEngine.Vector3(10000,20,0),UnityEngine.Quaternion.identity);
  initialImpact=manager.Played(GameSound.FireImpact);projectile.Launch(UnityEngine.Vector2.right,12,.1f,null);phase=3;deadline=UnityEditor.EditorApplication.timeSinceStartup+.3;
 }else if(phase==3){
  Check(manager.Played(GameSound.FireImpact)==initialImpact,"Range expiry produces no collision/impact sound");menu.PauseGame();source=manager.GetComponentsInChildren<UnityEngine.AudioSource>().FirstOrDefault(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="Pause");Check(source!=null&&source.ignoreListenerPause,"Pause cue is exempt from listener pause");sample=source.timeSamples;phase=4;deadline=UnityEditor.EditorApplication.timeSinceStartup+.08;
 }else{Check(source.timeSamples>sample,"UI audio cursor advances while gameplay listener is paused");Finish();}
 System.IO.File.WriteAllLines("Temp/sfx-work/lifetime-test.txt",results);
}catch(System.Exception e){Finish(e.Message);}};
UnityEditor.EditorApplication.update+=tick;return "Lifetime checks scheduled";
