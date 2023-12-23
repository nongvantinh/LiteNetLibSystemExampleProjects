using Godot;

public partial class NavigationBehaviour3D : BaseMovementBehaviour3D
{
    [Export]
    private NavigationAgent3D _navigationAgent = null;
    [Export]
    private float _speed = 3.6f;
    private Vector3 _movementTargetPosition = Vector3.Zero;

    public override void _Ready()
    {
        base._Ready();
        _navigationAgent.TargetReached += () => { _movementTargetPosition = Vector3.Zero; };
    }

    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        if (inputData.TargetPosition != Vector3.Zero)
        {
            _navigationAgent.TargetPosition = _movementTargetPosition = inputData.TargetPosition;
        }

        if (IsMoving())
        {
            Vector3 tempVelocity = (_navigationAgent.GetNextPathPosition() - GlobalTransform.Origin).Normalized() * _speed;
            velocity.X = tempVelocity.X;
            velocity.Z = tempVelocity.Z;
        }
    }

    public Shape3D GetShape()
    {
        return GetNode<CollisionShape3D>("CollisionShape3D").Shape;
    }

    public bool IsMoving()
    {
        return _movementTargetPosition != Vector3.Zero || !_navigationAgent.IsNavigationFinished();
    }
}