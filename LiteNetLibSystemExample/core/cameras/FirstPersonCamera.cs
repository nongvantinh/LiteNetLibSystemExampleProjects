using Godot;

public partial class FirstPersonCamera : BaseCamera
{
    [Export] private float _smoothSpeed = 6.25f;
    [Export] private float _sensitivity = 0.002f;
    [Export] private float _cameraMinVertical = -55; // -55 recommended
    [Export] private float _cameraMaxVertical = 75; // 75 recommended
    private Node3D _horizontalAxis = null;
    private Node3D _verticalAxis = null;

    public override void _Ready()
    {
        base._Ready();

        _horizontalAxis = GetNode<Node3D>("horizontal_axis");
        _verticalAxis = GetNode<Node3D>("horizontal_axis/vertical_axis");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        Update(delta);
    }

    private void Update(double delta)
    {
        if (Target == null)
        {
            return;
        }

        GlobalPosition = Pivot.GlobalPosition;
        float horizontalAxis = (float)Mathf.Lerp(_horizontalAxis.Rotation.Y, CameraRotation.X, delta * _smoothSpeed);
        float verticalAxis = (float)Mathf.Lerp(_verticalAxis.Rotation.X, CameraRotation.Y, delta * _smoothSpeed);

        _horizontalAxis.Rotation = new Vector3(_horizontalAxis.Rotation.X, horizontalAxis, _horizontalAxis.Rotation.Z);
        _verticalAxis.Rotation = new Vector3(verticalAxis, _verticalAxis.Rotation.Y, _verticalAxis.Rotation.Z);
        HorizontalRotation = horizontalAxis;
    }

    public override void Rotate(Vector2 input)
    {
        base.Rotate(input);

        CameraRotation.X += input.X * _sensitivity;
        CameraRotation.Y += input.Y * _sensitivity;

        CameraRotation.Y = Mathf.Clamp(CameraRotation.Y, Mathf.DegToRad(_cameraMinVertical), Mathf.DegToRad(_cameraMaxVertical));
    }

}
