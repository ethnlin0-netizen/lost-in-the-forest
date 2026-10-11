# Gloamthorne Course 2

Open `Assets/Scenes/GloamthorneLevel02.unity` and press Play. The separate scene preserves Level02's course geometry, camera settings, thorn hazards, entrance recovery, and crystal. Level02 currently contains three enemy placement markers and no active enemy instances; these are preserved.

The new GloamthorneCanopy-Panorama.png is a pixel-art reinterpretation of the original GloamthornCanopy background, matching Prismbloom's rendering style while retaining cool indigo foliage, violet mist, crystal leaves, thorny corruption, and faint green bark fissures. Three contiguous sections cover 81 by 27 world units. They remain fixed in world space with no follow controller. Point filtering, no mipmaps, full sprite rectangles, and uncompressed import keep the artwork crisp and joins aligned.

Lenny uses the same original-artwork mesh animator as Prismbloom, fitted around the inherited player collider. The inherited alternate LennyAnimation is disabled only in this separate scene to avoid overlapping visual systems. Shared movement, combat, and original player prefab remain unchanged.

The crystal in the separate PrismbloomLevel01 scene now loads GloamthorneLevel02. The original Level01 and Level02 scenes are untouched. Gloamthorne's second crystal retains Level02's destination, Level03.

Editor setup order: CreateGloamthorneLevel02 (only if the destination does not exist), ConfigureGloamthorneLenny, ConfigureGloamthornePanorama, then ConnectGloamthorneLevel02. These are Unity Pipeline eval snippets, not MonoBehaviours.

Verification: 27 camera coverage checks and both sprite joins passed while existing gameplay components were unchanged. Play Mode checks passed for idle, walk, facing, melee, actual ranged projectile, damage, double jump, pause, finite mesh geometry, nine runtime coverage positions/aspects, and thorn return to entrance. The actual Course 1 crystal handler loaded Gloamthorne at its entrance. Start, middle, and end renders were visually reviewed. Original Level02 and background image hashes remained unchanged.

The panorama was generated with the built-in image generator. The exact prompt is saved in Tools/GloamthornePanoramaPrompt.txt.
