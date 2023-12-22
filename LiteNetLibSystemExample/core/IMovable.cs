using Godot;

public enum UserInputAction
{
    None,
    Walk = 1,
    Sprint = 1 << 1,
    Swim = 1 << 2,
    Fly = 1 << 3,
    Crouch = 1 << 4,
    Jump = 1 << 5,
    Fire = 1 << 6,
    ChangeViewMode = 1 << 7,
    SkillZero = 1 << 8,
    SkillOne = 1 << 9,
    SkillTwo = 1 << 10,
    SkillThree = 1 << 11,
}

public struct UserCommandData
{
    public Vector2 MovementVector;
    public Vector2 RotationVector;
    public UserInputAction Actions;
    public Vector3 TargetPosition; // Navigation
    private static readonly UserCommandData _identity = new UserCommandData(0);
    public static UserCommandData Identity => _identity;

    private UserCommandData(int _)
    {
        MovementVector = Vector2.Zero;
        RotationVector = Vector2.Zero;
        TargetPosition = Vector3.Zero;
        Actions = UserInputAction.None;
    }
}


public interface IMovable
{
    [Export]
    public Node3D _head { get; set; }
    [Export]
    public Node3D _pivot { get; set; }
    public BaseEntityPawn AttachedPlayer { get; internal set; }

    public Vector3 GlobalPosition { get; set; }
    public Vector3 GlobalRotation { get; set; }
    public Vector3 Velocity { get; set; }

    public bool IsVisibleInTree();
    public bool IsOnFloor();
    public void Movement(UserCommandData command, float delta);
}