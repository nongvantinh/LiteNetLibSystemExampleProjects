using Godot;

public partial class GravityBehaviour3D : BaseMovementBehaviour3D
{
    [Export]
    private float _gravity = GameManager.Gravity;
    [Export]
    private float _mass = 2.0f;

    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        velocity.Y -= _gravity * _mass * delta;
    }
}
