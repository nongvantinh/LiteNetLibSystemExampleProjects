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

public struct UserInputData
{
    public Vector3 Direction;
    public Vector2 RotationVector;
    public UserInputAction Actions;
    public Vector3 TargetPosition; // Navigation
    private static readonly UserInputData _identity = new UserInputData(0);
    public static UserInputData Identity => _identity;

    private UserInputData(int _)
    {
        Direction = Vector3.Zero;
        RotationVector = Vector2.Zero;
        TargetPosition = Vector3.Zero;
        Actions = UserInputAction.None;
    }
}

public abstract partial class BaseBehaviour3D : Node3D
{
    protected IMovable Body;
    /// <summary>
    /// Emitted when ability has been active, is called when [b]set_active()[/b] is set to true.
    /// </summary>
    [Signal]
    public delegate void ActivedEventHandler();
    /// <summary>
    /// Emitted when ability has been deactive, is called when [b]set_active()[/b] is set to false.
    /// </summary>
    [Signal]
    public delegate void DeactivedEventHandler();
    /// <summary>
    /// Defines whether or not to activate the ability. Returns true if the ability is active.
    /// </summary>
    [Export]
    public virtual bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }
            _active = value;
            if (_active)
            {
                EmitSignal(SignalName.Actived);
            }
            else
            {
                EmitSignal(SignalName.Deactived);
            }
        }
    }
    private bool _active = false;

    public abstract void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta);

    public override void _Ready()
    {
        base._Ready();
        Body = GetParent<IMovable>();
        Body.RegisterBehavior(this);
    }

}
