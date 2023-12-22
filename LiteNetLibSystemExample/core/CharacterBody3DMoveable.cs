using Godot;

public partial class CharacterBody3DMoveable : CharacterBody3D, IMovable
{
    [Export]
    public Node3D _head { get; set; }
    [Export]
    public Node3D _pivot { get; set; }


    public BaseEntityPawn AttachedPlayer { get; set; }

    [Export(PropertyHint.Range, "2,8")]
    private float _speed = 60.6f;
    [Export]
    public float _acceleration = 3.0f;
    [Export]
    public float _deceleration = 5.0f;
    [Export(PropertyHint.Range, "1,0.05")]
    public float _airControl = 0.3f;

    public void Movement(UserCommandData command, float delta)
    {
        Vector3 velocity = Vector3.Zero;

        float yAxis = velocity.Y;
        velocity.Y = 0;
        Vector3 direction = new Vector3(command.MovementVector.X, 0.0f, command.MovementVector.Y);
        Vector3 target = direction * _speed;

        float tempAccel = (0 < direction.Dot(velocity)) ? _acceleration : _deceleration;

        if (!IsOnFloor())
        {
            tempAccel *= _airControl;
        }

        velocity = velocity.Lerp(target, tempAccel * delta);
        velocity.Y = yAxis;

        Velocity = velocity;
        MoveAndSlide();
    }
}
