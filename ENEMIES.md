# Blight enemies and platform navigation

Open Assets/Scenes/EnemyAI.unity and press Play. Controls are unchanged:
arrows move/aim, Z jumps and double jumps, X performs melee, holding X then
releasing performs a charged tail sweep, and F fires a projectile.

The player starts at x = -28 with 300 HP. The 64-unit arena and following camera
are preserved. Walk right through the pink Chomper lane, purple Spora lane,
and blue Collusus lane. Enemy spawns are -18, -3.5, and 28 respectively.

## Demonstrating navigation

- The three left steps rise from the ground to tops at -2.1, -0.8, and 0.5.
  Lead Chomper up them, move to a different platform, then return to the ground
  to demonstrate route changes and safe drops.
- The right steps have tops at -2.9 and -2.6. Lead Collusus onto these lower
  platforms to show its slower pursuit and shorter jumps.
- The solid cover wall at x = 11.5 blocks attacks and projectiles. Chomper can
  use the approach step at x = 9.7 to jump over it. Collusus cannot use every
  Chomper connection; its graph uses its own body and capabilities.
- The orange island above the center has a top at 2.4. Double jump from the
  left edge of the center step, then steer onto the island after rising above
  it. Enemies choose a reachable alternative and wait.

Platforms are solid BoxCollider2D surfaces. Actors physically jump and fall;
navigation never teleports an enemy onto a route. The finished Chomper, Spora,
and Collosus artwork and health bars are on reusable gameplay prefabs in
Assets/Prefabs/Enemies. Each design keeps its original aspect ratio and is
aligned to the existing collider's feet. The Visuals root handles facing;
EnemyVisualAnimator supplies the distinct animation poses. The AttackCue remains
a separate indicator.

## Enemy animations

All three gameplay prefabs include EnemyVisualAnimator. It builds a subdivided
UV mesh from the original transparent artwork at runtime. The source PNGs stay
unchanged, and the original SpriteRenderer remains visible in Edit Mode. The
animation only changes the artwork; colliders, navigation and attack ranges
retain their existing shapes and settings.

- Chomper sways and twitches its petals while idle, crawls its roots and trails
  its crown while moving, coils during windup, and lashes forward on a real
  strike. Hits bend it away from the impact. Death curls and wilts the body with
  falling pink petals over 1.65 seconds.
- Spora breathes and ripples its cap and smaller mushrooms while stationary.
  Windup compresses its stem and swells its cap. A successful projectile launch
  triggers a pop, rebound and purple spores. Hits squash it sideways. Death
  deflates and slumps the mushroom with a harmless cloud over 1.9 seconds.
- Collosus shifts its weight and blinks, swings its rigid limbs during movement,
  and emits dust on steps and landings. Windup raises its arms; a real slam
  drives them down with dust. Hits produce a short jolt and stone chips. Death
  separates the helmet, body, arms and feet into falling regions over 2.2 seconds.

The controller exposes facing and normalized windup progress. Successful attacks
are detected through AttacksPerformed, so a cancelled attack never produces a
successful strike animation. EnemyHealth exposes hit direction and defers removal
until the complete death animation ends. The original windup pulse and death
shrink remain as fallbacks for enemies without an enabled animation component.
All pose clocks, particles and delayed removal use scaled game time, including
while the title menu pauses the game. Health bars and attack indicators remain
independent of the animated artwork.

Materials are in Assets/Materials/EnemyAnimations; the unlit artwork/particle
shader is Assets/Shaders/EnemyAnimatedArtwork.shader. Per-prefab visible pixel
bounds and death duration are configurable on EnemyVisualAnimator. Material
references ensure the shader is included in builds.

The level variants in Assets/Prefabs/IntegratedEnemies inherit the same animated
artwork. Their former added CharacterArtwork copies are removed to avoid drawing
two bodies. UpgradeEnemyAnimationVariants.Apply upgrades those variants and
verifies all six gameplay/level prefabs; level placement and colliders stay intact.

