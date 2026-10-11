# Gameplay sound effects — Lost in the Forest

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
