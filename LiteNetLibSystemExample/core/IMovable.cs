using Godot;

public enum MovementBehaviour
{
    Translate,
    Interpolate,
    Navigation,
}

public interface IMovable
{
    public Node3D Head { get; set; }
    public Node3D Pivot { get; set; }
    public Node3D MuzzlePosition { get; protected set; } // The muzzle's position
    public Node3D TargetAimingVisualizer { get; protected set; }
    public Node3D AttackRangeVisualizer { get; protected set; }
    public Node3D SkillShotVisualizer { get; protected set; }

    public BaseEntityPawn AttachedPlayer { get; internal set; }
    public MovementBehaviour Behaviour { get; set; }

    public Vector3 GlobalPosition { get; set; }
    public Vector3 GlobalRotation { get; set; }
    public Vector3 Velocity { get; set; }

    public bool IsVisibleInTree();
    public bool IsOnFloor();
    public void QueueFree();
    public void Start(MovementBehaviour behaviour = MovementBehaviour.Translate);
    public void RegisterBehavior(BaseBehaviour3D behaviour3D);
    public void Movement(UserInputData command, float delta);
}