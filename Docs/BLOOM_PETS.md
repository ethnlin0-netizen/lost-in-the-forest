# Recruitable Bloom companions

## Fixed gameplay placements

The three regular levels were randomly selected once during development. These saved prefab instances do not reroll on new games or deaths:

- **Petalborn:** `Assets/Scenes/GameLoop/PrismbloomLevel01Story.unity`, feet at `(2.882, -1.460)`. This is the playable Level 1 scene used by the title-screen story route; main removed the older standalone Level 1 scene.
- **Whisperseed:** `Assets/Scenes/GameLoop/GloamthorneLevel02.unity`, feet at `(1.691, 11.040)`.
- **Dryad:** `Assets/Scenes/GameLoop/Ashpetal Level 03.unity`, feet at `(0.017, -1.960)`.

Each companion stands on the entrance platform, a short clear walk away from Lenny, outside lethal hazards and enemy contact. Touch one to recruit it automatically. There are no new abilities, power-ups, or dialogue.

`BloomCompanionSession` remembers each recruited identity and recruitment order in memory. On subsequent GameLoop scene loads, it recreates followers from the existing pet prefabs and attaches them to the new Lenny, with distinct arrival positions and follow slots. A collected companion's original scene instance is reused when revisiting that level, so it does not create an extra pickup. Physical followers remain owned by their current scene; no character object survives with stale terrain references.

`Assets/Prefabs/BloomPets/BloomCompanions.asset` is the reusable catalog containing the source prefabs. Each pet's **Run recruitment** Inspector section contains its unique identity and catalog reference. Tune speed, distance, and spacing on the pet prefabs. Move the saved scene instances in Unity to change pickup locations. `Tools/PlaceBloomCompanions.cs` is the one-time authoring tool and deliberately refuses to reroll existing placements.

Recruited companions stay recruited after health death or lethal acid/thorn contact. The existing level restart recreates all collected companions at safe, separate positions beside the new Lenny. A collected companion's authored pickup is reused as its follower when its own level restarts, preventing duplicate pickups or followers. Uncollected companions remain at their fixed scene placements. New Game, return to title, and quit clear recruitment for a fresh run. Nothing is written to a saved game or PlayerPrefs.

The story introduction pauses and restores the existing follower components. Companions also carry into Boss and BossStory; their existing ending pauses them with the rest of gameplay. The current story route uses Boss and its victory crystal ending. Source art, player prefabs, navigation algorithms, enemy placement, and scene progression are unchanged by the companion feature.

## Preview

Open `Assets/Scenes/BloomPreview.unity` and enter Play mode. Walk right with the arrow keys to touch Whisperseed, Dryad and Petalborn; each joins a trailing group once. The existing player prefab retains its movement, double jump, attacks and health. This scene originally contained only a camera, so the preview adds that prefab, a ground platform, a raised platform and a cover wall.

## Assets and behavior

- `Assets/Prefabs/BloomPets/WhisperseedPet.prefab`
- `Assets/Prefabs/BloomPets/DryadPet.prefab`
- `Assets/Prefabs/BloomPets/PetalbornPet.prefab`

Each pet prefab nests the corresponding existing FriendlyCharacters artwork prefab, scaled to 1.35 units tall. The original artwork and player prefabs are unchanged. There are no walking animation clips on the artwork prefabs; sprites flip with their walking direction. Recruitment briefly tints and pulses the visual without changing the physics hull.

`BloomPetFollower` handles touch recruitment, walking, facing, jump following and terrain safety. `BloomPetGroup` is added to the recruiting player at runtime, assigns distinct slots, prevents repeated registration and observes existing jump impulses after the player's movement callback. It does not modify the player prefab or persist between scenes.

Pets use trigger-only kinematic Rigidbody2D hulls. Their motion and gravity use Rigidbody2D.MovePosition, diagonal hull casts with sliding against solid terrain and ground probes. Ceilings stop upward motion, walls stop horizontal motion and landings stop falling. This keeps them from pushing actors or appearing in the player's existing non-trigger ground and combat queries. Only a collider attached to a live, enabled Movement Rigidbody2D can recruit a pet; enemy and player-projectile contact is rejected.

Pets mirror the normal and wing-assisted jump with a small delay per follow slot. Reactive hops stay above their takeoff platform; crossing a lip requires a validated navigation route. The pet prefabs reuse the existing EnemyNavigator2D controller (which has no combat dependency), with the existing PlatformNavigation2D graph on Bloom Preview Platforms. Both shared navigation scripts remain unchanged. The graph validates walks, jumps and safe drops against each pet's dimensions and gravity. Narrow landing platforms compress the trailing spacing. Unreachable destinations use the navigator's reachable fallback; pets never teleport.

## Initial Inspector tuning

