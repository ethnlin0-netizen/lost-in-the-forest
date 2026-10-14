# Lost in the Forest

A 2D fantasy platformer developed at **Knight Hacks IX** by a four-person team using **Unity and C#**. Play as Lenny, explore a magical forest consumed by corruption, recruit Bloom companions, and fight the Blights standing in your way.

**Download the playable build from [GitHub Releases](https://github.com/ethnlin0-netizen/lost-in-the-forest/releases).** Extract the downloaded archive and launch the game; Unity is not required to play.

## Gameplay demo

[**Watch the gameplay demo on YouTube**](https://youtu.be/i5maxjDw0L0?si=qFrGcENRt8p1Jtrc)

## Story

The same magical amulet that cursed Lenny and led to the destruction of Knight Hacks VIII has shattered into six pieces, each tied to a different biome. Its curse is spreading through the forest, turning it toxic and hostile.

The **Blooms** seek to collect and destroy the pieces, restore the forest, and help T.K. and Lenny escape. The **Blights** seek to reunite the amulet and keep them trapped forever.

## Features

- **Platforming and combat:** Run, double jump, strike enemies with a tail sweep, and fire directional fireballs.
- **Distinct enemies:** Chomper pursues and bites, Spora fires poisonous spores, and Collosus delivers heavy close-range attacks.
- **2D platform navigation:** Mobile enemies use walking, jumps, and safe drops to pursue the player while accounting for walls and platform edges.
- **Recruitable companions:** Touch Whisperseed, Dryad, or Petalborn to recruit them. They follow Lenny and persist between levels.
- **Six gameplay stages:** Explore changing forest environments, collect amulet shards, and face a final boss combining Blight traits.
- **Gameplay feedback:** Health bars, hit feedback, knockback, enemy deaths, and level resets when the player dies.
- **Audio:** Music and sound effects accompany exploration, movement, combat, recruitment, and the boss encounter.

## Controls

| Input | Action |
| --- | --- |
| Left / Right arrow | Move and face left or right |
| Z | Jump; press again while airborne to double jump |
| X | Tail sweep with knockback |
| F | Fire a fireball |
| Up / Down arrow | Aim fireballs vertically while held |
| Enter | Return to the menu after the ending |

Without vertical aiming, fireballs travel in Lenny's facing direction. In the Unity Editor, click the Game view before using the keyboard.

## Run the project

1. Install **Unity 6000.6.1f1** through Unity Hub.
2. Install [Git LFS](https://git-lfs.com/) before cloning; the repository uses it for large assets.
3. Clone the repository and download its LFS assets:

   ```bash
   git lfs install
   git clone https://github.com/ethnlin0-netizen/lost-in-the-forest.git
   cd lost-in-the-forest
   git lfs pull
   ```

4. Add the repository folder as a project in Unity Hub and open it with the matching Editor version.
5. Allow Unity to import assets and restore packages.
6. Open `Assets/Scenes/GameLoop/TitleScreen.unity` and press **Play**.

## Build

Open **File → Build Profiles**, select the target platform, and install its build-support module through Unity Hub if needed. Include all seven scenes in `Assets/Scenes/GameLoop`, with **TitleScreen first**.

Use **Build and Run** to test a standalone build. Save the output outside `Assets`. For a release, disable **Development Build** and rebuild. When sharing a Windows build, distribute the entire output folder rather than only the executable.

## Team

| Contributor | Primary responsibility |
| --- | --- |
| [ctkimondo](https://github.com/ctkimondo) | Player movement, combat, health, and status systems |
| [ethnlin0-netizen](https://github.com/ethnlin0-netizen) | Level design and environment layout |
| [RyanRothmund](https://github.com/RyanRothmund) | Art, sprites, animation, boss fight, and boss AI |
| [mattmirf](https://github.com/mattmirf) | Enemy AI, navigation, and attack behavior |

The team collaborated through Git branches and pull requests, with contributions spanning these primary roles.

## Project structure

| Folder | Contents |
| --- | --- |
| `Assets/Scenes/GameLoop` | Title screen and playable levels |
| `Assets/Scripts` | Gameplay, AI, companions, and audio systems |
| `Assets/Prefabs` | Reusable gameplay objects |
| `Assets/Art` | Character and environment artwork |
| `Assets/Audio` | Music and sound effects |
| `Docs` | System guides, art notes, and asset attribution |
| `Tools` | Asset preparation and Editor verification utilities |

## Credits and documentation

Music selections include edited tracks by **Scott Buckley**. Third-party music and sound effects retain their own licenses and attribution requirements; see the [music credits](Docs/Assets/Audio/Music/MUSIC.md) and [sound-effect credits](Docs/Assets/Audio/SFX/SFX.md) before reusing assets.

Additional project guides are available in [Docs](Docs/). Contributors should follow [AGENTS.md](AGENTS.md) and preserve Unity `.meta` files when changing assets.
