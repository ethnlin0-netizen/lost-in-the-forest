# Prismbloom Course 1

Open `Assets/Scenes/PrismbloomLevel01.unity` and press Play. This separate scene combines the original Level01 course and enemy instances with Lenny's original artwork and a world-fixed, pixelated Prismbloom forest panorama. Original Level01 and biome artwork remain available unchanged.

The panorama has three contiguous square sprite sections occupying 72 by 24 world units. Point filtering, full sprite rectangles, and shared source boundaries keep the joins aligned. The background covers camera travel at aspect ratios through 2.4 without following the player or camera.

LennyVisualAnimator animates the original artwork mesh from movement, melee, ranged, and health events without moving the gameplay collider. The current shared combat system performs a full-strength tail sweep on X press and launches fireballs on F. Only the separate course disables the inherited LennyAnimation component and hides its alternate artwork, so two visual systems do not overlap. Other scenes keep their current shared player prefab.

Editor setup snippets in Tools run through Unity Pipeline eval, not as MonoBehaviours. To recreate the scene, run CreatePrismbloomLevel01 only when the destination does not exist, then ConfigurePrismbloomLenny, ConfigurePrismbloomPanorama, and SyncPrismbloomWithMain in that order. Run VerifyPrismbloomLenny in a fresh Play session, then exit Play Mode.

Verification after syncing main: nine camera coverage checks; idle, walking, facing, melee, actual ranged projectile, damage, double jump, pause, and finite mesh geometry all passed. Panorama setup separately checked 27 camera coverage positions/aspects, two section joins, and unchanged gameplay components.

Background images were generated with the built-in image generator. Exact prompts are preserved in Tools/PrismbloomPixelatedPrompt.txt and Tools/PrismbloomPanoramaPrompt.txt.
