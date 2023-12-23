using Godot;
using LiteEntitySystem;

public enum CameraMode
{
    RTS,
    FirstPerson,
    ThirdPerson,
    Count
}

public partial class BaseEntityController : HumanControllerLogic<UserInputData, BaseEntityPawn>
{
    private UserInputData _nextCommand;
    private Vector2 _skillThreeDirection;
    private Vector2 _skillTwoDirection;
    private Vector2 _skillOneDirection;
    private Vector2 _skillZeroDirection;

    public BaseEntityController(EntityParams entityParams) : base(entityParams)
    {
    }

    protected override void GenerateInput(out UserInputData input)
    {
        input = _nextCommand;
        _nextCommand.RotationVector = Vector2.Zero;
    }

    protected override void ReadInput(in UserInputData input)
    {
        ControlledEntity.SetInput(input);
    }

    protected override void OnConstructed()
    {
        base.OnConstructed();
        LinkControls();

        SubscribeToClientRequestStruct<CameraModePacket>(SwitchCameraMode);
    }

    public void SwitchCameraMode(CameraModePacket packet)
    {
        Debug.Log($"packet: {packet.CameraMode}");
        ControlledEntity.OnSwitchCameraMode(packet.CameraMode);
    }

    private void OnSwitchCameraMode()
    {
        int mode = (int)ControlledEntity.SyncedCameraMode;
        if (++mode == (int)CameraMode.Count)
        {
            mode = 0;
        }
        if (EntityManager.IsClient)
        {
            SendRequestStruct(new CameraModePacket { CameraMode = (CameraMode)mode });
        }
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

        GUIManager.Instance.SkillZeroJoystick.Touch += OnSkillZeroInitiation;
        GUIManager.Instance.SkillZeroJoystick.Drag += OnSkillZeroAiming;
        GUIManager.Instance.SkillZeroJoystick.Release += OnSkillZeroExecution;

        GUIManager.Instance.SkillOneJoystick.Touch += OnSkillOneInitiation;
        GUIManager.Instance.SkillOneJoystick.Drag += OnSkillOneAiming;
        GUIManager.Instance.SkillOneJoystick.Release += OnSkillOneExecution;

        GUIManager.Instance.SkillTwoJoystick.Touch += OnSkillTwoInitiation;
        GUIManager.Instance.SkillTwoJoystick.Drag += OnSkillTwoAiming;
        GUIManager.Instance.SkillTwoJoystick.Release += OnSkillTwoExecution;

        GUIManager.Instance.SkillThreeJoystick.Touch += OnSkillThreeInitiation;
        GUIManager.Instance.SkillThreeJoystick.Drag += OnSkillThreeAiming;
        GUIManager.Instance.SkillThreeJoystick.Release += OnSkillThreeExecution;

        GUIManager.Instance.ScreenWipe += OnScreenWipe;
        GUIManager.Instance.SwitchCameraModeBtn.Pressed += OnSwitchCameraMode;
    }

    public void UnlinkControls()
    {
        GUIManager.Instance.MovementJoystick.Touch -= OnMovementInitiation;
        GUIManager.Instance.MovementJoystick.Drag -= OnMovementAiming;
        GUIManager.Instance.MovementJoystick.Release -= OnMovementExecution;

        GUIManager.Instance.SkillZeroJoystick.Touch -= OnSkillZeroInitiation;
        GUIManager.Instance.SkillZeroJoystick.Drag -= OnSkillZeroAiming;
        GUIManager.Instance.SkillZeroJoystick.Release -= OnSkillZeroExecution;

        GUIManager.Instance.SkillOneJoystick.Touch -= OnSkillOneInitiation;
        GUIManager.Instance.SkillOneJoystick.Drag -= OnSkillOneAiming;
        GUIManager.Instance.SkillOneJoystick.Release -= OnSkillOneExecution;

        GUIManager.Instance.SkillTwoJoystick.Touch -= OnSkillTwoInitiation;
        GUIManager.Instance.SkillTwoJoystick.Drag -= OnSkillTwoAiming;
        GUIManager.Instance.SkillTwoJoystick.Release -= OnSkillTwoExecution;

        GUIManager.Instance.SkillThreeJoystick.Touch -= OnSkillThreeInitiation;
        GUIManager.Instance.SkillThreeJoystick.Drag -= OnSkillThreeAiming;
        GUIManager.Instance.SkillThreeJoystick.Release -= OnSkillThreeExecution;

        GUIManager.Instance.ScreenWipe -= OnScreenWipe;
        GUIManager.Instance.SwitchCameraModeBtn.Pressed -= OnSwitchCameraMode;
    }


