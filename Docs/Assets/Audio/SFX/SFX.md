# Gameplay sound effects — Lost in the Forest

## GameLoop coverage repair — October 11, 2026

The saved-scene audit found sound adapters on every player and enemy, with valid
licensed clips in the existing catalog. Veinroot Level04 and Wraithglass Barrens
Level 05 lacked a GameAudioController, so their adapters silently rejected sound
requests. Both scenes now have a scene-owned controller using the existing catalog,
24 effect voices, and 0.8 master effects gain. Their original music clips and 0.6
music gains are preserved. No ambience, actor placement, physics, health, enemy
navigation, or shared prefab changes were needed.

| Scene | Audit and repair |
| --- | --- |
| PrismbloomLevel01 and PrismbloomLevel01Story | Existing controller and adapters retained. Shared amulet/restart feedback added. |
| GloamthorneLevel02 | Existing controller retained. Shared amulet, hazard-death and restart feedback added. |
| Ashpetal Level 03 | Existing controller retained. Shared amulet/restart feedback added. |
| Veinroot Level04 | Added missing controller. Existing actor adapters now play; shared interactions also work. |
| Wraithglass Barrens Level 05 | Added missing controller. Existing actor adapters now play; shared interactions also work. |
| Boss and BossStory | Existing audio retained. Boss defeat and crystal appearance voices can finish during the story pause. Story return to title uses existing transition feedback. |
| TitleScreen | Preserved WraithglassBarrens_Loop, looping automatically at 0.6 gain. |

AmuletCollect reuses the existing licensed CrystalTouch clip at 0.4 cue gain;
LevelRestart reuses Resume at 0.3 cue gain. Both have a 0.5-second minimum interval,
one concurrent pooled voice, fixed pitch, and priority 45. The new enum values are
appended, preserving every old serialized cue ID. Successful collection plays one
short tail across the scene transition. Restart requests queue one cue on the
replacement scene's controller, rather than preserving old music or audio managers.
Lethal hazard callbacks use the PlayerDeath clip as a short transition tail so an
immediate reload cannot cut it off. Health-death timing remains unchanged.

Transition requests now respect their cue's emitter cooldown and remain limited to
four temporary voices. Existing gameplay effects keep their per-emitter intervals,
per-cue concurrency, and 24-voice pool. Attack warning cancellation is preserved.
Only boss defeat and crystal appearance presentation are exempted from the story's
listener pause; ordinary attacks and movement remain silent while paused.

`Tools/AuditGameLoopAudio.cs` reads all nine saved GameLoop scenes through preview
scenes. `Tools/ConfigureGameplayAudioScenes.cs` repairs missing controllers/adapters
in all of them without rewriting existing music or ambience settings. Repeating
the setup produced no additional scene changes. `Tools/ConfigureGameplaySfx.cs`
also retains the two new cue aliases when rebuilding the catalog.

### Verification for this repair

- 46 native-frame gameplay checks passed in the repaired Level04 using temporary
  prefab fixtures: walking, jump/double jump/landing, melee and projectiles, all
  three Blights, hit/death feedback, all three Bloom recruitment cues, boss attacks
  and phase feedback, pause/resume, warning cancellation, and bounded voices.
- `Tools/VerifyGameLoopAudioCoverage.cs` passed 327 checks across all eight saved
  gameplay scenes. Each scene exercised native player movement and ranged input,
  controlled basic/charged melee execution, physical projectile impacts, native
  Blight windups/attacks, damage/death cues, real health-death reloads, and unchanged
  music playback. Scenes with hazards and amulets also exercised repeated trigger
  callbacks and verified one cue, proper reload feedback, and original destinations.
  The short restart cue was sampled during its lifetime. Temporary input settings
  and synthetic devices were restored after the run.
- `Tools/VerifyBossSceneAudio.cs` passed 29 checks covering both authored boss scenes:
  slam/lunge/spore/phase cues, defeat and crystal reveal, advancing defeat audio
  during the actual story pause, normal crystal collision and story return to title,
  one title music loop, and transition-tail cleanup.
