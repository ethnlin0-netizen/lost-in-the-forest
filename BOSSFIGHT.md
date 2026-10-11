# Blight Amalgam boss

Open `Assets/Scenes/Boss.unity` on `bossfight` and enter Play mode. Arrow keys move/aim, Z jumps and double jumps, X performs the current full-strength tail sweep, and F fires a fireball. The scene uses `Assets/Prefabs/Bosses/BossPlayer.prefab`, a variant with 100 HP; the shared Player prefab remains at 300 HP for other scenes. The boss prefab has 1,000 HP.

## Visuals and arena

The reusable prefab is `Assets/Prefabs/Bosses/BlightAmalgamBoss.prefab`. Its temporary artwork assembles existing Collosus, Chomper and Spora sprites with mineral veins and an exposed chest fragment; original art/import settings are untouched. Measured regular artwork heights are Chomper 1.3355, Spora 1.4549 and Collusus 1.8549 units. The boss is approximately 3.7098 units tall, twice Collusus. The 2.75 x 3.2 collider is smaller than the assembled silhouette.

Boss originally contained only Main Camera. The scene now has the BossPlayer prefab variant, a 38-unit floor, two low dodge platforms, solid arena boundaries, the existing Dreadcrown forest background, and an overlay boss health bar. Player spawn is at -15 and boss spawn at 7, outside initial detection. Boss pursuit stays in the central arena (-8.5 to 8.5); side platforms provide dodge positions without trapping the boss behind them. It does not climb platforms; spores reach elevated players when line of sight is clear.

`Boss Arena/RaisedPlatformRoutes` adds nine moss-topped stone shelves. Mirrored routes climb from the existing dodge platforms through tops at -0.1, 1.3, 2.7 and 4.1 units, then meet on a central shelf at 5.5. Each new shelf uses a BoxCollider2D with a one-way PlatformEffector2D: jump through from below and land from above. Tier rises are 1.4 units; shelves are 3.4–3.8 units wide. The player's existing normal jump reaches every leg, while double jump provides extra margin. The central floor, safe spawn, camera, player prefab and boss prefab remain unchanged by this layout edit. Upper shelves provide escape positions; the boss still fights on the floor and elevated combat depends on spore line of sight.

Regular Blight prefabs in `Assets/Prefabs/Enemies` and their `IntegratedEnemies` variants now share an automatic runtime navigation map if their scene has no authored `PlatformNavigation2D`. The map scans active static BoxCollider2D terrain in that scene, excluding actors and triggers, and refreshes when geometry changes. Existing authored maps retain their original hierarchy scope. This requires no added scene object or prefab override; sprite-only `EnemyCharacters` prefabs still have no AI. Route validation respects axis-aligned one-way shelves: actors may pass upward through them and land from above, while ordinary solid walls remain obstacles.

## Combat

The boss selects one attack at a time, faces its target, tracks early windup and locks its aim at 60% of windup. Melee checks horizontal reach, vertical separation, the committed side and line of sight at impact. Leaving attack range cancels the windup into recovery without opening the chest. Lunges use physics hull casts, arena limits and leading-foot support checks.

Slam raises the rock arm, marks the threatened ground, hits in front, and launches a low shockwave. Flytrap lunge opens the chest mouth and visibly pulls the artwork back before a committed movement. Spore volley swells and brightens mushroom caps, then fires three slow visible shots with 22-degree angular gaps. Hazards sweep their hulls against terrain/player colliders and expire; shockwaves stop at unsupported ground and rises.

After completed slams and lunges, the fragment turns yellow and enlarges for a counterattack opening. The boss always accepts damage. BlightBossHealth reuses EnemyHealth and multiplies incoming damage during this opening. The shared EnemyHealth change is solely making TakeDamage virtual; ordinary enemy behavior is unchanged. Existing player attack target deduplication and knockback resistance are retained.

At 50% health, a single non-damaging transition cancels attacks/hazards, brightens veins and adds mushroom pulsing. Pursuit becomes slightly faster. Every second phase-two slam can queue a spore volley after its recovery, an additional delay and a fresh full spore windup. Range and sight are rechecked; there is no forced hit or overlapping boss attack state.

At zero HP, the existing EnemyHealth system disables collisions and physics, fades/shrinks the artwork, and removes the boss. Boss behavior cancels immediately, owned hazards disappear, and EnemyHealthBar hides. The existing BossCrystalReward reveals a fragment at the arena center. Touching it with a living player now returns to `TitleScreen` through `BossCrystalExit`. Its trigger ignores non-player objects and cannot activate before the reward is revealed. `TitleMenu.ReturnToMainMenu` resets the persistent menu's run/pause state, shows the main buttons, and allows Play to start a fresh run. Opening Boss directly also works: the crystal loads TitleScreen when no persistent menu exists. No saved progression is added.

## Initial tuning

