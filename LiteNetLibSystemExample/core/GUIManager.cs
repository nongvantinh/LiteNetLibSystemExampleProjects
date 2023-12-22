using Godot;

public partial class GUIManager : Control
{
    public static GUIManager Instance { get; private set; }
    [Export] public RichTextLabel DebugText;
    [Export] public VirtualJoystick MovementJoystick = null;

    [Signal] public delegate void ScreenWipeEventHandler(Vector2 relative);
    [Export] public TextureButton SwitchCameraModeBtn { get; set; } = null;

    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!GameManager.IsPlaying)
        {
            return;
        }

        Vector2 input = Vector2.Zero;
        // Desktop mouse controls.
        if ((GameManager.Instance.CurrentOS & SupportedOS.Desktop) != 0)
        {
            if (inputEvent is InputEventMouseMotion mouseMotion)
            {
                input = mouseMotion.Relative;
                GetViewport().SetInputAsHandled();
            }
        }
        // Mobile touch controls.
        else if ((GameManager.Instance.CurrentOS & SupportedOS.Mobile) != 0)
        {
            if (inputEvent is InputEventScreenDrag screenDrag)
            {
                input = screenDrag.Relative;
                GetViewport().SetInputAsHandled();
            }
        }

        EmitSignal(SignalName.ScreenWipe, -input);
    }
}