| Setting | Default |
| --- | ---: |
| Movement speed | 6 units/s |
| Catch-up speed / distance | 8 units/s / 4 units |
| Acceleration | 30 units/s² |
| First pet distance | 1.5 units |
| Follow spacing | 1.25 units |
| Stop / resume distance | 0.12 / 0.35 units |
| Edge look-ahead | 0.12 units beyond the hull and next step |
| Edge ground-probe depth | 0.18 units |
| Ground-contact probe | At most 0.06 units |
| Mirror player jumps | Enabled |
| Jump delay per pet | 0.08 seconds per slot, starting at 0.08 |
| Gravity multiplier | 3, matching the player |
| Maximum fall speed | 30 units/s |
| Navigation jump speed | 18 units/s |
| Maximum validated drop | 8 units |
| Route refresh interval | 0.3 seconds |
| Hull skin | 0.025 units |
| Recruitment feedback duration | 0.35 seconds |

Use the pet prefab Inspector to change the settings. Jump speed, maximum drop and route refresh are on EnemyNavigator2D; follow and reactive jump settings are on BloomPetFollower. Keep the fall-speed cap high enough for the configured jump/drop trajectory (30 supports the initial 18/8 settings). When the player turns, each destination moves to the new trailing side. Pets stop inside the stopping distance and resume only outside the larger resume distance. Catch-up applies to direct walking and reactive hops; planned jumps retain their validated horizontal speed.

## Verification

Verified on October 10, 2026 in the running Unity 6000.6.1f1 Editor, in BloomPreview Play mode:

- All three pets stay idle before contact and reject a real Chomper and a player-owned projectile.
- Walking the player through all three recruits them with unique slots, even with an additional player collider. All three physically change position and remain separate scene objects.
- All three settle at their follow destinations, remain grounded, stay still for 200 physics steps and ignore a small player-position change.
- Direction changes switch the trailing side and flip walking sprites.
- A representative pet stops at a temporarily taller, unjumpable solid wall and the left floor edge.
- All three navigate from the floor to the raised platform using validated jumps, settle in compressed follow positions and use validated drops to return to the floor.
- All three mirror the player's normal and wing jumps with staggered takeoffs, then fall and land safely. A separate pet hull is stopped by the raised platform's underside and lands safely.
- Existing Input System arrow movement, first jump and double jump retain 6/10/14 speeds. Pets do not replenish airborne jumps.
- The existing basic-melee method, X full-strength tail sweep with knockback and F projectiles hit a PracticeTarget. A projectile passes through a pet to reach its target. The latest main branch removed hold-to-charge; the verification tool follows those controls without changing PlayerCombat.
- A separate physical-overlap check verified the recruitment tint.
- A second check used normal Unity Update/FixedUpdate callbacks: the player's existing jump request was set, pets observed it and landed; the player was then placed on the raised platform and all three jumped to it. Moving the player beyond its edge let real player gravity cause a fall, and all three followed with safe drops. No pet callbacks were manually ticked. Background execution was temporarily enabled because the unfocused Editor was not advancing gameplay frames, then restored; no project settings were saved. Test placement and the jump request were driven by the Editor, so this is not human keyboard playtesting.
- The camera render was visually inspected for the three recognizable sprites, framing and a separated following group.

The repeatable integration suite is `Tools/VerifyBloomPets.cs`. Start fresh Play mode in BloomPreview, then run it through Unity CLI `eval_file`. It uses real Physics2D.Simulate and Input System keyboard events with manually stepped component callbacks, and stops short of claiming human keyboard playtesting. Stop Play mode afterward to restore spawn positions and runtime state.

## Limitations

Platform navigation supports static, axis-aligned BoxCollider2D terrain, including reachable jumps and safe drops. Moving platforms, slopes, TilemapCollider2D navigation and climbing are not supported. Solid undersides can block a reactive hop; pets then land and find a validated route. A reactive hop remains over its original platform while a subsequent route handles crossing to another surface. There are no pet attacks, abilities, health systems or menus. Gameplay feel has not been assessed by a human playtester.

## Gameplay verification — October 11, 2026

`Tools/VerifyBloomCompanionRun.cs` passed 17 asynchronous Play mode scene cases. It uses actual physics contacts for recruitment, actual scene loads for progression, the existing health damage/death coroutine, and the lethal hazard trigger callback. It verified all three recruits, persistent prefab references, unique identities and slots, arrival separation, revisiting collected pickup scenes, repeated scene callbacks, companions respawning beside Lenny after both health and acid deaths, retaining recruitment in earlier levels without duplicate pickups, the Level 1 story pause/walk/resume, and clearing recruitment on return to title.

`Tools/VerifyBloomPets.cs` passed all 12 physical following and player-control checks against the latest navigation code: enemy/projectile rejection, touch recruitment, separated follow destinations, settling without jitter, facing changes, walls/edges/ceilings, raised-platform navigation, returning to lower terrain, both jumps, tail sweep knockback, and projectiles passing through companions. Its synthetic keyboard updates explicitly use the game's input update while the editor is unfocused; temporary input settings are restored afterward. The lower-platform check validates physical landing at the follow destination rather than requiring a particular route-link type.

These controlled Editor checks do not replace a complete human playthrough or standalone executable build; neither has been completed for this change. Navigation retains the terrain-type limitations above, and unreachable routes do not teleport companions through obstacles.
