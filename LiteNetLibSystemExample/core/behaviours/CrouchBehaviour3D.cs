using Godot;

public partial class CrouchBehaviour3D : BaseMovementBehaviour3D
{
    [Export(PropertyHint.Range, "0,10")]
    private float _speedDecreasePercentage = 8.0f;
    /// <summary>
    /// Time for the character to reach full speed.
    /// </summary>
    [Export]
    public float _acceleration = 3.0f;
    /// <summary>
    /// Time for the character to stop walking.
    /// </summary>
    [Export]
    public float _deceleration = 5.0f;
    /// <summary>
    /// Sets control in the air.
    /// </summary>
    [Export(PropertyHint.Range, "1,0.05")]
    public float _airControl = 0.3f;
    /// <summary>
    /// Collider that changes size when in crouch state.
    /// </summary>
    [Export]
    private CollisionShape3D _collision;
    /// <summary>
    /// Raycast that checks if it is possible to exit the crouch state.
    /// </summary>
    [Export]
    private RayCast3D _headCheck;
    /// <summary>
    /// Collider height when crouch actived.
    /// </summary>
    [Export]
    private float _heightInCrouch = 1.0f;
    /// <summary>
    /// Collider height when crouch deactived.
    /// </summary>
    private float _defaultHeight = 2.0f;

    public override void _Ready()
    {
        base._Ready();
        if (_collision.Shape is CapsuleShape3D capsule)
        {
            _defaultHeight = capsule.Height;
        }
    }

    // Set collision height.
    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);

        if (!Active)
        {
            return;
        }

        if (!(_collision.Shape is CapsuleShape3D capsule))
        {
            return;
        }

        bool isCrouching = (inputData.Actions & UserInputAction.Crouch) != 0 || _headCheck.IsColliding();

        if (isCrouching)
        {
            capsule.Height -= delta * 8;
        }
        else if (!_headCheck.IsColliding())
        {
            capsule.Height += delta * 8;
        }

        capsule.Height = Mathf.Clamp(capsule.Height, _heightInCrouch, _defaultHeight);

        if (isCrouching)
        {
            velocity.X -= velocity.X * _speedDecreasePercentage / 100;
            velocity.Z -= velocity.Z * _speedDecreasePercentage / 100;
        }
    }
}
