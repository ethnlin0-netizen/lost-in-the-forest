if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var file="Temp/sfx-work/import-cursor.txt";
int start=System.IO.File.Exists(file)?int.Parse(System.IO.File.ReadAllText(file)):0;
var paths=UnityEditor.AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio/SFX","Assets/Audio/Ambience"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/Sources/")).OrderBy(p=>p).ToArray();
for(int i=start;i<System.Math.Min(start+12,paths.Length);i++){
 var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(paths[i]);
 var settings=importer.defaultSampleSettings;
 bool bed=paths[i].StartsWith("Assets/Audio/Ambience/");
 settings.loadType=bed?UnityEngine.AudioClipLoadType.Streaming:UnityEngine.AudioClipLoadType.DecompressOnLoad;
 settings.compressionFormat=bed?UnityEngine.AudioCompressionFormat.Vorbis:UnityEngine.AudioCompressionFormat.PCM;
 settings.quality=.8f;settings.sampleRateSetting=UnityEditor.AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=!bed;
 importer.defaultSampleSettings=settings;importer.forceToMono=!bed;importer.loadInBackground=bed;
 importer.SaveAndReimport();
 System.IO.File.WriteAllText(file,(i+1).ToString());
}
return new{imported=System.IO.File.ReadAllText(file),total=paths.Length};
