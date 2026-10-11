if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var names=new[]{"PrismbloomLevel01","GloamthorneLevel02","Ashpetal Level 03","Boss"};
var tracks=new[]{"PrismbloodForest_Loop","GloamthornCanopy_Loop","AshpetalForest_Loop","DreadcrownForest_Loop"};
var beds=new[]{"Prismbloom","Gloamthorne","Ashpetal","Boss"};
int index=0,phase=0,sourceSample=0,musicSample=0,wraps=0;double deadline=0;
var results=new System.Collections.Generic.List<string>();
GameAudioController current=null,previous=null;UnityEngine.AudioSource bed=null,music=null;BossCrystalReward reward=null;int touches=0;
var oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
UnityEditor.EditorApplication.CallbackFunction tick=null;
void Check(bool pass,string text){if(!pass)throw new System.Exception(names[System.Math.Min(index,3)]+": "+text);results.Add("PASS: "+text);}
void Load(){previous=current;current=null;UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/GameLoop/"+names[index]+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));phase=0;deadline=UnityEditor.EditorApplication.timeSinceStartup+.8;}
void Finish(string error=null){UnityEditor.EditorApplication.update-=tick;UnityEngine.AudioListener.pause=false;UnityEngine.Time.timeScale=1;UnityEngine.Application.runInBackground=oldBackground;if(error!=null)results.Add("FAIL: "+error);System.IO.File.WriteAllLines("Temp/sfx-work/scene-test.txt",results);}
tick=()=>{if(UnityEditor.EditorApplication.timeSinceStartup<deadline)return;try{
 if(!UnityEditor.EditorApplication.isPlaying){Finish("Play mode exited");return;}
 if(phase==0){
  current=GameAudioController.Instance;Check(current!=null&&UnityEngine.Object.FindObjectsByType<GameAudioController>().Length==1,names[index]+" has one audio controller");Check(previous==null,names[index]+" unloads prior scene controller");
  bed=current.ambience;music=current.music;
  Check(bed!=null&&bed.clip.name==beds[index]+"_Ambience"&&bed.isPlaying&&bed.loop,names[index]+" correct ambience autoplays and loops");
  Check(music!=null&&music.clip.name==tracks[index]&&music.isPlaying&&music.loop,names[index]+" preserves its existing music");
  Check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>().Count(s=>s.loop)==2,names[index]+" has exactly one music and one ambience source");
  Check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>().Count(l=>l.enabled)==1,names[index]+" has one listener");
  foreach(var m in UnityEngine.Object.FindObjectsByType<Movement>())Check(m.GetComponent<GameplaySfx>()?.role==GameplaySfx.Role.Player,names[index]+" player has its event adapter");
  foreach(var e in UnityEngine.Object.FindObjectsByType<BlightEnemy>())Check(e.GetComponent<GameplaySfx>()!=null,names[index]+" enemy "+e.name+" has its event adapter");
  sourceSample=bed.timeSamples;musicSample=music.timeSamples;phase=1;deadline=UnityEditor.EditorApplication.timeSinceStartup+.3;
 }else if(phase==1){
  Check(bed.timeSamples>sourceSample&&music.timeSamples>musicSample,names[index]+" music and ambience cursors advance");
  UnityEngine.AudioListener.pause=true;phase=2;deadline=UnityEditor.EditorApplication.timeSinceStartup+.2;
 }else if(phase==2){sourceSample=bed.timeSamples;musicSample=music.timeSamples;phase=3;deadline=UnityEditor.EditorApplication.timeSinceStartup+.25;
 }else if(phase==3){
  Check(System.Math.Abs(bed.timeSamples-sourceSample)<=256&&System.Math.Abs(music.timeSamples-musicSample)<=256,names[index]+" music and ambience stop during listener pause");
  UnityEngine.AudioListener.pause=false;phase=4;deadline=UnityEditor.EditorApplication.timeSinceStartup+.25;
 }else if(phase==4){
  Check(bed.isPlaying&&bed.timeSamples>sourceSample,names[index]+" ambience resumes");bed.time=bed.clip.length-.1f;phase=5;deadline=UnityEditor.EditorApplication.timeSinceStartup+.4;
 }else if(phase==5){
  Check(bed.isPlaying&&bed.time<1f,names[index]+" ambience wraps across its seam");index++;
  if(index<names.Length)Load();else{
   reward=UnityEngine.Object.FindAnyObjectByType<BossCrystalReward>();Check(reward!=null,"Boss has existing crystal reward");
   var boss=UnityEngine.Object.FindAnyObjectByType<BlightBoss>();Check(boss!=null,"Boss actor exists in authored scene");
   boss.GetComponent<BlightBossHealth>().TakeDamage(100000,UnityEngine.Vector2.right,0);
   Check(reward.Revealed&&reward.crystal.activeSelf&&current.Played(GameSound.CrystalAppear)==1,"Actual boss defeat reveals crystal with one appearance cue");
   var player=UnityEngine.Object.FindAnyObjectByType<Movement>();player.GetComponent<UnityEngine.Rigidbody2D>().position=reward.crystal.transform.position;UnityEngine.Physics2D.SyncTransforms();
   phase=6;deadline=UnityEditor.EditorApplication.timeSinceStartup+.4;
  }
 }else if(phase==6){
  Check(current.Played(GameSound.CrystalTouch)==1,"Actual crystal trigger plays one touch cue");
  Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="TitleScreen","Crystal returns to existing main menu");
  phase=7;deadline=UnityEditor.EditorApplication.timeSinceStartup+1.6;
 }else{
  Check(UnityEngine.Object.FindObjectsByType<GameAudioController>().Length==1,"Return to title leaves one audio controller");
  Check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>().Count(s=>s.loop)==0,"Return to title leaves no gameplay music/ambience");
  Check(!UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>().Any(s=>s.gameObject.name=="InteractionSoundTail"),"Cross-scene interaction tail cleans itself up");Finish();
 }
 System.IO.File.WriteAllLines("Temp/sfx-work/scene-test.txt",results);
}catch(System.Exception ex){Finish(ex.ToString());}};
UnityEditor.EditorApplication.update+=tick;Load();return "Scene playback and crystal checks scheduled.";
