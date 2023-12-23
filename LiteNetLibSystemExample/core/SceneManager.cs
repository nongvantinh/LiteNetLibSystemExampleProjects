using Godot;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

    [Export] public PackedScene _rtsCameraScene, _firstPersonCameraScene, _thirdPersonCameraScene;
    [Export] public PackedScene CharacterScene;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }
}
