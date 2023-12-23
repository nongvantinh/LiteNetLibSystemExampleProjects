using Godot;

public partial class BaseCamera : Node3D
{
    [Export] protected Camera3D CameraInstance = null;
    [Export] public Node3D Pivot { get; set; }
    [Export] public Node3D Target { get => _target; set { _target = value; OnTargetChanged(); } }
    private Node3D _target;
    public bool Current { get => CameraInstance.Current; set { CameraInstance.Current = value; } }
    public float HorizontalRotation { get; protected set; }
    [Export] public Vector3 CameraRotation = Vector3.Zero;

    public virtual void Rotate(Vector2 input)
    {
    }

    protected virtual void OnTargetChanged()
    {
    }
}
