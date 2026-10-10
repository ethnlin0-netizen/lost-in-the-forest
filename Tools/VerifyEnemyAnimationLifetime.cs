// Editor eval statement block. Uses temporary Play Mode instances and reports through SessionState.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("EnemyAI Play Mode required.");
UnityEditor.SessionState.SetBool("EnemyAnimBackground",UnityEngine.Application.runInBackground);
UnityEngine.Application.runInBackground=true;
foreach(var enemy in UnityEngine.Object.FindObjectsByType<BlightEnemy>())enemy.gameObject.SetActive(false);
foreach(var player in UnityEngine.Object.FindObjectsByType<Movement>())player.gameObject.SetActive(false);
var names=new[]{"Chomper","Spora","Collusus"};
var sample=new UnityEngine.GameObject[3];
for(int i=0;i<names.Length;i++){
    sample[i]=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/"+names[i]+".prefab"));
    sample[i].name="Animation lifetime preview "+names[i];
    sample[i].transform.position=new UnityEngine.Vector3(-3+i*3,0,0);
    sample[i].GetComponent<BlightEnemy>().enabled=false;
    sample[i].GetComponent<EnemyNavigator2D>().enabled=false;
    sample[i].GetComponent<UnityEngine.Rigidbody2D>().simulated=false;
}
foreach(var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>())
    if(renderer.GetComponentInParent<BlightEnemy>()==null)renderer.enabled=false;
foreach(var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>())
    if(canvas.GetComponentInParent<BlightEnemy>()==null)canvas.gameObject.SetActive(false);
var camera=UnityEngine.Camera.main;
foreach(var behaviour in camera.GetComponents<UnityEngine.MonoBehaviour>())
    if(!(behaviour is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))behaviour.enabled=false;
camera.transform.position=new UnityEngine.Vector3(0,.35f,-10);
camera.orthographicSize=2.3f;
camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
camera.backgroundColor=new UnityEngine.Color(.08f,.1f,.16f);
void Capture(string name){
    var texture=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    System.IO.File.WriteAllBytes("Library/EnemyAnimations"+name+".png",UnityEngine.ImageConversion.EncodeToPNG(texture));
    UnityEngine.Object.Destroy(texture);
}
UnityEditor.SessionState.SetString("EnemyAnimationLifetimeResult","Running");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
int phase=0; float start=UnityEngine.Time.time, death=0, pausedTime=0;
double wallStart=UnityEditor.EditorApplication.timeSinceStartup, pauseStarted=0;
UnityEngine.Vector3[] pausedMesh=null;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    try{
        if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play Mode ended during verification.");
        if(UnityEditor.EditorApplication.timeSinceStartup-wallStart>25)throw new System.Exception("Animation lifetime test timed out.");
        float elapsed=UnityEngine.Time.time-start;
        if(phase==0 && elapsed>.2f){
            Capture("Idle");
            foreach(var go in sample){var a=go.GetComponent<EnemyVisualAnimator>();typeof(EnemyVisualAnimator).GetField("windupBlend",flags).SetValue(a,1f);typeof(EnemyVisualAnimator).GetMethod("Pose",flags).Invoke(a,new object[]{0f});}
            UnityEngine.Time.timeScale=0;pauseStarted=UnityEditor.EditorApplication.timeSinceStartup;phase=1;
        }else if(phase==1 && UnityEditor.EditorApplication.timeSinceStartup-pauseStarted>.15){
            Capture("Windup");
            foreach(var go in sample){var a=go.GetComponent<EnemyVisualAnimator>();typeof(EnemyVisualAnimator).GetField("windupBlend",flags).SetValue(a,0f);typeof(EnemyVisualAnimator).GetField("strikeAt",flags).SetValue(a,UnityEngine.Time.time);typeof(EnemyVisualAnimator).GetMethod("Pose",flags).Invoke(a,new object[]{0f});}
            pauseStarted=UnityEditor.EditorApplication.timeSinceStartup;phase=2;
        }else if(phase==2 && UnityEditor.EditorApplication.timeSinceStartup-pauseStarted>.15){
            Capture("Strike");UnityEngine.Time.timeScale=1;death=UnityEngine.Time.time;
            foreach(var go in sample)go.GetComponent<EnemyHealth>().TakeDamage(1000,UnityEngine.Vector2.right,0);
            phase=3;
        }else if(phase==3 && UnityEngine.Time.time-death>.4f){
            foreach(var go in sample)if(go==null)throw new System.Exception("Enemy was removed before its death animation finished.");
            UnityEngine.Time.timeScale=0;pausedTime=UnityEngine.Time.time;pauseStarted=UnityEditor.EditorApplication.timeSinceStartup;
            pausedMesh=sample[0].GetComponent<EnemyVisualAnimator>().AnimatedMesh.vertices;phase=4;
        }else if(phase==4 && UnityEditor.EditorApplication.timeSinceStartup-pauseStarted>.3){
            if(UnityEngine.Time.time!=pausedTime)throw new System.Exception("Death time advanced during pause.");
            var pose=sample[0].GetComponent<EnemyVisualAnimator>().AnimatedMesh.vertices;
            for(int i=0;i<pose.Length;i++)if(pose[i]!=pausedMesh[i])throw new System.Exception("Death pose advanced during pause.");
            UnityEngine.Time.timeScale=1;phase=5;
        }else if(phase==5 && UnityEngine.Time.time-death>.65f){
            Capture("Death");
            int count=0;foreach(var go in sample)count+=go.GetComponentInChildren<UnityEngine.ParticleSystem>().particleCount;
            if(count==0)throw new System.Exception("Death particles did not simulate.");
            phase=6;
        }else if(phase==6 && UnityEngine.Time.time-death>2.4f){
            foreach(var go in sample)if(go!=null)throw new System.Exception("Enemy was not removed after its complete death animation.");
            UnityEditor.SessionState.SetString("EnemyAnimationLifetimeResult","PASS: all death animations survive the original 0.4s removal, freeze during pause, show live particles, and remove enemies after their full durations.");
            UnityEditor.EditorApplication.update-=tick;
        }
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }catch(System.Exception error){
        UnityEngine.Time.timeScale=1;
        UnityEditor.SessionState.SetString("EnemyAnimationLifetimeResult","FAIL: "+error.Message);
        UnityEditor.EditorApplication.update-=tick;
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Started real-time death, pause, particle and visual capture verification.";
