This project is meant to provide a working version of using `LiteNetLibSystem` with Godot Engine.

## Prerequisites
- Godot 4.2.stable.mono
- .NET SDK 8

## Project Usage

1. Open one of the provided projects.
2. Build the project.
3. Open the `game_manager` scene. In the Inspector panel, `uncheck` the `IsServer` field.
4. Export the project.
5. Go back to the editor, `check` the `IsServer` field, and run the server in the editor.
6. Go to the exported folder and run the exported game.

You can use the virtual joystick to move the character, press `ESC` to release the mouse capture, and use mouse movement to apply rotation.

## Project Structures

### LiteNetLibSystemExample
The first folder, `LiteNetLibSystemExample`, provides the basic version of the game. It contains pure character movement and rotation, nothing more. This way, you can gain an idea of how to use `LiteNetLibSystem` with Godot.