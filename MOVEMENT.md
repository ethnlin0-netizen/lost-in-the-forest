# Movement practice

Open `Assets/Scenes/Movement.unity` in Unity and press Play. Click the Game view
to give it keyboard focus.

- **Left / Right arrow:** move and face left / right.
- **Up / Down arrow:** aim the ranged attack straight up / down while held.
  With neither held, aim in the facing direction. Opposing arrows cancel out.
- **Z:** jump. Release and press Z again in the air for one wing-assisted double
  jump. Holding Z does not repeat jumps. Landing restores the airborne jump.
  The first jump uses a speed of 10; the wing jump uses 14 for a higher boost.
- **X:** tap and release for a basic melee hit in front of the character.
- **Hold X for at least 0.75 seconds, then release:** a wider tail sweep in front
  of the character that knocks back the practice target. The charge bar turns
  yellow when ready. Each press produces one attack on release.
- **F:** fire one projectile in the aim direction per press.

The green rectangle is the player, the orange block is a movable practice
target, and the purple shapes flash as wings on the second jump. Attacks flash
the target; the charged melee also applies knockback. The practice target has no
health or enemy AI. The reusable player has 300 HP and an overhead health bar;
enemy attacks in EnemyAI reduce it. A hit gives 0.65 seconds of protection,
with a red flash and brief flickering. At zero HP, movement and combat stop.
Projectiles stop at solid colliders or their maximum
range. Stop and restart Play mode to reset the scene after falling off the
platform or pushing the target off it.

Select **Player** to adjust movement speed, jump speeds, the ground mask,
charge time, melee reach, knockback impulse, projectile speed, and projectile
range in the **Movement** and **Player Combat** components. The projectile asset
is `Assets/Prefabs/PlaceholderProjectile.prefab`. The platform has a static
BoxCollider2D; the player and target have dynamic Rigidbody2D components with
rotation frozen.

The controller uses the existing Input System package. Losing Game view focus
or disabling the combat controller cancels an unfinished charge.

## Reusing the player

Drag `Assets/Prefabs/Player.prefab` from the Project window into another scene
and place it above a platform with a 2D collider. The prefab includes movement,
combat, physics, colliders, all placeholder visuals, and the projectile reference.
The Movement scene uses an instance of this prefab.

Select the prefab to edit **Movement → Double Jump Speed** in the Inspector.
This changes the wing-assisted jump without affecting **Jump Speed**, which
controls the first jump. Editing the prefab updates its instances unless an
instance has its own override. Each scene still needs a camera and ground;
the practice target is a separate scene object.
