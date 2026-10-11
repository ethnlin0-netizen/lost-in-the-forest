# Lost in the Forest — biome music

Six cinematic fantasy selections matched to the six forest backgrounds, progressing from luminous woodland to ominous corruption. These are edited excerpts of existing music by Scott Buckley, not original compositions for this game. The initial asset preparation did not edit scenes or gameplay. The GameLoop scenes now include music playback as described below.

## Selections and measured results

| Biome / prepared loop | Original track and source | Direction | Duration | Integrated LUFS | True peak dBTP |
| --- | --- | --- | ---: | ---: | ---: |
| [Prismblood Forest](Loops/PrismbloodForest_Loop.wav) | [Echoes Of Home](https://www.scottbuckley.com.au/library/echoes-of-home/) — Scott Buckley | Luminous rainbow woodland: warm orchestral wonder and gentle melodic movement. | 94s | -18.00 | -5.04 |
| [Gloamthorn Canopy](Loops/GloamthornCanopy_Loop.wav) | [Moonlight](https://www.scottbuckley.com.au/library/moonlight/) — Scott Buckley | Blue-violet moonlit canopy: restrained piano and strings, quiet mystery. | 88s | -18.00 | -4.85 |
| [Ashpetal Forest](Loops/AshpetalForest_Loop.wav) | [Reverie](https://www.scottbuckley.com.au/library/reverie/) — Scott Buckley | Rose and ash-pink woodland: dreamlike piano, soft strings and wistful atmosphere. | 92s | -18.00 | -4.72 |
| [Veinroot Hollow](Loops/VeinrootHollow_Loop.wav) | [The Long Dark](https://www.scottbuckley.com.au/library/the-long-dark/) — Scott Buckley | Ancient amber roots and shadow: slower low strings, reflective piano and subdued tension. | 88s | -18.00 | -5.10 |
| [Wraithglass Barrens](Loops/WraithglassBarrens_Loop.wav) | [Permafrost](https://www.scottbuckley.com.au/library/permafrost/) — Scott Buckley | Grey crystalline wasteland: cold, spacious strings, atmospheric layers and solitary violin. | 96s | -18.00 | -4.70 |
| [Dreadcrown Forest](Loops/DreadcrownForest_Loop.wav) | [Decoherence](https://www.scottbuckley.com.au/library/decoherence/) — Scott Buckley | Blood-red corrupted forest: ominous strings, dark atmospheric textures and unsettled tension. | 102s | -17.99 | -3.55 |

Selections use orchestral, piano, strings and atmospheric textures rather than the previous retro direction. Descriptions are based on the creator’s descriptions and the supplied visual references; suitability and absence of vocal textures have not been verified by listening.

## Preparation

The unchanged source downloads are retained as `Sources/<track>/Original.mp3`, with source URLs, SHA-256 hashes and license records. The free source files are MP3s: prepared WAVs are decoded PCM, not lossless originals. All six loops are stereo, 44,100 Hz, 16-bit PCM WAV. There is no resampling, time stretching, pitch shifting, EQ, dynamic compression or limiter.

Candidate excerpt boundaries were compared using pitch-class spectra, nearby context and energy. This numerical selection aids editing but does not establish bar alignment or musical continuity. The selected tail and head use a raised-cosine, unity-sum crossfade; the loop is rotated so that the overlap is inside the file. A 3 ms endpoint ramp brings the first and last samples to zero without inserting a silent interval. Static gain matches whole-loop loudness to approximately −18 LUFS, retaining musical dynamics.

| Biome | Source excerpt (seconds) | Crossfade | Static gain | Loudness range (LU) |
| --- | ---: | ---: | ---: | ---: |
| Prismblood Forest | 174.0–270.0 | 2s | -3.97 dB | 7.0 |
| Gloamthorn Canopy | 64.5–154.5 | 2s | -4.25 dB | 3.6 |
| Ashpetal Forest | 63.5–157.5 | 2s | -3.67 dB | 4.1 |
| Veinroot Hollow | 140.5–232.5 | 4s | -4.04 dB | 3.9 |
| Wraithglass Barrens | 220.0–320.0 | 4s | -3.71 dB | 8.5 |
| Dreadcrown Forest | 97.5–203.5 | 4s | -2.45 dB | 11.1 |

Exact durations, gain, excerpt boundaries, source/output hashes and measurements are in [Measurements.json](Measurements.json). Measurements use FFmpeg 7.1 `loudnorm` analysis on the finished PCM files; the loudnorm filter was used for measurement only.

## Unity import

Prepared loops and retained sources use stereo (Force To Mono off), Streaming, Vorbis, quality 80%, Preserve Sample Rate (44,100 Hz), Load In Background on and Preload Audio Data off. The running Unity Editor creates and preserves all `.meta` files. Select a loop in the Project window and use its AudioClip Inspector preview with looping enabled to audition it.

## GameLoop playback

| Scene under Assets/Scenes/GameLoop | Prepared loop |
| --- | --- |
| PrismbloomLevel01 | VeinrootHollow_Loop.wav |
| GloamthorneLevel02 | GloamthornCanopy_Loop.wav |
| Level03 | AshpetalForest_Loop.wav |
| Boss | DreadcrownForest_Loop.wav |

Each scene has one `LevelMusic` object with an AudioSource set to Play On Awake, Loop, 2D (Spatial Blend 0), volume 0.6 and pitch 1. No persistent music object or additional listener is created; unloading a scene removes its source. The existing menu's AudioListener pause also pauses music. Music follows scene changes directly, without crossfading. No mixer exists in the project, so output uses the default audio bus.

`Tools/ConfigureGameLoopMusic.cs` is an Editor setup snippet for Unity CLI eval_file, not a runtime gameplay script. It updates the scene-owned sources idempotently. Scene geometry, player and enemy components, and clip import settings remain unchanged. The game's existing credits panel includes the creator, titles, website, license URL and adaptation notice for these four selections.

Verified in Unity 6000.6.1f1 Play mode on October 10, 2026 with `Tools/VerifyGameLoopMusic.cs`: all four scenes loaded their assigned clip, automatically played with an advancing audio cursor, had one source and one enabled listener, paused/resumed with AudioListener.pause, wrapped after a seek near the clip end, and removed the previous scene's music source on a single-scene load. The checks used normal Unity and audio callbacks; the pause check allows queued audio to settle before measuring. Scenes were loaded through the Editor's Play-mode scene API, including Boss, which is not currently in the build scene list. This does not verify progression through the full game or add Boss to that list.

The existing title credits panel was opened in Play mode: the music credit was active and TextMeshPro reported no overflow at the current Game view size. Automatic text sizing was enabled for the longer credit. Scene-file comparison found only the added music objects/root references plus Unity's automatic serialization of two default fields in PrismbloomLevel01; no gameplay objects were removed or repositioned. Listening was unavailable: advancing playback and loop wrapping do not establish audible quality, seamless phrases, volume balance during combat, or three full heard repetitions.

## Checks and listening limitations

- Measured: all loops last 88–102 seconds; whole-loop loudness spans only 0.01 LU; true peaks range from −5.10 to −3.55 dBTP. Every loop contains zero clipped samples.
- Measured: three concatenated PCM repetitions have zero sample steps at both file boundaries. This checks waveform discontinuities, not musical phrasing or compressed-playback quality.
- Unity import and preview checks are recorded in [Verification.json](Verification.json). They verify imported clips, advancing Inspector-preview playback and a seek across a loop boundary; they do not constitute listening.
- Listening was unavailable. No claim is made that three full repetitions were heard, that every crossfade is musically seamless, that all transitions have equal perceived loudness, or that the loops have been checked under gameplay sound effects. Equal integrated loudness does not guarantee equal perceived volume at every possible transition point.
- Before integration, listen to at least three complete repeats of each loop, especially the edited overlap near the end; compare all adjacent biome changes. Adjust excerpt boundaries or fades if a melody, pulse or swell resets awkwardly. These are prepared assets awaiting that listening acceptance check.

## License and required attribution

All six originals are released under [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/). See the retained [legal code](Sources/CC-BY-4.0-LegalCode.txt), per-track `LICENSE.md` files, and [Scott Buckley’s usage guidance](https://www.scottbuckley.com.au/library/using-this-music/). Credit the composer and license in the game’s music credits; identify the excerpt/loop/gain edits. Keep this information with redistributed assets. No endorsement by the composer is implied.

Suggested game credit block:

- “Echoes Of Home” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Prismblood Forest: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/echoes-of-home/) · [License](https://creativecommons.org/licenses/by/4.0/)
- “Moonlight” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Gloamthorn Canopy: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/moonlight/) · [License](https://creativecommons.org/licenses/by/4.0/)
- “Reverie” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Ashpetal Forest: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/reverie/) · [License](https://creativecommons.org/licenses/by/4.0/)
- “The Long Dark” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Veinroot Hollow: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/the-long-dark/) · [License](https://creativecommons.org/licenses/by/4.0/)
- “Permafrost” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Wraithglass Barrens: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/permafrost/) · [License](https://creativecommons.org/licenses/by/4.0/)
- “Decoherence” by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au. Adapted for Dreadcrown Forest: excerpted, crossfaded into a loop and level-adjusted. [Original](https://www.scottbuckley.com.au/library/decoherence/) · [License](https://creativecommons.org/licenses/by/4.0/)

The composer’s usage guidance also requests synchronization with a media project rather than isolated music resale/streaming reuploads, and excludes registration with audio-fingerprinting/Content ID services. These files are prepared for later integration into Lost in the Forest.

The four GameLoop scenes use the selections listed above. Listening acceptance remains pending.
