# Gloamthorne Course 2

Open `Assets/Scenes/GloamthorneLevel02.unity` and press Play. The separate scene preserves Level02's course geometry, camera settings, thorn hazards, entrance recovery, and crystal. Gloamthorne contains seven placed enemies: two Chompers, two Sporas, and three Collusus. Their artwork, attack cues, health bars, and animation references are connected to their actual scene children. PlatformNavigation2D on Level02Route uses LevelGeometry's twelve surfaces for enemy pursuit and attacks.

The new GloamthorneCanopy-Panorama.png is a pixel-art reinterpretation of the original GloamthornCanopy background, matching Prismbloom's rendering style while retaining cool indigo foliage, violet mist, crystal leaves, thorny corruption, and faint green bark fissures. Three contiguous sections cover 81 by 27 world units. They remain fixed in world space with no follow controller. Point filtering, no mipmaps, full sprite rectangles, and uncompressed import keep the artwork crisp and joins aligned.

Lenny retains Prismbloom's animated body for idle, walk, jump, landing, ranged recoil, hit and death reactions. In Gloamthorne, the mesh samples the existing separated LennyBody texture, while LennyAnimation animates the separate wings and tail. The aligned rig mirrors with facing and gives the tail a full rotating, extending swipe without drawing a second tail or body. The player collider, movement and combat damage settings remain unchanged.

PlayerHealth now restarts the player's current scene after a 0.75-second death reaction. Lenny returns to the authored entrance with full health; enemies and other scene objects reset. This shared death behavior applies to the other enabled biome scenes as well.

The crystal in the separate PrismbloomLevel01 scene now loads GloamthorneLevel02. The original Level01 and Level02 scenes are untouched. Gloamthorne's second crystal retains Level02's destination, Level03.

Editor setup order: CreateGloamthorneLevel02 (only if the destination does not exist), ConfigureGloamthorneLenny, ConfigureGloamthornePanorama, then ConnectGloamthorneLevel02. After placing enemies, FixGloamthorneEnemyBindings reconnects their visuals and navigation. These are Unity Pipeline eval snippets, not MonoBehaviours.

Verification: 27 camera coverage checks and both sprite joins passed while existing gameplay components were unchanged. Play Mode checks passed for idle, walk, facing, melee, actual ranged projectile, damage, double jump, pause, finite mesh geometry, nine runtime coverage positions/aspects, and thorn return to entrance. The actual Course 1 crystal handler loaded Gloamthorne at its entrance. Start, middle, and end renders were visually reviewed. Original Level02 and background image hashes remained unchanged.

The panorama was generated with the built-in image generator. The exact prompt is saved in Tools/GloamthornePanoramaPrompt.txt.

Final animation checks passed for tail rotation, extension and recovery in both directions, matching body/part facing, and absence of duplicate body/tail artwork. The full player-animation, pause, background-coverage and thorn-recovery checks passed with this combined rig. Seven enemy animations, actual attacks, hit/death reactions and pause checks passed; five mobile enemies passed pursuit-route checks. Death respawn was verified at the exact scene-load frame in Gloamthorne: entrance (-5, 12), full health, movement and physics restored.
