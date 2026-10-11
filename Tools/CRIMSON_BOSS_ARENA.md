# Crimson Dreadcrown arena

Open `Assets/Scenes/GameLoop/Boss.unity`. This rebuild is local and has not been
pushed. The scene uses the fused Blight Amalgam artwork, a fully crimson forest
background without the former icy transition, a continuous 38-unit floor, and
eleven one-way escape shelves. Pale weathered stone separates playable surfaces
from the red scenery. The asymmetric routes have varied widths, uneven gaps,
1.25–1.4-unit rises, and an offset top shelf. Two optional outer perches use
double jump. Extra regular enemies were removed from this boss arena.

## Boss

- 1,000 maximum HP; inset 3 x 3.35 collider, aligned to the feet of 3.8-unit art.
- Pursuit speeds 2.1 / 2.8 units per second, with bounded physical movement.
- Stone slam: 25 damage, a marked windup, ground shockwave and recovery opening.
- Thorn lunge: 20 damage, committed direction, physics hull checks and recoil.
- Spore volley: three visible projectiles at 12 damage per projectile.
- Crimson eruption: three marked floor zones, then short 20-damage thorn bursts.
  Each patch warns for 0.48–0.63 seconds, hits at most once, and can be jumped over.
  The center erupts first and the outer zones follow in a ripple.
- Close-range claw swipe: 14 damage within 1.15 units of the hull, a 0.22-second
  warning, 0.85-second cooldown, committed direction and line-of-sight checks.
  Leaving range or jumping above the swipe avoids it; player invulnerability still applies.
- Faster scene timings (windup / recovery / cooldown, in seconds): slam
  0.60 / 0.70 / 1.65; lunge 0.50 / 0.65 / 1.55; spores 0.65 / 0.48 / 2.00;
  thorns 0.70 / 0.65 / 2.30. Charge speed is 9; spores 6.2; shockwave 6.5.
- Scene-only crimson/gold slash ribbons and impact rings, faceted spore darts
  with fading trails, outlined shockwaves, and curved thorns with warm tips.
  Effects have no colliders, expire promptly and cancel when the boss dies.
- At half health, one non-damaging phase transition enables five-zone eruptions
  and the existing slam/spore combination behavior.
- Procedural breathing, heavy gait, separate attack windups, recoil, phase shake,
  hit response, and death collapse animate the fused sprite around its feet.
  This is state-driven sprite motion, not a hand-drawn frame animation sheet.
- Existing health bar, attack telegraph, defeat fragment and
  player controls and health are retained. Collecting the victory crystal now
  plays the teammates' existing T.K./Lenny ending before returning to the menu.
  The separate purple chest-gem overlay was removed at the user's request;
  recovery vulnerability and its damage multiplier remain functional.

The new thorn attack is enabled only on this scene's boss. The original boss
prefab is preserved; this scene's boss instance was unpacked for the unique rig.
One-way shelves do not act as invisible horizontal walls for pursuit/lunges.

## Audio and artwork

Twelve original synthetic effects live in `Assets/Audio/SFX/CrimsonBoss`.
`CrimsonBossCatalog.asset` copies the existing catalog, replacing only boss cues
and adding two thorn cues. Other scenes retain their original audio catalog.
Boss music is the original `Boss_Dreadcrown_Crowncrusher` loop.

`DreadcrownForest-CrimsonArena.png` was generated with the built-in image tool
using the existing Dreadcrown background as an edit reference. Prompt is in
`Tools/CrimsonBossArtPrompt.md`. Enemy artwork retains its smooth outlined style;
the background uses point sampling. No global rendering settings were changed.

Rebuild with the Unity eval statement block in `Tools/RebuildCrimsonBoss.cs`,
then apply `Tools/RestyleCrimsonPlatforms.cs` for the final stone texture, irregular
layout, and gem removal. Then run `Tools/PolishCrimsonBoss.cs` for the final faster
abilities, melee strikes and effects. `CrimsonArena-PaleStone.png` uses point sampling and was
generated with the built-in image tool; its prompt is in `CrimsonBossArtPrompt.md`.
SFX can be reproduced using `Tools/ComposeCrimsonBossSfx.py` (NumPy required).
Backups of the open scene's edits are retained under `.local-backups`.

Run `Tools/ConnectBossEnding.cs` after rebuilding to copy the original
`BossStory` scene's Story Sequence hierarchy into Boss and rebind its boss/player
references. `waitForCrystal` makes it wait for collection rather than boss death.
`BossStory` remains unchanged as a standalone scene. Story progression now routes
to the updated Boss arena. `Tools/VerifyBossEnding.cs` verifies defeat, collection,
the waking/celebration sequence and return to TitleScreen with pause restored.

## Verification

`Tools/VerifyCrimsonBoss.cs` passed in a temporary Boss Play-mode session using
real Physics2D simulation, Input System keys, reflection for isolated attack
cases, and stepped callbacks:

- Four attack poses preserve the collider; music and sound cues resolve.
- Slam windup is non-damaging, impact does 25 damage, and recovery exposes chest.
- Lunges move the physics body and face correctly in both directions.
- A spore fan launches three owned projectiles.
- Thorn warnings do not damage; active patches hit once, airborne players avoid
  them, and patch timing freezes while paused.
- Phase two starts once and expands the thorn pattern to five zones.
- Both complete ascent routes reach the top shelf using normal jumps and real
  landings and running approaches, without teleporting between steps. Both outer
  perches are reachable using the existing double jump. Combat checks were rerun
  successfully after the irregular layout and gem removal.
- Death cancels attacks, hazards, collisions and movement, begins the collapse,
  and reveals the existing victory crystal.
- Real slam, lunge, volley, thorn, phase and death events played their sound cues.

The rebuilt arena was rendered and visually inspected. Unity was returned to
edit mode after verification. Human combat balance, subjective audio quality,
and a full uninterrupted fight-through remain to be playtested.
