# Original corruption score

Four original synthesized instrumental loops composed for the current scenery. These are newly composed notes and synthesized sounds, not excerpts of the licensed Scott Buckley music already in Music/Loops. No vocals, external recordings or third-party samples were used.

| File | Scene and mood | Tempo | Length |
| --- | --- | --- | --- |
| Level03_Ashpetal_Embers.wav | Pink/red dying woodland: wistful piano-like melody, soft string pads and restrained plucked movement; enchantment with unease. | 76 BPM | 50.53 s |
| Level04_Veinroot_Pulse.wav | Tangled amber roots and dark stone: lower melody, minor/Phrygian harmony, pulsing bass and muted deep percussion. | 84 BPM | 45.71 s |
| Level05_Wraithglass_Echoes.wav | Shattered ice and purple corruption: sparse glass-like bells, cold pads, low drone and spacious stereo echoes. | 88 BPM | 43.64 s |
| Boss_Dreadcrown_Amalgam.wav | Crimson forest and Blight Amalgam combat: fast driving drums, low repeating strings, dissonant harmony and an urgent motif. | 132 BPM | 58.18 s |

The tracks share a rising/falling figure and related minor harmony. The three exploration loops use sixteen bars with changing four-bar phrases; the boss uses thirty-two bars and denser percussion. Note releases and ambience wrap into the beginning for repeated playback.

All files are stereo 44.1 kHz, 16-bit PCM WAV. Exploration tracks measure -19 dBFS RMS and the boss -17 dBFS RMS; these are RMS values, not LUFS. Peak and boundary measurements are in Verification.json. Numerical checks verified finite samples, no clipping, valid WAV headers and complete bar lengths. Subjective listening acceptance remains for the user.

Reproduce with Tools/compose_corruption_music.py (NumPy). Unity imports the tracks as streaming Vorbis audio. They are preview assets; no scene music playback, mixers, credits, or existing music have been changed.
