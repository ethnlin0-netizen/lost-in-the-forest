// Connect only the two separate biome scenes; leave original Level01/Level02 unchanged.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Save open scene work first.");
const string destination = "Assets/Scenes/GloamthorneLevel02.unity";
if (!System.IO.File.Exists(destination)) throw new System.InvalidOperationException("Create Gloamthorne first.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/PrismbloomLevel01.unity");
var pickups = UnityEngine.Object.FindObjectsByType<AmuletPickup>();
AmuletPickup entrancePickup = null;
foreach (var pickup in pickups) if (pickup.amuletNumber == 1) entrancePickup = pickup;
if (entrancePickup == null) throw new System.InvalidOperationException("Course 1 crystal missing.");
entrancePickup.nextScene = "GloamthorneLevel02";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(destination);
return new { crystal1Destination = "GloamthorneLevel02", readyScene = destination };
