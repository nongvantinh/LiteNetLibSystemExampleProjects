using Godot;

public partial class FlyBehaviour3D : BaseMovementBehaviour3D
{
    [Export]
    private float _speed = 2.0f;
    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        velocity = inputData.Direction * _speed;
    }
}
