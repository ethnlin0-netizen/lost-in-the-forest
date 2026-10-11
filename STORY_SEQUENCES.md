# T.K. story sequences

Work is on `friend-and-enemy-sprites`.

## Scenes and route

- `Assets/Scenes/GameLoop/PrismbloomLevel01Story.unity` uses the latest `Assets/Scenes/GameLoop/PrismbloomLevel01.unity`, with sleeping T.K. and an introduction.
- `Assets/Scenes/GameLoop/BossStory.unity` uses the latest `Assets/Scenes/GameLoop/Boss.unity`, with a defeat-triggered ending.
- Both copies are enabled in the build scene list. Original scenes, character prefabs, artwork, and level geometry are preserved.

Open `Assets/Scenes/GameLoop/TitleScreen.unity` and press Play, then select the game's Play button. The connected route is:

1. `Assets/Scenes/GameLoop/PrismbloomLevel01Story.unity`
2. `Assets/Scenes/GameLoop/GloamthorneLevel02.unity`
3. `Assets/Scenes/GameLoop/Ashpetal Level 03.unity`
4. `Assets/Scenes/GameLoop/Veinroot Level04.unity`
5. `Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity`
6. `Assets/Scenes/GameLoop/BossStory.unity`
7. `Assets/Scenes/GameLoop/TitleScreen.unity` after the ending is dismissed.

`TitleMenu` starts the story introduction. During a story run, `AmuletPickup` asks `StoryRunSession.Destination` for an exact scene path. This selects the finished GameLoop scenes and changes the existing Wraithglass crystal's Boss destination to `BossStory`, without changing the original scene assets. The obsolete Level06 bridge has been removed. Outside a story run, amulet destinations retain their existing behavior.

The enabled Ashpetal build entry now points to its GameLoop scene, rather than the older copy directly under Scenes. Both story scene GUIDs and their existing story UI, sprite references, and timing settings are retained. The refreshed copies include the current source scenes' audio setup and gameplay.

You can also open either story copy directly in the editor to preview its sequence. Playing the intro directly starts a story run; playing the boss directly shows the ending when its final boss dies.

## Introduction

T.K. is an instance of `TKVisual`, rotated 90 degrees and positioned on the entrance floor. His source PNG is unchanged.

Lenny says: **“I have to find all six amulet pieces to save my friend T.K.!”**

Press **Enter or Space** to dismiss the dialogue. A key already being held must be released first. Lenny moves **two world units right over one second**, then movement, attacks, physics, enemies, and game time resume. The walk uses unscaled time while the game is paused.

The introduction flag belongs to the current run in memory. Death and scene restarts keep it, while Play after returning to the title resets it. No saved-game progress or PlayerPrefs are introduced.

## Ending

`StoryEnding` subscribes to the final boss's existing `EnemyHealth.Died` event. Nonlethal hits do not trigger it; boss death still reveals the original crystal reward and runs the original defeat handlers.

The story overlay shows the existing T.K. and Lenny sprites, with T.K. horizontal. T.K. rotates upright over **0.5 seconds**, then both sprites bounce **three times over two seconds**. Afterward the dialogue says **“T.K. is awake!”**. Press **Enter** to return to the title. Space does not dismiss the ending. Existing gameplay HUD canvases are hidden during the ending; the story overlay remains visible.

`StoryGameplayPause` suspends active input and AI, pauses game time and audio, and suspends player physics. It restores the state it owns on scene exit, without re-enabling dead characters. `TitleMenu` and the original boss crystal exit respect the story lock so they cannot interrupt the sequence.

## Inspector adjustments

Select **Story Sequence** in either copy. `StoryIntroduction` exposes the walk distance and duration. `StoryEnding` exposes wake duration, celebration duration, bounce count, and bounce height (UI pixels). `StoryDialogue` holds the canvas, text, and original sprite references. **T.K. Sleeping** is the intro's visual prefab instance; adjust its transform to reposition or resize him.

## Verification

Controlled Unity Play mode checks covered both dismissal keys, held-key protection, the two-unit walk, restored arrow movement, first and stronger wing-assisted second jump, one tail sweep per X press, and F fireballs. The existing health-death coroutine was allowed to reload the scene; the intro remained skipped and the player restored. Scene restart also skipped it.

Checks also cover the connected GameLoop amulet route, nonlethal versus lethal final-boss damage, original reward reveal, blocked crystal exits during the ending, wake timing, all three bounce peaks, ending HUD hiding, Enter-only return, and New Game resetting the intro flag. The scenes were visually inspected through Unity camera captures.

These are controlled tests rather than a complete manual playthrough of all six levels. A standalone executable build and a full human playthrough have not been verified.

After refreshing from GameLoop, actual crystal/player collisions verified the entire route from the introduction through Gloamthorne, GameLoop Ashpetal, Level04, Wraithglass, and BossStory. Updated boss-defeat, wake, three-bounce, reward, and HUD checks passed. The final keyboard return-to-title check remains unverified: the synthetic keyboard test did not register Enter, and the subsequent computer-use check was stopped by the user.
