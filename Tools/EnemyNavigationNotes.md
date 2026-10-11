# Ledge pursuit fix

Chomper and Collusus now walk fully clear of a validated drop's takeoff edge,
then use the predicted fall velocity. The planner tries a straight fall first,
then half-speed and full-speed sideways falls when a gap requires them. Drop
landings require the collider's center to be safely supported; the body may
overhang. Jump landings still require the full collider's width on the surface.

Walking checks use the actual next physics step and the graph's edge inset so
enemies can reach takeoff points without stopping at a longer lookahead probe.
Drops are swept against obstacles and revalidated at takeoff. Ordinary patrols
still stop at edges; crossing an edge requires a validated traversal link.

## Tuning

- Chomper / Collusus maximum drop: 6.5 units, including the demonstration ledges.
- Collusus chase speed remains 1.5 units/second.
- Jump capabilities, detection ranges, attack timing and player controls unchanged.
- Spora remains stationary and uses line of sight for projectiles.

The existing navigation supports static, axis-aligned BoxCollider2D platforms,
including the one-way platforms and automatic scene maps brought in from main.
Targets beyond jump/drop capabilities remain unreachable; enemies use the
existing reachable fallback rather than attempting unsafe movement.

## Verification

Tested in the running Unity Editor's EnemyAI scene:

- Six normal-frame Play-mode cases: each moving enemy descended from the upper
  center ledge onto the player platform below, descended from the center-left
  ledge to the ground, and jumped to a neighboring reachable ledge. Each arrived
  and performed its attack. Results: Temp/LedgeNativeResults.txt (temporary).
- Tools/VerifyLedgePursuit.cs: controlled Play-mode Rigidbody2D simulation of the
  same six cases. Checks landing, approach spacing, no arrival jitter, no material
  collider penetration, no stuck recovery loops, and bounded route calculation.
- Tools/VerifyNavigation.cs: climbing, descending, unreachable fallback, cover
  routes, changed obstacles, stuck recovery, throttling and navigation death cleanup.
- Tools/VerifyAIRefinements.cs and Tools/VerifyFairCombat.cs: walking continuity,
  pursuit spacing, telegraphs, direction locks, recovery, hit reaction/knockback,
  patrol bounds, Spora line of sight and player invulnerability.
- Tools/VerifyBlights.cs, in a fresh Play session: player movement/double jump,
  all three player attacks, once-per-target damage, health bars, enemy hit feedback,
  knockback, death, enemy attacks and practice targets. An initial run after the
  native combat fixture failed its movement assertion; restarting Play mode
  removed that fixture's residual player combat state and the entire check passed.

No terrain or player assets were changed. EnemyAI inherits the revised drop limit
from the Chomper and Collusus prefabs, saved through Unity.

## Verification after merging main

Merged origin/main at a601bc6 into enemy-ai by fast-forward, then restored the
uncommitted ledge fix without conflicts. Repeated the six pursuit simulations,
navigation, AI refinements and fair-combat checks successfully. Also passed
Tools/VerifyEnemyAutoNavigation.cs (automatic per-scene maps, one-way platform
jumps, obstacle checks and geometry refresh) and Tools/VerifyEnemyAnimations.cs
(idle/walk/windup/attack/recovery/hit/death and cancelled attacks).

The older VerifyBlights combat fixture failed a ranged-hit assertion using its
original positions near the low center platform. Moved its temporary combat
fixtures onto clear ground and placed ranged targets beyond the current mouth
launch point. Its full movement, damage, health bar and death checks then passed.
No player or projectile gameplay code was changed for that test adjustment.

All six normal-frame pursuit-and-attack cases also passed after the merge;
results are in Temp/LedgeAfterMainResults.txt. Unity was left in Edit mode.


## Wall pursuit refinement

The demo's cover wall could be crossed only from the left by Chomper. Its top
was omitted from the graph because it is narrower than the enemy collider;
Collusus also had no reachable approach on the right. EnemyAI now includes a
matching Navigation Step - Cover Return at x=13.3, reusing the left step's visual
and collider configuration. Collusus keeps its 1.5 chase speed and 11 jump speed.

Narrow tops at least half the actor width plus a safety margin now get edge and
center navigation samples. Jump landings require the center safely inside the
top, with at least half the collider supported; wider surfaces keep the previous
full-width margin. Walking can center an actor on such a top. All flight and
walking clearance checks still apply, and very thin/too-tall walls remain blocked.

Tight jumps now approach their takeoff within 0.01 units instead of 0.09. The old
tolerance could repeatedly reject the same validated arc from a slightly wrong
position without advancing. An additional swept lift-first jump candidate uses
0.02 seconds of clearance delay before the existing 0.04-second candidate.

