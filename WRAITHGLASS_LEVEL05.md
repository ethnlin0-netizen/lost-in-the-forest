# Wraithglass Barrens Level 05

Open `Assets/Scenes/GameLoop/Wraithglass Barrens Level 05.unity` and press Play.

This separate scene copies the existing Level05 course, hazards, camera, entrance spawn and fifth crystal. The latest saved scene includes six enemies: two Chompers, two Sporas and two Collusus, including the original three thorn-section enemies and three additional user placements. The original Level05 scene is unchanged. The crystal retains its Level06 destination; earlier levels retain their existing destinations.

The background is entirely Wraithglass: shattered ice-coated trees, dark purple thorn growth, crystal clusters and lavender storm mist. There is no warm brown forest or transition from Biome 4. `Assets/Art/Sprites/WraithglassBarrens-Panorama.png` is a new 2172-by-724 pixel-art panorama generated with the built-in image generator. The source WraithglassBarrens-Background.png remains unchanged. The final edit prompt is saved in `Tools/WraithglassPanoramaPrompt.txt`.

Three contiguous sections cover 87 by 29 world units, fixed in world space. They use point sampling, uncompressed textures, no mipmaps and full sprite rectangles, matching the prior biome background setup. Camera settings and course geometry are inherited from Level05.

Lenny uses the combined animated-body and separate wing/tail rig configured for Prismbloom and Gloamthorne. The body samples the separated body texture so the rotating tail does not overlap a second tail. Movement and collider settings remain unchanged.

Validation: all 1229 inherited course components were unchanged by scene creation and background setup. Twenty-seven editor camera-coverage checks and both joins passed. In Play Mode, eighteen coverage checks passed at multiple screen proportions and camera positions; all three enemy animators and Lenny's body animator initialized; navigation found twenty-one surfaces; the crystal's Level06 destination was available. Entrance, middle and exit renders were visually inspected. Original Level05 and background files were verified unchanged. The new scene is enabled in the build list.

Setup snippets: CreateWraithglassLevel05, ConfigureWraithglassPanorama and ConfigureWraithglassLenny. VerifyWraithglassLevel05 runs in a fresh Play session and captures preview images under Library; stop Play afterward.

Enemy integration: FixWraithglassEnemies reconnects all three enemies' artwork, attack cues, health bars and the navigation surface root. Chomper's patrol radius is 0.55 and Collusus's is 1.1. Collusus uses jump speed 22 and maximum drop distance 4 in this scene so its gravity scale of 3 does not prevent crossing the gap from the safe divider to the acid approach. Spora retains its stationary ranged role. Enemy placements, colliders, course geometry and shared prefabs remain unchanged.

VerifyWraithglassEnemies passed three actual AI attack/animation checks, three hit/death/pause checks, and two patrol/pursuit checks. A separate navigation check verified that both moving enemies can route from their real placements to neighboring course platforms after the traversal adjustment.

Publish validation: after syncing main, the latest saved scene had no missing scripts, the combined Lenny rig was configured, and all six enemies had enabled AI and animation components. The prior gameplay checks covered the original three thorn-section enemies; the three additional placements were inspected in the saved scene. The enabled Wraithglass build entry was added while preserving main's new GameLoop/Level04 path.
