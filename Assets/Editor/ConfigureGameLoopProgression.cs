using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureGameLoopProgression
{
    private static readonly string[] Names={"TitleScreen","PrismbloomLevel01","GloamthorneLevel02","Ashpetal Level 03","Veinroot Level04","Wraithglass Barrens Level 05","Boss"};
    [MenuItem("Tools/Lost In The Forest/Connect GameLoop Scenes")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save open scenes first.");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var paths=Names.Select(n=>"Assets/Scenes/GameLoop/"+n+".unity").ToArray();
        foreach(var path in paths)if(!File.Exists(path))throw new IOException("Missing scene: "+path);
        int pickups=0;
        for(int i=1;i<=5;i++)
        {
            var scene=EditorSceneManager.OpenScene(paths[i]);
            var crystals=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AmuletPickup>(true)).Where(p=>p.amuletNumber==i).ToArray();
            if(crystals.Length!=1)throw new InvalidOperationException("Expected one crystal in "+Names[i]);
            crystals[0].nextScene=Names[i+1];EditorUtility.SetDirty(crystals[0]);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);pickups++;
        }
        var title=EditorSceneManager.OpenScene(paths[0]);
        foreach(var menu in title.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TitleMenu>(true)))
        {
            var serialized=new SerializedObject(menu);serialized.FindProperty("firstBiomeScene").stringValue=Names[1];serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(title);EditorSceneManager.SaveScene(title);
        // Keep the older scene assets available for editing, but make the build
        // and name-based scene loads use only the agreed GameLoop sequence.
        EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(original)&&File.Exists(original))EditorSceneManager.OpenScene(original);
        return "Configured seven GameLoop build scenes and "+pickups+" crystal destinations.";
    }
}
