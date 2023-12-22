using Godot;
using LiteEntitySystem;
using LiteNetLib;
using System.Collections.Generic;

public class BaseEntityPawn : PawnLogic
{
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector3> _globalPosition;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector3> _globalRotation;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    public SyncVar<Vector3> _cameraRotation = Vector3.Zero;

    public ThirdPersonCamera _thirdPersonCamera;
    private IMovable _body;
    private Queue<UserCommandData> _commands = new Queue<UserCommandData>();

    public Vector3 GlobalPosition => _globalPosition;
    public Vector3 GlobalRotation => _globalRotation;

    public BaseEntityPawn(EntityParams entityParams) : base(entityParams)
    {
    }

    protected override void OnConstructed()
    {
        Debug.Log("Contructed");

        _body = GameManager.Instance.CharacterScene.Instantiate<IMovable>();
        _body.AttachedPlayer = this;
        GameManager.World.AddChild((Node)_body);

        _thirdPersonCamera = GameManager.Instance._thirdPersonCameraScene.Instantiate<ThirdPersonCamera>();
        GameManager.World.AddChild(_thirdPersonCamera);
        _thirdPersonCamera.Pivot = (Node3D)_body;
        _thirdPersonCamera.Target = (Node3D)_body;

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log("OnDestroy");
        ((Node)_body)?.QueueFree();
        _thirdPersonCamera?.QueueFree();
    }

    public override void VisualUpdate()
    {
        _body.GlobalPosition = GlobalPosition;
        _body.GlobalRotation = GlobalRotation;
        _thirdPersonCamera.CameraRotation = _cameraRotation;
    }

    public override void Update()
    {
        base.Update();

        while (_commands.Count > 0)
        {
            UserCommandData command = _commands.Dequeue();
            _thirdPersonCamera.Rotate(command.RotationVector);
            _body.Movement(command, EntityManager.DeltaTimeF);
            if (EntityManager.IsServer)
            {
                _cameraRotation = _thirdPersonCamera.CameraRotation;
                _globalPosition = _body.GlobalPosition;
                _globalRotation = _body.GlobalRotation;
            }
        }
    }

    public void SetInput(UserCommandData command)
    {
        _commands.Enqueue(command);
    }
}
