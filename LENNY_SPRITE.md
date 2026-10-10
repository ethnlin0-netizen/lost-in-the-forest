# Lenny sprite

Open `Assets/Scenes/LennyPreview.unity` in Unity to inspect Lenny's artwork.
The separate preview has an orthographic camera and labels and is not added to
the game's build scene list.

Drag `Assets/Prefabs/LennyVisual.prefab` from the Project window into any scene
or Hierarchy to reuse the artwork. This visual-only prefab contains a Transform
and SpriteRenderer. The existing gameplay Player prefab and controls are unchanged.

The original PNG is stored locally at `Assets/Art/Sprites/Lenny.png` without
editing. Its transparency, aspect ratio, and transparent padding are preserved.
Import settings match the other characters: single sprite, bottom-center pivot,
full-rectangle geometry, bilinear filtering, uncompressed texture, and 512 pixels
per unit. The SpriteRenderer uses the project's URP Sprite-Unlit-Default material
and does not require lighting. Prefab scale `(1, 1, 1)` is an initial display scale;
adjust Transform scale to try different sizes.

## Source

Downloaded on October 10, 2026 from the supplied
[Lenny image](https://cdn.discordapp.com/attachments/1558288908175941735/1558572629772996728/image.png?ex=6acbed5f&is=6aca9bdf&hm=165fd6b982a83e53f7fd2ce6780d372954f48006d03ac25927c0cc7854eb2c61).
Unity uses the local PNG and does not depend on continued Discord access.

## Verification

Imported and reopened in a separate Unity 6000.6.1f1 verification project using
the project's URP settings. The visual prefab and linked preview scene instance
have valid sprite references. The rendered preview was visually checked for the
correct artwork, transparency, labels, and framing. The exported PNG matches
the downloaded original byte for byte, and existing project assets are unchanged.