- Measured PCM checks found zero clipped samples in all 56 prepared effects;
  maximum magnitude was 16,423/32,768 (approximately -6 dBFS). Every effect retained
  its matching metadata. No effect or music waveform was changed.

Detailed results are in [GameLoopVerification.txt](GameLoopVerification.txt).
The checks use actual Play-mode frames and audio playback state/cursors, plus
controlled runtime positions and selected private-method calls. Enemy damage was
temporarily suppressed in attack fixtures; hazard/amulet callbacks were invoked
directly for repeat-contact checks. This is not a manual full-level playthrough.
No listening check was performed, so perceived balance and subjective sound quality
remain to be reviewed by a person. No Blooms are authored in these GameLoop scenes;
their existing prefab recruitment integration was verified with temporary fixtures.

Prepared and integrated on `codex/music-assets`, 2026-10-10, using the running Unity 6000.6.1f1 Editor. These are adaptations of existing licensed recordings/effects, **not original recordings or generated compositions**.

## Delivered assets and scope

56 mono effect files supply 43 Inspector-configurable cues in `GameAudioCatalog.asset`. Four stereo wind/leaf loops are in `Assets/Audio/Ambience`. Sources, supplied licenses and provenance are retained in `Sources`. No new abilities, animation, movement, attack timing, damage values, navigation or progression were introduced.

The four GameLoop gameplay scenes have a scene-owned `GameAudio` controller and `ForestAmbience` source. Their existing music selections and 0.6 music gain are preserved. The moved GameLoop TitleScreen has menu sound adapters and the required leaf attribution; its original scene GUID is preserved. Unity automatically updated its existing build-list path to reflect that move. The latest main update renamed Level03 to Ashpetal Level 03 and selected the copy under Assets/Scenes in the build list; the same audio setup was retained in both copies of this third level. Other levels were preserved.

Reusable Player/BossPlayer, Blight, boss, projectile/hazard and BloomPet prefabs now carry `GameplaySfx` where appropriate. Existing scene instances, including unpacked enemies, also have adapters. Shared prefab behavior stays silent in scenes without a `GameAudioController`; adding sounds to other scenes is a separate integration task. No Blooms were newly placed in GameLoop scenes.

## Sources and permissions