Tools/VerifyEnemyAnimations.cs is an Editor eval statement block for EnemyAI in
Play Mode. It checks idle/movement poses, real windup and attack signals,
recovery, damage, paused geometry, death, cancelled strikes, particles and stone
joint continuity. Tools/VerifyEnemyAnimationLifetime.cs checks real-time death
removal, pause and particle simulation, and captures poses under Library. Stop
Play Mode after running either verification to restore the original scene.

## How the navigation works

PlatformNavigation2D on Enemy AI Test Arena collects static box colliders
under its surface root. It samples standing positions and adds walking links
where a full body can pass. Jump links are checked with gravity-based box casts
along the flight, including overhead clearance and landing width. Close risers
can use a short vertical lift before horizontal steering. Drop links start at
platform lips and require a safe landing within the enemy's drop limit.

Each EnemyNavigator2D builds a graph for its own collider size, movement
speed, jump velocity, air speed, gravity, and drop limit. Dijkstra chooses the
lowest travel cost, with a small penalty for jumps and drops. Unreachable
destinations produce a route to the closest reachable alternative.

Moving goals are considered at configured intervals and immediately when the
target changes supporting platforms. Unchanged goals retain their routes,
including when waiting at an unreachable destination. Consecutive walking
links are combined so intermediate waypoints do not add a stopped physics frame.
Ground contact requires the feet to be within 0.06 units of the floor.

Ground geometry is checked every 0.5 seconds; changes invalidate graphs. An
enemy making no progress for one second pauses and replans. In-flight traversals
finish before replanning so target movement does not produce midair reversals.
Knockback interrupts navigation, and death clears the route immediately.

Moving enemies detect nearby living players within their radius, including
behind cover, so they can seek routes around it. They remember the last known
feet position for two seconds after the player leaves that radius. All attacks
still require line of sight and valid range. Spora uses sight for detection and
stays stationary.

Select an enemy with Gizmos enabled to see its route and destination. Enable
Show Connections on PlatformNavigation2D to see the last built capability graph:
green walking links, cyan jumps, and yellow drops. EnemyNavigator2D exposes
jump speed, horizontal jump speed multiplier, drop distance, route refresh,
stuck timeout, and Show Route. BlightEnemy exposes detection, movement, damage,
attack ranges, windup, direction-lock fraction, recovery, and cooldown.

## Combat tuning

| Enemy | HP | Ground chase speed | Jump velocity / maximum horizontal air speed | Maximum drop | Route refresh | Damage | Windup | Recovery | Cooldown after attack |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Chomper | 60 | 4.5 | 12 / 4.5 | 4 | 0.35 s | 12 | 0.40 s | 0.35 s | 0.95 s |
| Spora | 45 | Stationary | No traversal | 0 | — | 10 | 0.60 s | 0.40 s | 1.40 s |
| Collusus | 120 | 1.5 | 11 / 3 | 2 | 0.50 s | 25 | 0.75 s | 0.60 s | 1.60 s |

Units are Unity units and seconds. All detection radii are 10. Melee ranges are
1.5 for Chomper and 2.2 for Collusus; Spora's firing range is 9. Spore speed is
6 and maximum travel distance is 14. Patrol speed is 55% of chase speed.
Collusus retains the requested 1.5 ground speed; its air multiplier permits a
short hop across a step wide enough for its rock body. Ground acceleration is
45 units/s² for Chomper and 10 for Collusus, reaching full chase speed in about
0.10 and 0.15 seconds respectively. Stops remain immediate for safety and attacks;
airborne connections retain their validated velocity.

Melee enemies stop at 90% of attack range (1.35 for Chomper, 1.98 for Collusus),
then resume closing only beyond attack range plus 0.2. This spacing buffer
prevents repeated movement toggles when a player hovers near attack range.
Patrols stay within three units for Chomper and two for Collusus. The patrol
center starts at spawn and becomes the current position after pursuit ends.
These values are editable in BlightEnemy's Movement and Combat Feel section.

