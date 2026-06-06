# New Game Project

A game built with the [Godot Engine](https://godotengine.org/) (4.6, Forward+ renderer, Jolt physics).

> Created for SekiroJam. Open source and open to contributions — see below.

## Getting started

1. Install **Godot 4.6** (or newer) from <https://godotengine.org/download>.
2. Clone this repository:
   ```bash
   git clone <repo-url>
   cd new-game-project
   ```
3. Open the Godot editor, click **Import**, and select the `project.godot` file in this folder.
4. Press **F5** (or the Play button) to run the game.

## Project layout

| Path | Purpose |
| --- | --- |
| `project.godot` | Godot project configuration. |
| `world_1.tscn` | Main world scene. |
| `icon.svg` | Project / window icon. |

## Contributing

Contributions are welcome! To make changes:

1. Create a branch: `git checkout -b my-feature`
2. Make your changes in the Godot editor.
3. Commit and push your branch: `git push -u origin my-feature`
4. Open a Pull Request describing what you changed.

Please keep scenes and scripts focused, and avoid committing the local `.godot/`
cache directory (it's already git-ignored).

## License

Released under the [MIT License](LICENSE).
