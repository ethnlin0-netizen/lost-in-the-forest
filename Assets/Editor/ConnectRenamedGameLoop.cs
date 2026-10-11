using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConnectRenamedGameLoop
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(!EditorSceneManager.SaveOpenScenes())throw new IOException("Save scenes first.");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string level3="Assets/Scenes/Ashpetal Level 03.unity";
        const string level5="Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity";
        const string boss="Assets/Scenes/GameLoop/Boss.unity";
        foreach(var required in new[]{level5,boss})if(!File.Exists(required))throw new IOException("Missing scene: "+required);
        var list=EditorBuildSettings.scenes.ToList();
        // Resolve only the stale Level 3 list entry left by the local rename.
        for(int i=0;i<list.Count;i++)
            if(list[i].path=="Assets/Scenes/GameLoop/Level03.unity"&&!File.Exists(list[i].path)&&File.Exists(level3))
                list[i]=new EditorBuildSettingsScene(level3,list[i].enabled);
        foreach(var path in new[]{boss})
        {
            if(!File.Exists(path))continue;
            list.RemoveAll(s=>s.path==path);list.Add(new EditorBuildSettingsScene(path,true));
        }
        EditorBuildSettings.scenes=list.ToArray();
        int crystals=0;
        foreach(var path in new[]{level5,"Assets/Scenes/GameLoop/Wraithglass Level 05.unity","Assets/Scenes/Level05.unity"})
        {
            if(!File.Exists(path))continue;
            var scene=EditorSceneManager.OpenScene(path);bool changed=false;
            foreach(var root in scene.GetRootGameObjects())foreach(var pickup in root.GetComponentsInChildren<AmuletPickup>(true))
            {
                string target=pickup.amuletNumber==5?boss:null;
                if(target==null)continue;
                pickup.nextScene=Path.GetFileNameWithoutExtension(target);
                EditorUtility.SetDirty(pickup);changed=true;crystals++;
            }
            if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        }
        if(!string.IsNullOrEmpty(original)&&File.Exists(original))EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();return "Connected "+crystals+" crystals; Level 5 destination="+boss;
    }
}
