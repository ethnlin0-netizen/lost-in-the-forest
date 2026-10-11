if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required.");
var mode=UnityEngine.Physics2D.simulationMode;
var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Automatic enemy navigation verification");
var root=new UnityEngine.GameObject("Runtime test platforms and enemies");root.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
UnityEngine.GameObject Box(string name,float x,float top,float width){var go=new UnityEngine.GameObject(name);go.transform.SetParent(root.transform,false);go.transform.position=new UnityEngine.Vector3(x,top-.2f,0);go.AddComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(width,.4f);return go;}
var floor=Box("Test floor",1004,0,24);var step=Box("Test raised platform",1006,1f,3);
var shelf=Box("Jump-through shelf",1001,1.8f,6);shelf.GetComponent<UnityEngine.BoxCollider2D>().usedByEffector=true;shelf.AddComponent<UnityEngine.PlatformEffector2D>().useOneWay=true;
var enemyPrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/Chomper.prefab");
var actor=UnityEngine.Object.Instantiate(enemyPrefab,root.transform);actor.transform.position=new UnityEngine.Vector3(1000,.8f,0);
var second=UnityEngine.Object.Instantiate(enemyPrefab,root.transform);second.transform.position=new UnityEngine.Vector3(1013,.8f,0);
var results=new System.Collections.Generic.List<string>();
void Check(bool condition,string message){if(!condition)throw new System.Exception(message);}
try{
UnityEngine.Physics2D.simulationMode=UnityEngine.SimulationMode2D.Script;root.SetActive(true);actor.GetComponent<BlightEnemy>().enabled=false;second.GetComponent<BlightEnemy>().enabled=false;second.SetActive(false);
UnityEngine.Physics2D.SyncTransforms();var world=PlatformNavigation2D.GetOrCreate(scene);world.RebuildSurfaces();
Check(UnityEngine.Object.FindObjectsByType<PlatformNavigation2D>(UnityEngine.FindObjectsSortMode.None).Count(n=>n.gameObject.scene==scene)==1,"More than one runtime map per scene.");Check(world.Surfaces.Count==3&&world.Surfaces.Contains(floor.GetComponent<UnityEngine.BoxCollider2D>())&&world.Surfaces.Contains(step.GetComponent<UnityEngine.BoxCollider2D>()),"Map missed scene geometry or included other scenes.");Check(PlatformNavigation2D.GetOrCreate(scene)==world,"Map not reused.");results.Add("Multiple dropped-in prefabs share one automatic map of their own scene's static box platforms.");
var nav=actor.GetComponent<EnemyNavigator2D>();var body=actor.GetComponent<UnityEngine.Rigidbody2D>();for(int i=0;i<30;i++)UnityEngine.Physics2D.Simulate(.02f);
nav.SetDestination(new UnityEngine.Vector2(1003,0));for(int i=0;i<130;i++){float speed=nav.Tick(.02f,4.5f);body.linearVelocity=new UnityEngine.Vector2(speed,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(.02f);}
Check(nav.IsGrounded&&UnityEngine.Mathf.Abs(nav.Feet.x-1003)<.35f,"Automatic-map walk failed: "+nav.Feet);
nav.SetDestination(new UnityEngine.Vector2(1001,1.8f));for(int i=0;i<700;i++){float speed=nav.Tick(.02f,4.5f);body.linearVelocity=new UnityEngine.Vector2(speed,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(.02f);}
Check(world.SupportAt(nav.Feet)==shelf.GetComponent<UnityEngine.BoxCollider2D>()&&nav.JumpsPerformed>0,"Jump-through shelf route failed: "+nav.Feet);results.Add("Enemy physically jumps upward through a one-way shelf and lands on top.");
nav.SetDestination(new UnityEngine.Vector2(1006,1));for(int i=0;i<700;i++){float speed=nav.Tick(.02f,4.5f);body.linearVelocity=new UnityEngine.Vector2(speed,body.linearVelocity.y);UnityEngine.Physics2D.Simulate(.02f);}
Check(world.SupportAt(nav.Feet)==step.GetComponent<UnityEngine.BoxCollider2D>()&&nav.JumpsPerformed>0,"Automatic-map jump failed: "+nav.Feet);results.Add("Real Rigidbody2D navigation walks to its destination and jumps onto a raised platform.");
var wall=Box("Solid obstruction",1011,5f,.5f);wall.GetComponent<UnityEngine.BoxCollider2D>().size=new UnityEngine.Vector2(.5f,10f);UnityEngine.Physics2D.SyncTransforms();Check(!world.ClearSegment(new UnityEngine.Vector2(1010,.04f),new UnityEngine.Vector2(1012,.04f),actor.GetComponent<UnityEngine.BoxCollider2D>().size),"Solid wall incorrectly treated as pass-through.");UnityEngine.Object.DestroyImmediate(wall);results.Add("Solid walls still block body-clearance checks.");
var added=Box("New platform",1010,1.5f,2);UnityEngine.Physics2D.SyncTransforms();int revision=world.Revision;typeof(PlatformNavigation2D).GetField("checkRemaining",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(world,0f);typeof(PlatformNavigation2D).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(world,null);
Check(world.Revision>revision&&world.Surfaces.Contains(added.GetComponent<UnityEngine.BoxCollider2D>()),"Automatic geometry refresh failed.");results.Add("New static platforms invalidate and refresh the automatic map.");
UnityEngine.Object.DestroyImmediate(world.gameObject);
var authored=root.AddComponent<PlatformNavigation2D>();authored.RebuildSurfaces();Check(PlatformNavigation2D.GetOrCreate(scene)==authored,"Authored map replaced by automatic map.");Check(authored.Surfaces.Count==4,"Authored hierarchy scan changed.");results.Add("Existing authored navigation is reused and retains its hierarchy-scoped platforms.");
return Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks=results,simulation="Play mode with actual Rigidbody2D physics and manually stepped navigator."});
}finally{UnityEngine.Physics2D.simulationMode=mode;UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);}
