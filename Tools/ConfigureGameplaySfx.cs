if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var records=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/GameplaySfxRecipes.json"));
var catalogPath="Assets/Audio/SFX/GameAudioCatalog.asset";
var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(catalogPath);
if(catalog==null){catalog=UnityEngine.ScriptableObject.CreateInstance<GameAudioCatalog>();UnityEditor.AssetDatabase.CreateAsset(catalog,catalogPath);}
catalog.cues=records.GroupBy(r=>(string)r["cue"]).Select(g=>new GameAudioCatalog.Cue{
 sound=(GameSound)System.Enum.Parse(typeof(GameSound),g.Key),clips=g.Select(r=>UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>((string)r["path"])).ToArray(),
 volume=(float)g.First()["volume"],minimumInterval=(float)g.First()["interval"],priority=(int)g.First()["priority"],maximumConcurrent=(int)g.First()["concurrent"],
 pitch=g.Key.Contains("Recruit")||g.Key.StartsWith("Menu")||g.Key=="CrystalTouch"||g.Key=="CrystalAppear" ? UnityEngine.Vector2.one : new UnityEngine.Vector2(.97f,1.03f)
}).ToArray();
if(catalog.cues.Any(c=>c.clips.Any(a=>a==null)))throw new System.Exception("Missing sound asset.");
UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssets();
var prefabRoles=new System.Collections.Generic.Dictionary<string,GameplaySfx.Role>{
 {"Assets/Prefabs/Player.prefab",GameplaySfx.Role.Player},
 {"Assets/Prefabs/Bosses/BossPlayer.prefab",GameplaySfx.Role.Player},
 {"Assets/Prefabs/Enemies/Chomper.prefab",GameplaySfx.Role.Chomper},
 {"Assets/Prefabs/Enemies/Spora.prefab",GameplaySfx.Role.Spora},
 {"Assets/Prefabs/Enemies/Collusus.prefab",GameplaySfx.Role.Collusus},
 {"Assets/Prefabs/Bosses/BlightAmalgamBoss.prefab",GameplaySfx.Role.Boss},
 {"Assets/Prefabs/PlaceholderProjectile.prefab",GameplaySfx.Role.Fireball},
 {"Assets/Prefabs/Enemies/SporeProjectile.prefab",GameplaySfx.Role.Spore},
 {"Assets/Prefabs/Bosses/BossSpore.prefab",GameplaySfx.Role.Spore},
 {"Assets/Prefabs/Bosses/BossShockwave.prefab",GameplaySfx.Role.Shockwave},
 {"Assets/Prefabs/BloomPets/WhisperseedPet.prefab",GameplaySfx.Role.Whisperseed},
 {"Assets/Prefabs/BloomPets/DryadPet.prefab",GameplaySfx.Role.Dryad},
 {"Assets/Prefabs/BloomPets/PetalbornPet.prefab",GameplaySfx.Role.Petalborn}
};
foreach(var entry in prefabRoles){
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(entry.Key);
 try{
  var sfx=root.GetComponent<GameplaySfx>();if(sfx==null)sfx=root.AddComponent<GameplaySfx>();
  sfx.role=entry.Value;sfx.stepDistance=entry.Value==GameplaySfx.Role.Boss?2f:entry.Value==GameplaySfx.Role.Collusus?1.2f:1.7f;
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,entry.Key);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
return new{cues=catalog.cues.Length,prefabs=prefabRoles.Count};
