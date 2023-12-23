using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Area3DMoveable : Area3D, IMovable
{
    [Export] public Node3D Head { get; set; }
    [Export] public Node3D Pivot { get; set; }

    [ExportGroup("Shooting")]
    [Export] public Node3D MuzzlePosition { get; set; } // The muzzle's position
    [Export] public Node3D TargetAimingVisualizer { get; set; }
    [Export] public Node3D AttackRangeVisualizer { get; set; }
    [Export] public Node3D SkillShotVisualizer { get; set; }

    public BaseEntityPawn AttachedPlayer { get; set; }
    [Export] public MovementBehaviour Behaviour { get; set; }
    public Vector3 Velocity { get; set; }

    private List<BaseBehaviour3D> _behaviours = new List<BaseBehaviour3D>();
    [Export] public string[] TargetGroup;
    public bool IsObjectOutOfBounds { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
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

    void IMovable.Movement(UserInputData command, float delta)
    {
        Vector3 tempVelocity = Velocity;
        for (int i = 0; i != _behaviours.Count; ++i)
        {
            _behaviours[i].Apply(ref tempVelocity, ref command, delta);
        }
        Velocity = tempVelocity;
    }

    public void Start(MovementBehaviour behaviour)
    {
        Behaviour = behaviour;
    }

    private void OnAreaEntered(Area3D area)
    {
        foreach (string group in TargetGroup)
        {
            if (area.IsInGroup(group))
            {
                IsObjectOutOfBounds = true;
            }
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        foreach (string group in TargetGroup)
        {
            if (body.IsInGroup(group))
            {
                IsObjectOutOfBounds = true;
            }
        }
    }

    public bool IsOnFloor()
    {
        throw new NotImplementedException();
    }

    public void RegisterBehavior(BaseBehaviour3D behaviour3D)
    {
        _behaviours.Append(behaviour3D);
        AddChild(behaviour3D);
    }

    bool IMovable.IsVisibleInTree()
    {
        throw new NotImplementedException();
    }

    bool IMovable.IsOnFloor()
    {
        throw new NotImplementedException();
    }

    void IMovable.Start(MovementBehaviour behaviour)
    {
        throw new NotImplementedException();
    }

    void IMovable.RegisterBehavior(BaseBehaviour3D behaviour3D)
    {
        throw new NotImplementedException();
    }

}