    private void OnMovementInitiation()
    {
    }

    private void OnMovementAiming(Vector2 direction)
    {
        Vector3 rotationDirection = new Vector3(direction.X, 0, direction.Y).Rotated(Vector3.Up, ControlledEntity.CurrentCamera.HorizontalRotation).Normalized();
        _nextCommand.Direction = rotationDirection;
    }

    private void OnMovementExecution(bool isInsideDeadzone)
    {
        _nextCommand.Direction = Vector3.Zero;
    }

    private void OnSkillZeroInitiation()
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}");
        ControlledEntity.Body.AttackRangeVisualizer.Show();
        float range = fts.ballistic_range(ControlledEntity.SyncedSpeed, GameManager.Gravity, ControlledEntity.Body.MuzzlePosition.GlobalPosition.Y) / 2;
        Vector3 scale = Vector3.Zero;
        scale.X = range;
        scale.Y = 1;
        scale.Z = range;
        ControlledEntity.Body.AttackRangeVisualizer.Scale = scale;
    }

    private void OnSkillZeroAiming(Vector2 direction)
    {
        _skillZeroDirection = direction;
    }

    private void OnSkillZeroExecution(bool isInsideDeadzone)
    {
        ControlledEntity.ExecuteSkillZero(_skillZeroDirection);
        ControlledEntity.Body.AttackRangeVisualizer.Hide();
    }

    private void OnSkillOneInitiation()
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}");
        ControlledEntity.Body.SkillShotVisualizer.Show();
    }

    private void OnSkillOneAiming(Vector2 direction)
    {
        _skillOneDirection = direction;
        if (direction != Vector2.Zero)
        {
            Vector3 rotation = Vector3.Zero;
            direction = direction.Rotated(-ControlledEntity.CurrentCamera.HorizontalRotation);
            rotation.Y = -Mathf.Atan2(direction.X, -direction.Y);
            ControlledEntity.Body.SkillShotVisualizer.GlobalRotation = rotation;
        }
    }

    private void OnSkillOneExecution(bool isInsideDeadzone)
    {
        ControlledEntity.ExecuteSkillOne(_skillOneDirection);
        ControlledEntity.Body.SkillShotVisualizer.Hide();
    }

    private void OnSkillTwoInitiation()
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}");
        ControlledEntity.Body.AttackRangeVisualizer.Show();
        ControlledEntity.Body.TargetAimingVisualizer.Show();
        Vector3 scale = Vector3.Zero;
        float range = fts.ballistic_range(ControlledEntity.SyncedSpeed, GameManager.Gravity, ControlledEntity.Body.MuzzlePosition.GlobalPosition.Y) / 2;

        scale.X = range;
        scale.Y = 1;
        scale.Z = range;
        ControlledEntity.Body.AttackRangeVisualizer.Scale = scale;
    }

    private void OnSkillTwoAiming(Vector2 direction)
    {
        _skillTwoDirection = direction;

        direction = direction.Rotated(-ControlledEntity.CurrentCamera.HorizontalRotation);
        // Range indicator.
        float range = fts.ballistic_range(ControlledEntity.SyncedSpeed, GameManager.Gravity, ControlledEntity.Body.MuzzlePosition.GlobalPosition.Y) / 2;

        Vector3 position = ControlledEntity.Body.GlobalPosition;
        position.X += direction.X * range;
        position.Z += direction.Y * range;

        ControlledEntity.Body.TargetAimingVisualizer.GlobalPosition = position;

        Vector3 rotation = ControlledEntity.Body.Pivot.Rotation;
        rotation.Y = -Mathf.Atan2(direction.X, -direction.Y);
        ControlledEntity.Body.Pivot.Rotation = rotation;
    }

    private void OnSkillTwoExecution(bool isInsideDeadzone)
    {
        if (isInsideDeadzone)
        {
            ControlledEntity.ExecuteSkillTwo(_skillTwoDirection);
        }

        ControlledEntity.Body.AttackRangeVisualizer.Hide();
        ControlledEntity.Body.TargetAimingVisualizer.Hide();
    }

    private void OnSkillThreeInitiation()
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}");
    }

    private void OnSkillThreeAiming(Vector2 direction)
    {
        _skillThreeDirection = direction;
    }

    private void OnSkillThreeExecution(bool isInsideDeadzone)
    {
        ControlledEntity.ExecuteSkillThree(_skillThreeDirection);
    }

    private void OnScreenWipe(Vector2 relative)
    {
        _nextCommand.RotationVector = relative;
    }
}
