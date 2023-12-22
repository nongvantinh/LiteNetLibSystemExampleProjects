using Godot;

public partial class VirtualJoystick : TextureRect
{
    public enum JoystickType { Static, Fixed, Free }

    [Signal] public delegate void TouchEventHandler();
    [Signal] public delegate void DragEventHandler(Vector2 direction);
    [Signal] public delegate void ReleaseEventHandler(bool isInsideDeadzone);

    [Export(PropertyHint.Range, "range(0,1)")] public float JoystickRatio = 0.6f;
    [Export] public JoystickType Type { get; set; } = JoystickType.Fixed;
    [Export] public float Deadzone = 0.0f;
    [Export] private bool _drawDeadzone = false;

    public TextureRect JoystickRing { get; set; } = null;
    private Vector2 CenterOfJoystick => GlobalPosition + (Size / 2);

    private int _eventId = -1;
    private Vector2 _eventPosition = Vector2.Zero;
    private bool _joystickStart = false;
    private float _radius = 0.0f;
    private bool _isDeadzoneReached = false;
    private Vector2 _direction { get; set; } = Vector2.Zero;
    private SupportedOS _currentOS { get; set; }

    public override void _Ready()
    {
        if (0.001f < Mathf.Abs(Size.X - Size.Y))
        {
            Debug.Log($"Rect size must be equal X: {Size.X}, Y: {Size.Y}");
        }

        _currentOS = GameManager.Instance.CurrentOS;
        JoystickRing = GetNode<TextureRect>("joystick_ring");
        _radius = Size.X / 2;
        JoystickRing.Size = Size * JoystickRatio;
        OnRelease();
    }

    public override void _GuiInput(InputEvent ie)
    {
        if (!IsVisibleInTree())
            return;
        if (ie is InputEventMouseButton mb && (_currentOS & SupportedOS.Desktop) != 0)
        {
            _eventPosition = GlobalPosition + mb.Position;
            if (mb.Pressed)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    // Does event happen inside joystick?
                    if (!_joystickStart && CenterOfJoystick.DistanceTo(_eventPosition) <= _radius)
                    {
                        _joystickStart = true;
                        EmitSignal(SignalName.Touch);
                        GetViewport().SetInputAsHandled();
                        OnDrag();
                    }
                }
            }
            else
            {
                if (mb.ButtonIndex == MouseButton.Left && _joystickStart)
                {
                    GetViewport().SetInputAsHandled();
                    OnRelease();
                }
            }
        }
        else if (ie is InputEventScreenTouch est && (_currentOS & SupportedOS.Mobile) != 0)
        {
            _eventPosition = GlobalPosition + est.Position;
            if (est.Pressed)
            {
                // Does event happen inside joystick?
                if (!_joystickStart && CenterOfJoystick.DistanceTo(_eventPosition) <= _radius)
                {
                    _joystickStart = true;
                    EmitSignal(SignalName.Touch);
                    _eventId = est.Index;
                    GetViewport().SetInputAsHandled();
                    OnDrag();
                }
            }
            else
            {
                if (est.Index == _eventId && _joystickStart)
                {
                    GetViewport().SetInputAsHandled();
                    OnRelease();
                }
            }
        }
        else if (ie is InputEventMouseMotion mm && _joystickStart && (_currentOS & SupportedOS.Desktop) != 0)
        {
            _eventPosition = GlobalPosition + mm.Position;
            GetViewport().SetInputAsHandled();
            OnDrag();
        }
        else if (ie is InputEventScreenDrag esd && _joystickStart && esd.Index == _eventId && (_currentOS & SupportedOS.Mobile) != 0)
        {
            _eventPosition = GlobalPosition + esd.Position;
            GetViewport().SetInputAsHandled();
            OnDrag();
        }
    }

    public override void _Draw()
    {
        if (_drawDeadzone && _isDeadzoneReached)
        {
            DrawArc(Size / 2, Deadzone, 0, 360, 360, new Color(1, 0, 0), 5, true);
        }
    }

    private void OnDrag()
    {
        if (CenterOfJoystick.DistanceTo(_eventPosition) <= _radius)
        {
            _direction = (CenterOfJoystick - _eventPosition) / _radius;
            switch (Type)
            {
                case JoystickType.Static:
                    break;
                case JoystickType.Fixed:
                case JoystickType.Free:
                    JoystickRing.GlobalPosition = _eventPosition - JoystickRing.Size / 2;
                    break;
                default:
                    break;
            }
        }
        else
        {
            _direction = (CenterOfJoystick - _eventPosition).Normalized();
            switch (Type)
            {
                case JoystickType.Static:
                    break;
                case JoystickType.Fixed:
                    JoystickRing.GlobalPosition = CenterOfJoystick - JoystickRing.Size / 2;
                    JoystickRing.GlobalPosition -= _direction * _radius;
                    break;
                case JoystickType.Free:
                    JoystickRing.GlobalPosition = _eventPosition - JoystickRing.Size / 2;
                    break;
                default:
                    break;
            }
        }

        if (0.0f < Deadzone)
        {
            if (Deadzone < (CenterOfJoystick - _eventPosition).Length())
            {
                _isDeadzoneReached = true;
                QueueRedraw();
            }
            else
            {
                _isDeadzoneReached = false;
                QueueRedraw();
            }
        }
        _direction = -_direction;
        EmitSignal(SignalName.Drag, _direction);
    }

    private void OnRelease()
    {
        EmitSignal(SignalName.Release, !_isDeadzoneReached);
        _isDeadzoneReached = false;
        QueueRedraw();
        _joystickStart = false;
        _eventId = -1;
        JoystickRing.GlobalPosition = CenterOfJoystick - JoystickRing.Size / 2;
    }
}