| Retained pack | Creator | Primary source | License | Uses |
| --- | --- | --- | --- | --- |
| KenneyRPG | Kenney / Kenney Vleugels | [RPG Audio](https://kenney.nl/assets/rpg-audio) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | Cloth, jaw snap/chop, plant creak |
| KenneyImpacts | Kenney / Kenney Vleugels | [Impact Sounds](https://kenney.nl/assets/impact-sounds) | CC0 | Grass steps, soft impacts, rock/mining impacts |
| KenneyInterface | Kenney / Kenney Vleugels | [Interface Sounds](https://kenney.nl/assets/interface-sounds) | CC0 | Restrained plucks and clicks |
| Swishes | artisticdude | [Swishes sound pack](https://opengameart.org/content/swishes-sound-pack) | CC0 | Jump, wings, tail sweep, lunge, projectile puffs |
| WetSquish | Independent.nu; distributed by qubodup | [8 Wet Squish Slurp Impacts](https://opengameart.org/content/8-wet-squish-slurp-impacts) | CC0 | Fungal windups, spore textures, wet hits |
| Leaves | Tim Mortimer; distributed by OpenGameArt/qubodup | [Leaves Cracking and Crumbling](https://opengameart.org/content/leaves-cracking-and-crumbling) | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) | Chomper, boss, sparse ambience |
| Fireball | Julien Matthey; submitted by diligentcircle | [Fireball](https://opengameart.org/content/fireball-1) | CC0 | Player fire launch/impact |
| Magic | JaggedStone | [Magic spell SFX](https://opengameart.org/content/magic-spell-sfx) | CC0 | Three related recruitment chimes, crystal, phase accent |
| Wind | SketchMan3 | [Wind whoosh loop](https://opengameart.org/content/wind-whoosh-loop) | CC0 | Quiet biome ambience |

Each source folder includes a `LICENSE.md` with creator, source and license links. Full downloaded archives retain originals; Kenney's supplied `License.txt` and Tim Mortimer's supplied `info.txt` are also retained separately. Standalone originals are retained for fireball, the three used magical effects and wind. Wet-squish FLAC files originated from MP3; conversion cannot restore lost detail.

Required in-game attribution is included in the title/pause credits: Tim Mortimer, the OpenGameArt source URL, CC BY 3.0 license URL, and trimming/pitch/layering/volume changes. Existing Scott Buckley music credits remain intact. No noncommercial or edit-restricted material is used.

## Preparation and measured results

`Sources/Measurements.json` records every prepared filename, original source layers, pitch/time offsets, duration, rate, channels, sample peak, RMS, clipping count, endpoints and SHA-256. It also records hashes for retained source archives/files. `Tools/PrepareGameplaySfx.py` contains the preparation recipes; `Tools/GameplaySfxRecipes.json` contains the playback tuning.

Short effects: trim to activity with approximately 6 ms lead and 40 ms tail, resample to 44.1 kHz mono, mix source layers, pitch/trim selected phrases, apply 4 ms edge fades, and use static gain to reach −6 dBFS sample peak. No dynamic compression is applied. Duration range: 0.1024–3.1060 seconds. All 56 effects have zero first/last samples and measured peaks of −6.0 dBFS.

Ambience: stereo source wind, 250 ms overlap at the source loop seam, four repetitions, slower pitch for progressively darker settings, and very quiet sparse leaf overlays in the first three scenes. Boss uses wind alone. Prepared peaks are −12.0 dBFS; durations are Prismbloom 22.8372 s, Gloamthorne 24.9099 s, Ashpetal 27.0437 s and Boss 30.7829 s. These are short atmospheric beds, not replacement music.

**Measured clipping: zero samples at or above 0.999 amplitude in all 60 prepared files.** This is a file-level sample check, not a true-peak or full-game mixed-output certification. Loop wrapping was tested in Unity; audible seam quality remains a listening check.

## Import and volume settings

Effects: PCM16 WAV, mono, 44.1 kHz preserved, Decompress On Load, PCM import, preload enabled, background loading disabled. All 56 clips were observed `Loaded`, mono and 44.1 kHz in Play mode. Short uncompressed clips avoid decoding latency and lossy transients. All playback is 2D with Doppler disabled.

Ambience: stereo WAV, Streaming, Vorbis quality 80%, source rate preserved, load in background enabled, preload disabled, looping 2D. Each scene contains one ambience source and its existing single music source. Original source-copy import defaults are not used for gameplay.

Select `GameAudio` in each scene to adjust independent **musicVolume**, **effectsVolume**, and **ambienceVolume**. Defaults: music 0.6, effects 0.8; ambience Prismbloom 0.10, Gloamthorne 0.085, Level03 0.075, Boss 0.06. Actor `GameplaySfx.volume` defaults to 1. Sound gain = catalog cue volume × effects volume × actor volume. These are conservative starting gains, not an auditioned final mix.

Select `GameAudioCatalog.asset` to adjust clips, cue gain, pitch range, minimum interval per emitter, priority and maximum simultaneous instances. Most pitch ranges are 0.97–1.03; recruitment, crystal and menu cues use pitch 1 to preserve their small melodies. Common steps/hits/launches have two or three clip variations.

| Cue family | Catalog gain | Priority (lower wins) | Limits |
| --- | --- | --- | --- |
| Player footsteps / soft landing / hard landing | 0.24 / 0.35 / 0.50 | 180 / 160 / 140 | 3 voices; 0.17 / 0.12 / 0.09 s per emitter |
| Jump / double jump / tail sweep / fire launch / fire impact | 0.38 / 0.40 / 0.53 / 0.45 / 0.50 | 140 / 130 / 100 / 100 / 110 | 3 voices; fire impacts limited to 0.14 s |
| Player hurt / death | 0.50 / 0.55 | 70 / 40 | 1 voice; 0.20 / 0.50 s |
| Chomper / Spora / Collusus warning | 0.52 / 0.48 / 0.58 | 35 / 35 / 30 | 3 voices; 0.20 / 0.20 / 0.25 s |
| Bite / spore launch / spore impact / rock slam | 0.60 / 0.48 / 0.42 / 0.65 | 80 / 90 / 120 / 70 | 2–3 voices; 0.15–0.25 s |
| Regular enemy hit / death | 0.40–0.45 / 0.50–0.60 | 110–115 / 40–45 | 3 hit voices, 2 death voices |
| Rock footsteps | 0.40 | 175 | 2 voices; 0.25 s |
| Boss slam / lunge / volley warning | 0.65 / 0.62 / 0.65 | 20 | 1 voice per warning; 0.30 s |
| Boss slam / shockwave / lunge / volley | 0.70 / 0.45 / 0.65 / 0.55 | 40 / 75 / 40 / 40 | 1 attack voice; 2 shockwave voices |
| Boss hit / phase / death | 0.48 / 0.65 / 0.70 | 90 / 15 / 10 | 2 hit voices, 1 phase/death voice |
| Recruitment / crystal appearance / crystal touch | 0.35 / 0.45 / 0.45 | 130 / 45 / 40 | 2 recruitment voices, 1 crystal cue |
| Menu focus / click / pause / resume | 0.20 / 0.35 / 0.35 / 0.35 | 140 / 60 / 55 / 55 | 2 focus/click voices, 1 pause/resume voice |

A scene controller owns at most 24 effect voices. Important warnings can replace lower-priority decorative voices when the pool is full. Per-cue concurrency and per-emitter intervals suppress spam. Short crystal interaction tails can cross a scene load, are capped at four, and destroy themselves after playback. Music and ambience do not persist across scene loads.

## Integration points

- `Movement.Jumped` and `Landed(fallSpeed)` were added at the existing physics transitions; `DoubleJumped` was reused. Landings below 1.5 units/s are silent, over 12 use the hard landing. Player steps require grounded physical travel, one step per 1.7 units. Collusus uses 1.2 units and boss 2 units; airborne/idle/dead actors do not generate steps.
- Existing `PlayerCombat.MeleePerformed` and `RangedPerformed` drive sweep/launch sounds. Health-change callbacks produce hit/death effects only after accepted HP loss. Invulnerable rejected damage is silent. Existing damage deduplication and knockback were preserved.
- Blight and boss scripts expose windup/execution/cancellation notifications at their existing state transitions. Warnings stop on cancellation, normal-Blight hit interruption, death or disable. The boss volley plays one launch cue for three projectiles. The phase notification occurs only once.
- Projectile swept collisions call the adapter before destruction. Expiry/cancellation does not play a false impact. Controller-owned voices keep impacts and death tails alive after their originating actors disappear.
- Bloom recruitment calls the appropriate chime after its existing once-only registration. Enter/Stay and multiple-collider overlaps do not recruit twice. No pet attack/ability audio was added.
- Crystal reveal uses the existing reveal guard; touching it uses the existing transition guard and a short detached sound tail. Progression logic is unchanged.
- Menu adapters handle pointer focus, keyboard selection and existing Button activation. Pause/resume notifications use UI voices with `ignoreListenerPause`; gameplay effects, music and ambience obey the existing listener pause and timescale system.

## Verification

Programmatic checks ran with normal Unity/audio callbacks in Play mode, not merely direct calls to `AudioSource.Play`. The fixtures were temporary and were not saved. Three suites recorded **128 passing assertions**, plus menu focus/click and credit-layout checks. Full results are in `VerificationResults.json`; reproduction helpers are under `Tools`.

- Virtual keyboard input exercised actual player grounded movement, normal jump, double jump, X sweep and F projectile. Footsteps, exactly-once jump/landing/attack cues and upward physics passed.
- All three real Blight prefabs detected the test player, entered their existing windups, attacked, accepted damage and died. The active Chomper warning was specifically interrupted by a hit; its bite did not execute. A death audio cursor continued advancing after actual actor destruction.
- Basic melee dealt 15 damage once to a target with an additional collider; the charged sweep retained 30 damage and a projectile retained 15. Invulnerable duplicate player damage produced only one accepted HP change and one hurt cue. No combat tuning changed.
- The boss exercised slam, shockwave, lunge, volley, hit, one phase transition, and once-only defeat. Bloom prefabs recruited through real trigger overlaps and each chime fired once across Enter/Stay callbacks.
- Fireball/spore swept collisions produced one impact; range expiry produced none. The 24-voice bound, warning priority/preemption and cancellation passed.
- The existing title/pause menu stayed functional; paused gameplay rejected new sound requests. A UI audio cursor advanced while the gameplay listener was paused. Resume restored the existing input/physics/audio state.
- Each of the four authored scenes had one controller, one listener, one correct music source and one correct ambience source. Both cursors advanced, paused, resumed, and ambience wrapped. Old controllers/sources disappeared on scene changes.
- Killing the actual boss in the authored Boss scene revealed the actual crystal once. Player contact returned to TitleScreen with one touch cue. The interaction tail cleaned itself up, with no leftover gameplay music/ambience.
- Credits with the added Tim Mortimer attribution fit the existing panel: TMP reported no text overflow, auto-sized at 16.65.
- Assets imported through Unity and matching metadata was checked. Scene/prefab diffs were reviewed for unrelated gameplay changes; Unity's ordinary serialization/default-field additions remain.

**Listening checks: unavailable.** No monitored audio output was available to judge timbre, clicks, warning clarity, fatigue, or perceived balance against music. Native playback cursor checks are not a substitute for listening. Audition the four encounters before treating these gains as a final mix. No device build, full gameplay run or mixed-output clipping measurement was performed.

During initial testing Boss was absent from the build list, so tests opened it by asset path. The subsequent main update enabled Boss and connected Level 5; those upstream changes were preserved. The existing move of TitleScreen and other local music work were preserved. The user subsequently requested committing, pushing and merging this work.

## Compatibility after merging main (720d05a)

Main adds the Crimson boss arena, its catalog and thorn sounds, removes the old
PrismbloomLevel01 duplicate, and routes the crystal through the story ending.
The earlier verification above describes the pre-merge scene set. Both catalogs
now preserve BossThornWarn/BossThorns IDs 43/44 and use IDs 45/46 for
AmuletCollect/LevelRestart. Existing boss clip assignments and its new 0.5
music gain are retained. Setup scripts retain the thorn cues on regeneration.

After merging, Unity compiled successfully. All eight remaining saved GameLoop
scenes have a controller and all 47 cues assigned. Play-mode smoke checks passed
in all seven gameplay scenes: music cursors advance and collection, restart and
thorn requests produce playing effect voices. The updated boss-ending regression
passed defeat gating, crystal collection, duplicate-trigger rejection, story pause
and return to title. These are playback/logic checks, not listening checks or a
repeat of the earlier full combat suite.

## Publication checks and final scene set

The obsolete BossStory scene and metadata are removed. Build settings, ending
constants and verification tools use the current Boss scene. The GameLoop folder
contains six gameplay scenes plus TitleScreen. Boss retains its victory-crystal
ending and the prompt "Enter: Return to menu". Lenny uses the existing walking
animator, with the local Boss visual scale/alignment retained.

The publication pass completed 249 playback/logic checks across the six current
gameplay scenes and 16 Boss attack/cutscene/title-return checks, including thorn
windup and execution. A music assertion was corrected to compare playback with
the owning controller gain; Boss correctly uses 0.5 even though its AudioSource
serialized gain is 0.6. Fixtures neutralize boss claw damage while checking audio.
All 69 present PCM sound clips have matching metadata and no clipped samples.
Listening checks and a standalone build were not performed.

After incorporating main 1e5ea0b, all three newly placed Blooms were checked through
the player trigger: one recruitment sound each, a playing effect voice, and silent
companion restoration after scene reload. These nine integration checks passed.
