using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class RigidBody3DMoveable : RigidBody3D, IMovable
{
    [Export] public Node3D Head { get; set; }
    [Export] public Node3D Pivot { get; set; }
    [Export] public Node3D MuzzlePosition { get; set; } // The muzzle's position
    [Export] public Node3D TargetAimingVisualizer { get; set; }
    [Export] public Node3D AttackRangeVisualizer { get; set; }
    [Export] public Node3D SkillShotVisualizer { get; set; }

    public BaseEntityPawn AttachedPlayer { get; set; }
    [Export] public MovementBehaviour Behaviour { get; set; }
    public Vector3 Velocity { get; set; }

    private List<BaseBehaviour3D> _behaviours = new List<BaseBehaviour3D>();
    private bool _isOnFloor = false;
    [Export] private RayCast3D _feet;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        LinearDamp = 1.0f;
    }

    public override void _Process(double delta)
    {
        if (AttachedPlayer.EntityManager.IsClient && AttachedPlayer.IsRemoteControlled)
        {
            GlobalPosition = AttachedPlayer.SyncedGlobalPosition;
            GlobalRotation = AttachedPlayer.SyncedGlobalRotation;
            if (AttachedPlayer.CurrentCamera != null)
            {
                AttachedPlayer.CurrentCamera.CameraRotation = AttachedPlayer.SyncedCameraRotation;
            }
        }
    }

    public void Movement(UserInputData command, float delta)
    {
        Vector3 tempVelocity = Velocity;
        for (int i = 0; i != _behaviours.Count; ++i)
        {
            _behaviours[i].Apply(ref tempVelocity, ref command, (float)delta);
        }
        Velocity = tempVelocity;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
    }


    public void Start(MovementBehaviour behaviour)
    {
        Behaviour = behaviour;
    }

    public bool IsOnFloor()
    {
        return _isOnFloor;
    }

    public void RegisterBehavior(BaseBehaviour3D behaviour3D)
    {
        _behaviours.Append(behaviour3D);
        AddChild(behaviour3D);
    }
}
