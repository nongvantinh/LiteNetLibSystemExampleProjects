using Godot;

public partial class WalkBehaviour3D : BaseMovementBehaviour3D
{
    [Export(PropertyHint.Range, "2,8")]
    private float _speed = 3.6f;
    [Export]
    public float _acceleration = 3.0f;
    [Export]
    public float _deceleration = 5.0f;
    [Export(PropertyHint.Range, "1,0.05")]
    public float _airControl = 0.3f;

    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        float yAxis = velocity.Y;
        velocity.Y = 0;
        Vector3 target = inputData.Direction * _speed;

        float tempAccel = (0 < inputData.Direction.Dot(velocity)) ? _acceleration : _deceleration;

        if (!Body.IsOnFloor())
        {
            tempAccel *= _airControl;
        }

        velocity = velocity.Lerp(target, tempAccel * delta);
        velocity.Y = yAxis;
    }
}