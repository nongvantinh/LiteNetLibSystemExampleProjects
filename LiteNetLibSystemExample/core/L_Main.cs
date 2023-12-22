using Godot;

public partial class L_Main : Node3D
{
    [Export] public Node3D TargetNode; // The target's position
    [Export] private Node3D _startPosition;

    public static L_Main Instance { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        GameManager.World = this;
    }
}
