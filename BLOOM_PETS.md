# Bloom preview pets

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

Platform navigation supports static, axis-aligned BoxCollider2D terrain, including reachable jumps and safe drops. Moving platforms, slopes, TilemapCollider2D navigation, climbing and recruitment persistence are not supported. Solid undersides can block a reactive hop; pets then land and find a validated route. A reactive hop remains over its original platform while a subsequent route handles crossing to another surface. There are no pet attacks, abilities, health systems or menus. Gameplay feel has not been assessed by a human playtester.

The Bloom feature changes are limited to BloomPreview, the new pet prefabs/scripts, this document and its verification tool.
