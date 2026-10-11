var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Open Boss first");
UnityEngine.Camera camera=null;foreach(var r in scene.GetRootGameObjects()){var c=r.GetComponentInChildren<UnityEngine.Camera>(true);if(c!=null)camera=c;}
var previousTarget=camera.targetTexture;var active=UnityEngine.RenderTexture.active;var aspect=camera.aspect;
var rt=new UnityEngine.RenderTexture(1280,720,24);var image=new UnityEngine.Texture2D(1280,720,UnityEngine.TextureFormat.RGB24,false);
try{camera.aspect=1280f/720;camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;image.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes("Library/CrimsonBoss-Arena.png",UnityEngine.ImageConversion.EncodeToPNG(image));}
finally{camera.targetTexture=previousTarget;camera.aspect=aspect;UnityEngine.RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
return "Library/CrimsonBoss-Arena.png";