Attacks pulse the body and display a yellow cue. Direction tracks the player
during the first 65% of windup, then locks and turns the cue orange. Locking
occurs before sampling a new aim so a delayed frame cannot retarget the final
part of the attack. Melee cannot hit behind the locked
facing. Spora fires along the locked aim and cancels if the player has moved far
off that aim. Range, vertical separation, and sight are rechecked when attacking.
Leaving range cancels into recovery. A successful attack briefly flashes the
cue white; the cue disappears during the remaining recovery window. Recovery
stops pursuit briefly to provide a counterattack window; cooldown prevents
immediately restarting.

The existing 0.22-second hit reaction now also cancels a pending windup and
enters recovery. Navigation pauses while retaining the knockback impulse.
EnemyNavigator2D routes and attack cues are cleared when AI is disabled.

The player has 0.65 seconds of configurable invulnerability after taking damage.
The body flashes red, then flickers while protected. Extra damage and knockback
are ignored during that interval, preventing overlapping enemies from draining
health in a burst. Charge visuals and movement input remain separate.

Basic melee and player projectiles deal 15 damage; charged tail sweeps deal 30.
Melee hits a health receiver once even with multiple colliders. Practice targets
retain their flash and charged knockback. Collusus resists 80% of knockback;
Spora resists all knockback. Enemy death stops AI/physics, disables colliders,
hides the HP bar, and fades out before removal. Player death empties the bar
and disables movement/combat/physics. Restart Play mode to reset health.

## Verification

Compilation passed. These checks passed in the running Editor's Play mode:

| Check | Coverage |
| --- | --- |
| Tools/VerifyAIRefinements.cs | Zero pauses on a 15-unit walk; unchanged routes cached; hovering actors not grounded; acceleration; melee spacing and buffer; early aim tracking and late-frame locking; cue colors/recovery; hit interruption with preserved knockback; bounded patrols and clear scene spawns. |
| Tools/VerifyNavigation.cs | Chomper climbs three steps and drops; Collusus climbs/descends; cover route; unreachable fallback without repeated jumping; wall addition/removal; immobilized enemy recovery; throttled routes; death cancels navigation. |
| Tools/VerifyFairCombat.cs | Both real AI controllers chase onto a platform and enter windup; visible cues; direction locking and dodging behind; recovery; Spora blocked by cover; overlapping damage protection and resumed damage after expiry. |
| Tools/VerifyBlights.cs | All three enemies: basic/charged/projectile damage, duplicate collider protection, HP, hit flash, knockback, death, outgoing damage, attack timing, cancellation and sight. Player movement/double jump and practice targets. |
| Tools/VerifyPlayerHealth.cs | Full 300 HP/bar, feedback, preserved charge visuals, knockback, damage protection, death disabling, rejection of dead targets. |
| Tools/VerifyDemoArena.cs | Landing on every demo surface; player double jump onto the enemy-unreachable island; camera tracking/bounds, 300 HP, Collusus speed, solid cover. |

Use a fresh Play session for each check and stop afterward. For example:

    unity command editor_play --caller plugin --skill unity-cli
    unity command eval_file --file Tools/VerifyNavigation.cs --caller plugin --skill unity-cli -- --timeout 40000
    unity command editor_stop --caller plugin --skill unity-cli

These are automated checks using real physics stepping, temporary actors, and
calls into the gameplay controllers. Attack timing boundaries are advanced with
reflection; this is not a manual difficulty playtest. Subjective difficulty
still needs player feedback.

## Current limits

Navigation supports static axis-aligned BoxCollider2D terrain with downward
gravity. Slopes, TilemapCollider2D/CompositeCollider2D terrain, moving platforms,
one-way platforms, ladders, and double jumps are not implemented. Changing terrain
can require a new capability graph; this prototype has been checked on the demo
size, not profiled on a large production level. Jump paths use the project's
fixed physics timestep. Change capabilities in Edit mode before testing.

Enemy artwork now uses procedural animations. Audio cues, automatic respawn,
and crowd-avoidance routing are not implemented. The artwork-only preview
prefabs remain static; use the gameplay or integrated prefabs for animation.
