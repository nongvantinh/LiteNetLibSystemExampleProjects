using Godot;

public partial class JumpBehaviour3D : BaseMovementBehaviour3D
{
    // Jump/Impulse Height.
    [Export]
    private float _jumpForce = 9.0f;

    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        if (Body.IsOnFloor() && (inputData.Actions & UserInputAction.Jump) != 0)
        {
            velocity.Y = _jumpForce;
        }
    }
}