| Inspector setting | Initial value |
| --- | ---: |
| Maximum HP / knockback resistance / death feedback | 1,000 / 0.85 / 0.8 s |
| BossPlayer variant maximum HP | 100 |
| Pursuit speed phase 1 / phase 2 | 1.2 / 1.6 units/s |
| Detection / melee vertical tolerance | 20 / 1.35 units |
| Slam reach / damage | 2.2 units / 30 |
| Slam windup / recovery / cooldown | 0.9 / 1.2 / 2.6 s |
| Shockwave speed / lifetime / damage | 4 units/s / 2.6 s / 15 |
| Lunge selection range / distance / speed / damage | 5.5 / 2.6 units / 6 units/s / 24 |
| Lunge windup / recovery / cooldown | 0.75 / 1.05 / 2.4 s |
| Spore range / speed / lifetime / damage | 14 units / 4.5 units/s / 3.2 s / 15 |
| Spore windup / recovery / cooldown / angular gaps | 1 / 0.8 / 3.4 s / 22 degrees |
| Direction lock fraction | 0.6 |
| Chest exposure duration / multiplier | 0.85 s / 1.5 |
| Phase threshold / transition / combo delay | 0.5 / 1.3 / 0.65 s |

Cooldowns count from attack completion and overlap recovery; phase-two slam combos instead wait recovery + combo delay, followed by the full volley windup. Actual player damage remains 15 for the legacy basic-melee method, 30 for X tail sweep, and 15 for fireballs. Existing player hit invulnerability is preserved. Phase two now begins at 500 HP. A 60–90 second fight is a tuning goal, not a measured result; fight duration with the updated health values needs human playtesting.

## Verification

Verified in Unity 6000.6.1f1 Boss Play mode on October 10, 2026:

- Measured double-Collusus artwork height, inset collider, safe spawn and 1,000/100 boss/player HP. Re-ran the automated combat suite after increasing all four attack damage settings by 50%.
- After reducing BossPlayer to 100 HP, separately checked Play-mode initialization at 100/100 and a 20-damage hit reducing health to 80 with the bar immediately at 80%.
- Real basic-melee method, X tail sweep and F fireball damage; duplicate boss colliders did not duplicate damage. Exposed chest increased a 30-damage hit to 45. Health bar updated immediately and configured resistance retained a knockback impulse.
- Slam produced no early damage, locked its direction, hit for 30 and opened recovery. Crossing behind after lock avoided damage. Existing player invulnerability rejected a second overlapping hit.
- A physical shockwave hit a grounded player for 15 and was avoided using the existing Z jump. Lunge (24) and spore (15) damage settings were checked on the saved prefab and inherited scene instance; individual hit amounts for those two attacks were not separately retested.
- A committed lunge stopped before a tall solid wall and recovered.
- Cover blocked spore attack selection; the three-shot fan had the configured 22-degree gaps. Fired projectiles stopped at a solid wall and expired by lifetime.
- Phase two began once at the health threshold. The second phase-two slam preserved its recovery, combo delay and a fresh full spore windup. Automatic attack selection exercised melee and spores.
- Death immediately canceled boss behavior, hazards, collision and health bar, and triggered BossCrystalReward.
- Existing run/normal jump/double-jump speeds remained 6/10/14; player health bar remained present.
- A separate normal Update/FixedUpdate run, with no manually ticked boss callbacks, performed attacks and damaged the player, completed the phase transition, and removed the boss and hazards after the death delay. Runtime background execution was temporarily enabled for this test and restored without saving project settings.
- The game view and camera render were visually inspected for the combined sprites, arena and readable boss health bar. Final sprite artwork and human combat readability still need review.
- After adding the raised platforms, tested both complete ascent routes using Input System arrow/Z events, the existing Movement callbacks and actual Physics2D simulation. Each route reached the central shelf with normal jumps and valid landings; placement was reset only at each route's starting spawn, with no teleport during traversal. The boss was temporarily disabled during the traversal check. Re-ran the existing automated combat suite successfully in the new arena and inspected its camera render. Human platforming/combat balance has not been playtested.
- Tested crystal return in Play mode from Boss opened directly and with the persistent menu carried from a started run. Before-victory and non-player callbacks were ignored; a real Physics2D player overlap after boss death loaded TitleScreen, showed one menu, and reset run state. Invoking the actual Play button event after the persistent-menu return started Biome1 and hid the menu. These checks use test placement and instant boss damage to isolate the transition; they are not a full human fight-through.
- Tested regular enemy instances added to Boss without an authored map: a single runtime map found 14 terrain colliders, moving enemies entered pursuit, and Chomper and Collusus attacked. `Tools/VerifyEnemyAutoNavigation.cs` additionally checks shared map creation, scene isolation, actual walking/jumping, upward passage and landing on a one-way shelf, solid-wall clearance, geometry refresh and reuse of an authored map in a temporary Play-mode scene. These checks passed without saving changes to other scenes or prefab assets.

The automated checks use test placement, reflection for isolated attack/phase cases, and real Unity physics. Spore angular spacing is verified; human dodge difficulty and fight duration are not. `Tools/VerifyBlightBoss.cs` runs from a fresh Boss Play-mode session through Unity CLI eval_file, using actual Physics2D.Simulate, Input System keyboard events and manually stepped callbacks. It does not constitute human playtesting.

## Limits

Temporary combined sprites need final boss artwork/animation from the art owner. No new audio, extra enemies, progression persistence, project settings or other scene edits are included. The encounter uses a flat central fighting surface and static box terrain; the boss does not traverse platforms. Human difficulty, visual readability and the intended fight duration need playtesting.
