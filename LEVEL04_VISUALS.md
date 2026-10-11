# Level04 — Veinroot Hollow visuals

Open `Assets/Scenes/Level04.unity` in Unity and press Play. The scene now uses
the existing `Assets/Art/Sprites/VeinrootHollow-Background.png` behind the
original Level04 course. Its aspect ratio is preserved. It stays fixed in world
space and covers the original camera travel, including the entrance view, at
4:3, 16:9, and wide aspect ratios through 2.4:1.

The 25 original terrain colliders have separate tiled visual children. Like
PrismbloomLevel01, those children cancel their parent objects' scale so textures
keep the same density on small platforms and large banks. The existing
Prismbloom stone and moss sprites are reused; their PNGs and import settings
have not changed. Level04-specific materials remap their colors to weathered
brown stone, red foliage, and amber highlights. Stone texture coordinates line
up in world space so touching surfaces share the same pattern.

The palette materials are in `Assets/Materials/VeinrootHollow`. Edit their
shadow, stone, foliage, and highlight colors to tune this scene. They use
`Assets/Shaders/VeinrootTerrain.shader`. Distant columns have subdued, tiled
surfaces, and the old stretched edge strips are hidden. Purple acid now uses
`Assets/Shaders/VeinrootAcid.shader` with animated currents, bubble rings, and
surface ripples. Its two materials in the same folder expose the acid colors
and visual flow speed. World coordinates keep texture density consistent across
both pools. Hazard triggers, boundaries, and contact behavior are unchanged.
Enemies and the amulet keep their existing art.

All original scene objects remain present. Terrain placement and size, camera
limits, colliders, rigidbodies, navigation, the four original enemy instances,
player controllers, hazards, and amulet progression are preserved. Two additional
Chompers occupy RaisedRuins and the middle ledge; an additional Spora occupies
the upper ruins platform. All three remain instances of the existing integrated
enemy prefabs, with their existing health, attacks, and navigation settings.
There are seven enemies total (four Chompers and three Spora).

The Level04 Player instance has `LevelRestartOnDeath`. Falling into acid or
reaching zero health reloads the saved level, restoring Lenny's health and all
enemy health, spawn positions, controllers, and colliders, including enemies
that were destroyed. Acid contact uses this component; health deaths use the
existing `PlayerHealth` biome restart and its 0.75-second death reaction delay.
This avoids competing scene loads. This resets progress within Level04. The shared
`Level02ThornRespawn` exposes a `ReturnedToEntrance` event; scenes without this
restart component retain their existing return-to-entrance behavior. Shared
player and enemy prefab assets, other scenes, project settings, and original
artwork are unchanged.

`Tools > Lost In The Forest > Style Level 04 Visuals` reapplies the treatment.
It refuses to run during Play mode or while an open scene has unsaved changes.
The setup checks existing transforms and serialized gameplay components before
saving. A local original-scene copy is kept in the ignored
`Temp/Level04VisualWork` folder.

## Verification

- Setup verified 64 camera coverage samples and preserved 80 gameplay components
  and 139 existing transforms.
- A serialized scene comparison found all 314 original object/component IDs
  still present and unchanged camera, physics, collider, and script blocks.
- 99 current Play mode checks passed: all 25 terrain surfaces align with their
  colliders at uniform texture scale; movement, first jump, stronger wing jump,
  blocked third jump, immediate tail sweep, one attack per press, fireballs and
  damage, vertical aiming, and all seven enemies initialize correctly.
- Both retry paths were verified separately: an actual acid trigger collision
  and health reaching zero reloaded Level04, restoring all seven enemies to full
  health and their starting positions, and Lenny to 300 health with working
  movement, combat, and physics. The acid test included a defeated, destroyed
  enemy and another enemy that had been moved and damaged.
- Adding the acid visuals, enemies, and restart component retained all 514
  existing scene object/component IDs and left the existing camera, physics,
  collider, and script blocks unchanged. Original enemy prefab overrides remain
  intact; the Player instance adds only the restart component.
- Camera renders at the entrance, middle, and exit were inspected for coverage,
  terrain consistency, and readability.
- Touching the amulet through an actual physics collision successfully loaded
  Level05; the editor was then restored to Level04.
- Original PNGs, texture import settings, and shared prefab assets are intact.

The Play mode smoke checks use controlled input and physics steps. Delayed enemy
death cleanup was completed explicitly in the acid test because the unfocused
editor did not advance game time; actual enemy damage, acid collision, scene
loading, and restored states were exercised. After integrating main's existing
health restart, its coroutine was advanced past the delay under controlled
testing, confirming it reloads the level without a competing acid restart.
The actual 0.75-second elapsed wait was not measured. Movement/combat checks and
both restored-state checks passed again against the pulled main changes.
A complete manual run through
every platform and a standalone build were not performed.
