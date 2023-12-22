using Godot;
using LiteEntitySystem;

public partial class BaseEntityController : HumanControllerLogic<UserCommandData, BaseEntityPawn>
{
    private UserCommandData _nextCommand;

    public BaseEntityController(EntityParams entityParams) : base(entityParams)
    {
    }

    public override void GenerateInput(out UserCommandData input)
    {
        input = _nextCommand;
        _nextCommand.RotationVector = Vector2.Zero;
    }

    public override void ReadInput(in UserCommandData input)
    {
        ControlledEntity.SetInput(input);
    }

    protected override void OnConstructed()
    {
        base.OnConstructed();
        LinkControls();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnlinkControls();
    }

    public void LinkControls()
    {
        GUIManager.Instance.MovementJoystick.Touch += OnMovementInitiation;
        GUIManager.Instance.MovementJoystick.Drag += OnMovementAiming;
        GUIManager.Instance.MovementJoystick.Release += OnMovementExecution;

        GUIManager.Instance.ScreenWipe += OnScreenWipe;
    }

    public void UnlinkControls()
    {
        GUIManager.Instance.MovementJoystick.Touch -= OnMovementInitiation;
        GUIManager.Instance.MovementJoystick.Drag -= OnMovementAiming;
        GUIManager.Instance.MovementJoystick.Release -= OnMovementExecution;

        GUIManager.Instance.ScreenWipe -= OnScreenWipe;
    }


    private void OnMovementInitiation()
    {
    }

    private void OnMovementAiming(Vector2 direction)
    {
        _nextCommand.MovementVector = direction;
    }

    private void OnMovementExecution(bool isInsideDeadzone)
    {
        _nextCommand.MovementVector = Vector2.Zero;
    }

    private void OnScreenWipe(Vector2 relative)
    {
        _nextCommand.RotationVector = relative;
    }
}
