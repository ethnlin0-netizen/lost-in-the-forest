# Enemy character artwork

Chomper, Spora, and Collosus are the Blight enemy characters. Their assets are
kept separately from the Bloom friendly characters.

Open `Assets/Scenes/EnemyCharactersPreview.unity` in Unity to inspect all three
side by side. This separate preview scene contains a camera and character
labels and is not added to the game's build scene list.

## Reuse

Drag one of these prefabs from the Project window into any scene or Hierarchy:

- `Assets/Prefabs/EnemyCharacters/Chomper.prefab`
- `Assets/Prefabs/EnemyCharacters/Spora.prefab`
- `Assets/Prefabs/EnemyCharacters/Collosus.prefab`

Each prefab contains only a Transform and SpriteRenderer. Enemy AI, movement,
combat, health, and physics behavior have not been implemented.

The original PNG files are stored locally in `Assets/Art/Sprites/EnemyCharacters`
and remain unmodified. Import settings match the friendly character artwork:
single sprites, transparency, bottom-center pivots, full-rectangle geometry,
bilinear filtering, uncompressed textures, and 512 pixels per unit. Original
aspect ratios and transparent padding are preserved. The prefab scale is
`(1, 1, 1)` as an initial display scale, rather than a final gameplay size.
Adjust an instance's Transform scale to try other sizes, or change the prefab
scale to update its instances.

The SpriteRenderer uses the project's URP Sprite-Unlit-Default material, so
the artwork remains visible without scene lighting.

## Verification

The assets were imported, saved, and reopened in a separate Unity 6000.6.1f1
verification project using this project's URP settings. All three prefabs have
valid sprite references, contain only their visual components, and are linked
to instances in the enemy preview scene. A rendered preview was visually
checked for the correct artwork, transparency, labels, and framing. Exported
PNG files match the downloaded originals byte for byte, and the existing
friendly character assets remain unchanged.

## Sources

Downloaded on October 10, 2026. Unity uses the local PNG files and does not
depend on continued access to Discord.

- [Chomper](https://cdn.discordapp.com/attachments/1558288908175941735/1558572508351832194/image.png?ex=6acbed42&is=6aca9bc2&hm=77005bc951a919eb39b41a5446a76445328ee5ee95010d813a60c67f709048ab)
- [Spora](https://cdn.discordapp.com/attachments/1558288908175941735/1558572569001721917/image.png?ex=6acbed51&is=6aca9bd1&hm=8ab113ba05503ae839754c8a4766ec866e59faaabb418bc10440f701164476d9)
- [Collosus](https://cdn.discordapp.com/attachments/1558288908175941735/1558572744923291728/image.png?ex=6acbed7b&is=6aca9bfb&hm=3c2c8d253e97dc8478c333c9ddf06197ae54c7a02e49b28ec1f374e0e91218cf)
