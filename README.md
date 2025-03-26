This project is meant to provide a working version of using `LiteNetLibSystem` with Godot Engine.


The client prediction still doesn't work well due to Godot's `MoveAndSlide` method not accepting the parameter `delta`. There's an ongoing discussion about bringing this option back in the engine [here](https://github.com/godotengine/godot/pull/84665), but the timeline is uncertain.

Godot 4.4 currently doesn't support simulating physics multiple times at once either. It's unclear when this option will be introduced, but there is a proposal for it: [Add ability to simulate physics manually/multiple times at once](https://github.com/godotengine/godot-proposals/issues/2821).

## Prerequisites
- [Godot 4.4.stable .NET Version](https://godotengine.org/download/archive/4.4-stable/)
- [.NET SDK 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Project Usage

1. Open one of the provided projects.
2. Build the project.
3. Go to **Debug** in the top menu, then select **Customize Run Instances**.
4. Enable 3 instances and set the **Launch Argument** for:
   - Instance 1 to `--server`
   - Instance 2 to `--client`
   - Instance 3 to `--client`
   
   ![Customize Run Instances](docs/run-instances.png?raw=true "Customize Run Instances")

5. Run the game in the editor.


You can use the virtual joystick to move the character, press `ESC` to release the mouse capture, and use mouse movement to apply rotation.

## Project Structures

### LiteNetLibSystemExample
The first folder, `LiteNetLibSystemExample`, provides the basic version of the game. It contains pure character movement and rotation, nothing more. This way, you can gain an idea of how to use `LiteNetLibSystem` with Godot.