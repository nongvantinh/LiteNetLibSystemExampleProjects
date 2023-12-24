using Godot;
using System.Collections.Generic;

public partial class CharacterBody3DMoveable : CharacterBody3D, IMovable
{
    [Export] public Node3D Head { get; set; }
    [Export] public Node3D Pivot { get; set; }
    [Export] public Node3D MuzzlePosition { get; set; } // The muzzle's position
    [Export] public Node3D TargetAimingVisualizer { get; set; }
    [Export] public Node3D AttackRangeVisualizer { get; set; }
    [Export] public Node3D SkillShotVisualizer { get; set; }

    public BaseEntityPawn AttachedPlayer { get; set; }
    [Export] public MovementBehaviour Behaviour { get; set; }

    private List<BaseBehaviour3D> _behaviours = new List<BaseBehaviour3D>();

    public void RegisterBehavior(BaseBehaviour3D behaviour3D)
    {
        if (_behaviours.Contains(behaviour3D))
        {
            Debug.Log(behaviour3D.Name + " already added to this object");
            return;
        }

        _behaviours.Add(behaviour3D);

        if (!HasNode(behaviour3D.GetPath()))
        {
            AddChild(behaviour3D);
        }
    }

    public void Start(MovementBehaviour behaviour)
    {
        Behaviour = behaviour;
        Show();
    }

    public void Movement(UserInputData command, float delta)
    {
        Vector3 tempVelocity = Velocity;
        for (int i = 0; i != _behaviours.Count; ++i)
        {
            _behaviours[i].Apply(ref tempVelocity, ref command, (float)delta);
        }
        Velocity = tempVelocity;
        MoveAndSlide();
    }
}
