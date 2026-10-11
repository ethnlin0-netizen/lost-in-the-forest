if(!UnityEditor.EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/GameLoop/Boss.unity")throw new System.Exception("Test the updated Boss in Play mode.");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
void Check(bool condition,string message){if(!condition)throw new System.Exception(message);}
var ending=UnityEngine.Object.FindAnyObjectByType<StoryEnding>();var reward=UnityEngine.Object.FindAnyObjectByType<BossCrystalReward>();var exit=reward.crystal.GetComponent<BossCrystalExit>();var player=ending.player;var collider=player.GetComponent<UnityEngine.Collider2D>();
var trigger=typeof(BossCrystalExit).GetMethod("OnTriggerEnter2D",flags);
Check(ending.waitForCrystal&&!ending.dialogue.canvas.enabled,"Ending should begin hidden");
trigger.Invoke(exit,new object[]{collider});Check(ending.State==StoryEnding.EndingState.WaitingForBoss,"Crystal played ending before defeat");
StoryRunSession.EnsureStoryRun();Check(StoryRunSession.Destination("Boss")=="Assets/Scenes/GameLoop/Boss.unity","Full run still loads outdated boss");
ending.boss.TakeDamage(100000,UnityEngine.Vector2.zero,0);
Check(reward.Revealed&&ending.State==StoryEnding.EndingState.WaitingForBoss&&!StoryRunSession.BlocksGameplay,"Defeat should reveal crystal before starting ending");
// Reproduce collection after the death animation has removed the boss object.
UnityEngine.Object.DestroyImmediate(ending.boss.gameObject);
Check(ending.boss==null,"Boss must be gone for this regression check");
trigger.Invoke(exit,new object[]{collider});Check(ending.State==StoryEnding.EndingState.Waking&&ending.dialogue.canvas.enabled&&StoryRunSession.BlocksGameplay&&UnityEngine.Time.timeScale==0,"Crystal did not start/pause for ending");
trigger.Invoke(exit,new object[]{collider});Check(ending.State==StoryEnding.EndingState.Waking,"Repeated crystal restarted ending");
typeof(StoryEnding).GetMethod("AdvanceAnimation",flags).Invoke(ending,new object[]{ending.wakeDuration+.01f});
Check(ending.State==StoryEnding.EndingState.Celebrating,"Wake animation failed");
typeof(StoryEnding).GetMethod("AdvanceAnimation",flags).Invoke(ending,new object[]{ending.celebrationDuration+.01f});
Check(ending.State==StoryEnding.EndingState.AwaitingReturn&&ending.dialogue.message.text=="T.K. is awake!","Ending text/animation failed");
ending.ReturnToTitle();Check(ending.State==StoryEnding.EndingState.ReturningToTitle,"Ending return failed");
return "PASS: no cutscene before defeat/collection; crystal opens teammates' waking/celebration ending; gameplay paused; duplicate trigger ignored; return-to-title accepted; full run uses updated Boss.";
