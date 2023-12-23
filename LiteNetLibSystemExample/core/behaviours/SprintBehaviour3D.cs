using Godot;

public partial class SprintBehaviour3D : BaseMovementBehaviour3D
{
    [Export(PropertyHint.Range, "0,3")]
    private float _speedIncreasePercentage = 2.5f;
    /// <summary>
    /// Time for the character to reach full speed.
    /// </summary>
    [Export]
    public float _acceleration = 10.5f;
    /// <summary>
    /// Time for the character to stop walking.
    /// </summary>
    [Export]
    public float _deceleration = 14.0f;
    /// <summary>
    /// Sets control in the air.
    /// </summary>
    [Export(PropertyHint.Range, "1,0.05")]
    public float _airControl = 0.3f;
    /// <summary>
    /// Takes direction of movement from input and turns it into horizontal velocity.
    /// </summary>
    /// <param name="velocity"></param>
    /// <param name="speed"></param>
    /// <param name="isOnFloor"></param>
    /// <param name="direction"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    public override void Apply(ref Vector3 velocity, ref UserInputData inputData, float delta)
    {
        base.Apply(ref velocity, ref inputData, delta);
        if (!Active)
        {
            return;
        }

        if ((inputData.Actions & UserInputAction.Sprint) != 0)
        {
            velocity.X += velocity.X * _speedIncreasePercentage / 100;
            velocity.Z += velocity.Z * _speedIncreasePercentage / 100;
        }
    }

}