A grounded walk-off may yield to melee spacing or attack windup when the player
is already reachable, rather than waiting for traversal timeout against their
collider. Airborne jumps and falls remain committed until landing.

Tools/VerifyWallPursuit.cs passed eight controlled Play-mode pursuit cases:
both directions across the authored cover wall and a temporary 1.6-unit narrow
wall, for both Chomper and Collusus. Checks include physical clearance, attack
spacing, no stuck loops, and no arrival jitter. A temporary 8-unit wall remained
unreachable without repeated impossible jumps.

After the final changes, ledge pursuit, navigation/recovery, automatic and one-way
navigation, AI refinements, fair combat, enemy animation, and full player/enemy
damage and movement checks all passed again.

All four normal-frame cover-wall pursuit cases also passed: Chomper and Collusus
crossed in both directions and performed their attacks after arriving. Results:
Temp/WallNativeResults.txt. Unity was left in Edit mode with EnemyAI saved.

## GameLoop upward pursuit refinement (2026-10-10)

Reproduced in GameLoop/GloamthorneLevel02: pursuit from CombatLedge (top 6.5)
to FinalBranch (top 9.0) was unreachable at the previous jump settings. Chomper
stopped at the lip with no jumps. Rigidbody2D gravity scale 3 gave the previous
12/11 launch speeds approximate discrete apex rises of 2.33/1.95 units, below
the required 2.5-unit rise.

Updated shared enemy prefabs through the running Editor:

| Setting | Chomper | Collusus |
| --- | --- | --- |
| Maximum upward launch speed | 16 | 15 |
| Air speed multiplier | 1 | 3 |
| Ground chase speed | 4.5 | 1.5 |
| Maximum air speed | 4.5 | 4.5 |
| Approximate maximum rise at gravity scale 3 | 4.19 | 3.67 |
| Maximum drop distance (earlier ledge fix) | 6.5 | 6.5 |

Navigation tries a validated 75%-strength hop first, then the full configured
launch speed if needed. Each jump link records its upward velocity; takeoff
revalidation uses the actor's real feet and applies the newly validated velocity.
Flight casts now use nearly the full physical body, leaving a 0.002-unit inset
per side instead of the more permissive walking skin. This keeps corner
clearance closer to the actual collider. The original TryJump overload retains
its fixed-strength behavior; the navigator uses the new variable-strength overload.

Enemy/player solid body pairs no longer block each other. This prevents contact
with the player from interrupting a validated landing or treating the enemy as
a player platform. Ignored pairs are restored when an enemy is enabled again.
Trigger interactions, terrain collision, projectile collision, and explicit melee
and slam damage checks remain active. All damage and attack timing values are
unchanged. IntegratedEnemies prefab variants inherit the shared jump tuning;
per-scene overrides still take priority (including Wraithglass Collusus's jump 22
and maximum drop 4). No GameLoop scene layout was edited for this refinement.

Verified in the running Unity Editor:

- Tools/VerifyGameLoopPursuit.cs: eight controlled Play-mode tests on the real
  GloamthorneLevel02 terrain using IntegratedEnemies variants. Both enemies
  climb LowerSafeIsland -> DecayingBranch, DecayingBranch -> ThornCrossing,
  CombatLedge -> FinalBranch, and FinalBranch -> ItemSanctuary. All land on the
  requested platform, approach, stop without jitter, avoid material penetration,
  and finish without stuck recovery loops. Collusus ground speed stays 1.5.
- EnemyAI regression checks passed: wall pursuit in both directions, descending
  ledges, navigation/blocked route recovery, unreachable fallback, automatic maps,
  one-way platforms, AI movement/combat feel, fair combat, and enemy animations.
- VerifyBlights passed in a fresh Play session: player movement and double jump,
  basic/charged/projectile damage for each enemy, duplicate collider protection,
  health bars, knockback, hit feedback, deaths, player damage, and practice targets.
  The initial run after other combat fixtures failed its first-jump assertion;
  a fresh Play session removed the leftover test state and the full suite passed.
- The descent fixture now accepts either a safe drop or a validated downward jump,
  since greater jump capability can change the planner's cheapest route. The
  too-tall wall fixture is now 12 units high: the former 8-unit wall can be reached
  through an intermediate platform with the new jump capability.

Testing of every encounter in every other GameLoop biome remains outstanding.
The navigator still supports static axis-aligned BoxCollider2D terrain. Platforms
outside configured jump/drop reach or without body clearance remain unreachable;
enemies wait at a reachable fallback rather than teleporting or crossing walls.

Final normal-frame Play-mode verification also passed all eight Level 2 cases:
both enemies jumped, landed on the player's platform, and executed their attack.
Temporary results: Temp/GameLoopJumpNativeResults.txt. Player blocking remained
disabled after enemy re-enable. Unity was left in Edit mode on GloamthorneLevel02.

