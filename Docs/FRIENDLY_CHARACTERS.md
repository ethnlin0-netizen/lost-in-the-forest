# Friendly character artwork

Open `Assets/Scenes/FriendlyCharactersPreview.unity` in Unity to see Whisperseed,
Dryad, and Petalborn side by side. The preview includes an orthographic camera
and character labels and is not added to the game's build scene list.

## Reuse

The reusable prefabs are in `Assets/Prefabs/FriendlyCharacters`:

- `Whisperseed.prefab`
- `Dryad.prefab`
- `Petalborn.prefab`

Drag a prefab from the Project window into any scene or its Hierarchy. Each
prefab contains only a Transform and SpriteRenderer. There are no movement,
AI, combat, recruitment, power-up, or physics components.

The original, unmodified PNG files are stored in
`Assets/Art/Sprites/FriendlyCharacters`. They are imported as single sprites
with transparency, a bottom-center pivot, full-rectangle geometry, bilinear
filtering, and uncompressed textures. All three use 512 pixels per unit and
prefab scale `(1, 1, 1)` as an initial display scale; this does not establish
their final gameplay sizes. Adjust an instance's Transform scale to try other
sizes, or change the prefab scale to update all its instances.

The SpriteRenderer uses the project's URP Sprite-Unlit-Default material, so the
artwork remains visible without scene lighting. Original aspect ratios and
transparent padding are preserved.

## Verification

The assets were imported, saved, and reopened in a separate Unity 6000.6.1f1
verification project using this project's URP settings. All three prefabs have
valid sprite references, contain only their visual components, and are linked
to instances in the preview scene. A rendered preview was visually checked for
the correct character artwork, transparency, labels, and framing. The exported
PNG files match the downloaded originals byte for byte.

## Sources

Downloaded on October 10, 2026. These links are for provenance; Unity uses the
local PNG files and does not depend on Discord access.

- [Whisperseed](https://cdn.discordapp.com/attachments/1558288908175941735/1558572543471001640/image.png?ex=6acbed4b&is=6aca9bcb&hm=5b22d4b8f054c33233baa031ffb6f8ce175cb472dd72c1bb5079950bb2c8664a)
- [Dryad](https://cdn.discordapp.com/attachments/1558288908175941735/1558572602451034182/image.png?ex=6acbed59&is=6aca9bd9&hm=b60b5dc866773ff811cfcab9c0d41a7c493e4650caf5cf0ff284fd886af8d6d0)
- [Petalborn](https://cdn.discordapp.com/attachments/1558288908175941735/1558572710169149570/image.png?ex=6acbed72&is=6aca9bf2&hm=571f9dbd77e9607a3a021c9c1a48e545819c1f609016c8f2d38f7e11699029ea)
