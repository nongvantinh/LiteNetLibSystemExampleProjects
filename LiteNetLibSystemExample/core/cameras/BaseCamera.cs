using Godot;

public partial class BaseCamera : Node3D
{
    [Export]
    protected Camera3D Camera = null;

    [Export]
    public Node3D Pivot { get; set; }

    [Export]
    public Node3D Target { get => _target; set { _target = value; OnTargetChanged(); } }
    private Node3D _target;

    public bool Current { get => Camera.Current; set { Camera.Current = value; } }
    public float HorizontalRotation { get; protected set; }
    [Export]
    public Vector3 CameraRotation = Vector3.Zero;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public virtual void Rotate(Vector2 input)
    {

    }

    protected virtual void OnTargetChanged()
    {

    }

}
