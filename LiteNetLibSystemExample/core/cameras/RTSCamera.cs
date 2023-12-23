using Godot;

public partial class RTSCamera : BaseCamera
{
    [Export] private float _smoothSpeed = 6.25f;
    [Export] private Vector3 _offset = new Vector3(0.774f, 12.948f, 1.625f);

    public override void _Process(double delta)
    {
        base._Process(delta);
        FollowTarget((float)delta);
    }

    private void FollowTarget(float delta)
    {
        if (Target == null)
        {
            return;
        }

        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition + _offset, _smoothSpeed * delta);
        GlobalRotation = GlobalRotation.Lerp(CameraRotation, _smoothSpeed * delta);

        HorizontalRotation = Rotation.Y;
    }

}
