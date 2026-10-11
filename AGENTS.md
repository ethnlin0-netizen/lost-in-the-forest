# Lost in the Forest

## Project context

Lost in the Forest is a 2D Unity game. The magical amulet that cursed Lenny and
led to the destruction of Knight Hacks VIII has shattered into six pieces, each
tied to a different biome. Its curse is spreading through the forest, turning
half of it toxic and hostile.

- **Bloom:** Collect all six amulet pieces and destroy them to end the curse,
  restore the forest, and help T.K. and Lenny escape.
  Classes: Whisperseed, Dryad, and Petalborn.
- **Blight:** Bring together the six amulet pieces to keep the forest corrupted
  and trap T.K. and Lenny in the never-ending forest.
  Classes: Chomper, Spora, and Collosus.

## Team roles

| Team member | Primary responsibility | Scope |
| --- | --- | --- |
| ctkimondo | Game mechanics | Movement, fighting, health, and status systems. |
| ethanlin0-netizen | Level design | Biome layouts, level structure, terrain, and placement of gameplay elements. |
| RyanRothmund | Design/UI | Character/enemy art, environments, sprites, animations, and other visual and technical assets. |
| mattmirf | Enemy AI | Enemy behavior, detection, navigation, targeting, and attack decisions. |

## Collaboration guidelines

- Use the roles above to identify the primary owner of a feature and coordinate
  changes that span responsibilities.
- Enemy AI should integrate with the movement, combat, and health systems owned
  by ctkimondo. Coordinate changes to shared interfaces between those systems.
- Coordinate enemy placement and navigation needs between mattmirf and
  ethanlin0-netizen.
- Coordinate visual asset and animation requirements with RyanRothmund.
- Keep changes focused on the requested task and preserve teammates' existing
  work. Avoid unrelated changes to shared scenes, prefabs, or project settings.
- Keep Unity `.meta` files with their corresponding assets and include them in
  version control. Move or rename assets through Unity's Project window when
  possible so references remain intact.
- Do not commit generated Unity folders such as `Library`, `Temp`, `Logs`, or
  `obj`.
- Before staging (`git add`) or committing (`git commit`), run `git status`,
  then `git pull --ff-only` to bring in remote changes. Run `git status` again
  after pulling and review the changes before proceeding.
- If the pull fails or local changes prevent it, resolve the issue before
  staging or committing. Preserve local work; do not discard changes or force
  an update to make the pull succeed.

## Guidance for coding agents

- Use the game premise and team roles above as context for project work.
- Treat team roles as ownership and coordination guidance, not as a restriction
  on work explicitly requested by a team member.
- Do not invent mechanics, class abilities, or biome details that have not been
  specified. Ask for clarification when a task depends on those decisions.
- For code changes, perform verification appropriate to the change and report
  any checks that could not be completed.
