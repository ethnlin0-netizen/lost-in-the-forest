# Lenny animations

Open `Assets/Scenes/Movement.unity` and press Play. The scene's existing linked
`Assets/Prefabs/Player.prefab` now displays Lenny's layered artwork. Drag the same
prefab into another scene to reuse movement, combat, health, and animations.

## Controls and timing

- Left/Right Arrow moves and mirrors Lenny's body, wings, and tail together.
- Up/Down Arrow aims the ranged attack vertically; otherwise it follows facing.
- Z performs the original ground jump (speed 10). A second press in the air
  performs the stronger airborne jump (speed 14) and flaps Lenny's wing joints.
  **Movement → Double Jump Speed** remains independently adjustable.
- **X: tail sweep.** Each press immediately triggers one full-strength tail attack.
  Holding or releasing X does not trigger additional attacks; there is no charge
  requirement, timer, or charge bar.
  His tail immediately reaches the existing attack area, then sweeps back to its
  resting pose. Damage (30), reach (1.8), knockback (8), and animation duration
  (0.2 seconds) are preserved and applied once per press.
- F launches a four-frame fireball from Lenny's mouth. Its core is aligned with the existing circle
  collider, and its artwork rotates to match the actual travel direction.

## How the animations are connected

`Movement.DoubleJumped` fires only when an airborne jump is accepted.
`LennyAnimation` uses that event to rotate the two wing joints; it never applies
movement. The first jump and blocked third jumps produce no flap.

`PlayerCombat.MeleePerformed` passes the full-strength flag, captured facing, swing
duration, and existing hit reach to `LennyAnimation` before the original hit
query. Pressing X places the tail in its contact pose immediately, keeps
its joint attached to the hip, stretches its length to match that reach, and
returns it to rest over **Swing Duration**.
No additional hit query, damage, knockback, delay, or cooldown is introduced.
The captured direction is retained if Lenny turns during the sweep.

`PlaceholderProjectile.Launch` initializes the existing projectile movement and
asks the optional `FireballAnimation` component to orient its sprite.
`FireballAnimation` loops the four flame sprites at 16 frames per second.
The original projectile prefab path/GUID, collider, speed (12), range (14), and
damage (15) are preserved. Enemy spore projectiles keep their separate prefab.

**Player Combat → Projectile Origin** references the Player prefab's **Mouth**
child at local position `(0.10, 0.21, 0)`, aligned with the layered body's mouth.
Its local X mirrors with facing before input is processed, including a turn and
F press in the same frame. It follows the player root as Lenny moves and jumps.
Up/Down aiming changes travel direction while retaining the mouth origin. The
projectile's existing shooter exclusion allows it to leave the player's collider.
For existing controller setups without a launch-point reference, the previous
collider-clearance origin remains the fallback.

Unrelated controller references remain attached to their original objects.
The charge state, timer, and Charge Indicator object are removed.
The former wing objects now render Lenny's wings. The health bar,
physics, and colliders remain available. The visible cyan "pipe" was the Aim
Indicator's Aim Line and Aim Tip; their SpriteRenderers are disabled in the Player
prefab. The aim Transform reference remains intact and vertical aiming still works.
The old basic melee visual reference remains intact but X uses Lenny's actual
tail instead. The original basic-melee reach/damage fields remain serialized for
compatibility; X always uses the full-strength tail settings. The legacy
"charged" flag in the animation and practice-target interfaces identifies this
full-strength attack; it does not require charging.
The placeholder rectangle and placeholder wing scaling are disabled through their
new Inspector toggles in this Player prefab. Other controller setups can still
use the full-strength placeholder visual through its toggle.

All layers inherit the existing body renderer's health flash and death tint.
Disabling combat or losing focus resets both animation poses.

## Inspector adjustments

On **Player → Lenny Animation**, adjust **Wing Flap Duration**, **Wing Flaps**,
**Wing Flap Angle**, and **Sweep Arc**. **Resting Tail Reach** describes the art's
resting length; update it when replacing the tail artwork or changing its scale.
The tail sweep uses **Player Combat → Swing Duration**. There is no charge
duration setting. On
`Assets/Prefabs/PlaceholderProjectile.prefab`, **Fireball Animation → Frames Per
Second** changes the flame playback speed without affecting travel speed.

## Artwork

`Assets/Art/Sprites/Lenny.png` and its importer are preserved. Derived art is in
`Assets/Art/Sprites/LennyAnimation/`: body, left wing, right wing, tail, and a
four-frame fireball atlas. The built-in image-generation tool produced these
approved layers and frames. The full generation prompts are in that folder's
`ARTWORK.md`, for the art owner to review or replace. Unity import pivots align
the body parts at their joints and the fireball cores between frames; the PNG
files are stored unchanged from the generated outputs.

## Verification

Unity 6000.6.1f1 compiled the gameplay scripts and passed 134 Play mode checks
(67 in Movement and 67 in a separate scene with a fresh Player prefab instance).
These exercised movement and facing, vertical aim, unchanged first jump,
stronger second jump, wing-joint rotation and recovery, blocked third jump,
hidden aim-pipe renderers, mouth-origin launches and travel in all four aiming
directions, removal of the charge bar, damage on the exact X press frame,
holding/releasing X without extra attacks, fresh presses creating new sweeps,
synchronized left/right sweeps, unchanged damage, reach, knockback, and duration,
one-hit behavior, focus cancellation, disable/re-enable while X stays held,
fireball animation, projectile hits, solid-platform collision, maximum range,
shooter exclusion, high-speed sweep collision, and health-flash propagation.

Unity camera renders were checked for Lenny's proportions, joint placement,
foot/platform alignment, wing poses, tail contact, mouth-origin fireballs, and
removal of the cyan aim pipe.
Export checks confirmed that original PNG/importer hashes, prefab GUIDs,
unrelated object IDs, controller references/settings, and physics components
were preserved. Only the charge timer/reference and Charge Indicator object were
removed from the prefab. Existing scenes and project settings were not modified.
Checks ran in a temporary project copy using the project's cached packages and
URP settings. A standalone game build and manual play session were not run.
