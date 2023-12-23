using Godot;

public partial class SwimBehaviour3D : BaseMovementBehaviour3D
{
    /// <summary>
    /// Emitted when character controller touched water.
    /// </summary>
    [Signal] public delegate void EnteredWaterEventHandler();
    /// <summary>
    /// Emitted when character controller stopped touching water.
    /// </summary>
    [Signal] public delegate void ExitedWaterEventHandler();
    /// <summary>
    /// Emitted when we start to float in water.
    /// </summary>
    [Signal] public delegate void StartedFloatingEventHandler();
    /// <summary>
    /// Emitted when we stop to float in water.
    /// </summary>
    [Signal] public delegate void StoppedFloatingEventHandler();

    /// <summary>
    /// Minimum height for [CharacterController3D] to be completely submerged in water.
    /// </summary>
    [Export]
    private float _submergedHeight = 0.36f;
    /// <summary>
    /// Minimum height for [CharacterController3D] to be float in water.
    /// </summary>
    [Export]
    private float _floatingHeight = 0.55f;
    /// <summary>
    /// Speed multiplier when floating on water.
    /// </summary>
    [Export]
    private float _onWaterSpeedMultiplier = 0.75f;
    /// <summary>
    /// Speed multiplier when submerged in water.
    /// </summary>
    [Export]
    private float _submergedSpeedMultiplier = 0.5f;
    [Export] private RayCast3D _raycast;
    private bool _isOnWater = false;
    private bool _isFloating = false;
    private bool _wasOnWater = false;
    private bool _wasFloating = false;
    /// <summary>
    // Returns the height of the water in [Character Controller 3D].
    // 2.1 or more - Above water level
    // 2 - If it's touching our feet
    // 0 - If we just got submerged.
    /// </summary>
    public float DepthOnWater { get; private set; } = 0.0f;

    public override bool Active
    {
        get => base.Active;
        set
        {
            _isOnWater = _raycast.IsColliding();

            DepthOnWater = _isOnWater ? -_raycast.ToLocal(_raycast.GetCollisionPoint()).Y : 2.1f;

            base.Active = DepthOnWater < _submergedHeight && _isOnWater && value;
            _isFloating = DepthOnWater < _floatingHeight && _isOnWater && value;

            if (IsOnWater && !_wasOnWater)
            {
                EmitSignal(SignalName.EnteredWater);
            }
            else if (!IsOnWater && _wasOnWater)
            {
                EmitSignal(SignalName.ExitedWater);
            }

            if (IsFloating && !_wasFloating)
            {
                EmitSignal(SignalName.StartedFloating);
            }
            else if (!IsFloating && _wasFloating)
            {
                EmitSignal(SignalName.StoppedFloating);
            }

            _wasOnWater = _isOnWater;
            _wasFloating = _isFloating;
        }
    }

    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }
        if (!IsFloating)
        {
            return;
        }

        float speed = IsFloating ? _onWaterSpeedMultiplier : _submergedSpeedMultiplier;

        velocity = inputData.Direction * speed;

        if (_floatingHeight - DepthOnWater < 0.1f)
        {
            // Prevent free sea movement from exceeding the water surface.
            velocity.Y = Mathf.Min(velocity.Y, 0);
        }
    }

    /// <summary>
    /// Returns true if we are touching the water.
    /// </summary>
    public bool IsOnWater => _isOnWater;

    /// <summary>
    /// Returns true if we are floating in water.
    /// </summary>
    public bool IsFloating => _isFloating;

    /// <summary>
    /// Returns true if we are submerged in water.
    /// </summary>
    public bool IsSubmerged => Active;
}
