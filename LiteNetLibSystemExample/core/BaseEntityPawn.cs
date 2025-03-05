using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Extensions;
using System.Collections.Generic;

[EntityFlags(EntityFlags.UpdateOnClient)]
public partial class BaseEntityPawn : PawnLogic
{
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector3> _globalPosition;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector3> _globalRotation;
    private SyncVar<Vector2> _position;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<CameraMode> _currentCameraMode;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector3> _cameraRotation;
    [SyncVarFlags(SyncFlags.AlwaysRollback)]
    private SyncVar<byte> _health;
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<float> _speed;
    public readonly SyncString Name = new();
    private readonly SyncTimer _shootTimer = new(0.5f);
    private static RemoteCall<SkillPacket> _skillZeroRemoteCall;
    private static RemoteCall<SkillPacket> _skillOneRemoteCall;
    private static RemoteCall<SkillPacket> _skillTwoRemoteCall;
    private static RemoteCall<SkillPacket> _skillThreeRemoteCall;
    private RTSCamera _rtsCamera;
    private FirstPersonCamera _firstPersonCamera;
    private ThirdPersonCamera _thirdPersonCamera;
    public BaseCamera CurrentCamera;

    public IMovable Body;
    private UserInputData _commands = UserInputData.Identity;

    public Vector3 SyncedGlobalPosition => _globalPosition;
    public Vector3 SyncedGlobalRotation => _globalRotation;
    public Vector3 SyncedCameraRotation => _cameraRotation;
    public CameraMode SyncedCameraMode => _currentCameraMode;
    public float SyncedSpeed => _speed;

    private List<IMovable> _pool = new List<IMovable>();
    private const int POOL_SIZE = 100;

    public BaseEntityPawn(EntityParams entityParams) : base(entityParams)
    {
        _globalRotation.Value = Vector3.Zero;
        _cameraRotation.Value = Vector3.Zero;
        _speed.Value = 5.0f;
    }

    protected override void OnConstructed()
    {
        Debug.Log("Contructed");

        Body = SceneManager.Instance.CharacterScene.Instantiate<IMovable>();
        Body.AttachedPlayer = this;
        GameManager.World.AddChild((Node)Body);

        _rtsCamera = SceneManager.Instance._rtsCameraScene.Instantiate<RTSCamera>();
        GameManager.World.AddChild(_rtsCamera);
        _rtsCamera.Pivot = (Node3D)Body;
        _rtsCamera.Target = (Node3D)Body;

        _firstPersonCamera = SceneManager.Instance._firstPersonCameraScene.Instantiate<FirstPersonCamera>();
        GameManager.World.AddChild(_firstPersonCamera);
        _firstPersonCamera.Pivot = Body.Head;
        _firstPersonCamera.Target = (Node3D)Body;

        _thirdPersonCamera = SceneManager.Instance._thirdPersonCameraScene.Instantiate<ThirdPersonCamera>();
        GameManager.World.AddChild(_thirdPersonCamera);
        _thirdPersonCamera.Pivot = (Node3D)Body;
        _thirdPersonCamera.Target = (Node3D)Body;

        OnSwitchCameraMode(_currentCameraMode);
        //PackedScene bulletScene = ResourceLoader.Load("res://scenes/bullet.tscn") as PackedScene;
        //PackedScene ballScene = ResourceLoader.Load("res://scenes/ball.tscn") as PackedScene;

        // for (int i = 0; i < POOL_SIZE; ++i)
        // {
        //     CharacterBody3DMoveable bullet = bulletScene.Instantiate() as CharacterBody3DMoveable;
        //     Area3DMoveable ball = ballScene.Instantiate() as Area3DMoveable;
        //     L_Main.Instance.AddChild(bullet);
        //     L_Main.Instance.AddChild(ball);

        //     _pool.Add(bullet);
        //     _pool.Add(ball);
        // }

    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Body?.QueueFree();
        _rtsCamera?.QueueFree();
        _firstPersonCamera?.QueueFree();
        _thirdPersonCamera?.QueueFree();
    }

    protected override void Update()
    {
        base.Update();
        if (EntityManager.IsClient)
        {
            Body.GlobalPosition = SyncedGlobalPosition;
            Body.GlobalRotation = SyncedGlobalRotation;
            OnSwitchCameraMode(_currentCameraMode);
            CurrentCamera.CameraRotation = SyncedCameraRotation;

            if (IsRemoteControlled)
            {
                Debug.Log("Sync global position: " + SyncedGlobalPosition);
                return;
            }
        }

        CurrentCamera.Rotate(_commands.RotationVector);

        UpdateRotation(_commands.Direction);
        Body.Movement(_commands, EntityManager.DeltaTimeF);

        _globalPosition.Value = Body.GlobalPosition;
        _globalRotation.Value = Body.GlobalRotation;
        _cameraRotation.Value = CurrentCamera.CameraRotation;
    }

    public void SetInput(UserInputData command)
    {
        _commands = command;
    }
    
    protected override void RegisterRPC(ref RPCRegistrator r)
    {
        base.RegisterRPC(ref r);
        r.CreateRPCAction(this, OnSkillZeroExecuted, ref _skillZeroRemoteCall, ExecuteFlags.ExecuteOnPrediction | ExecuteFlags.SendToOther);
        r.CreateRPCAction(this, OnSkillOneExecuted, ref _skillOneRemoteCall, ExecuteFlags.ExecuteOnPrediction | ExecuteFlags.SendToOther);
        r.CreateRPCAction(this, OnSkillTwoExecuted, ref _skillTwoRemoteCall, ExecuteFlags.ExecuteOnPrediction | ExecuteFlags.SendToOther);
        r.CreateRPCAction(this, OnSkillThreeExecuted, ref _skillThreeRemoteCall, ExecuteFlags.ExecuteOnPrediction | ExecuteFlags.SendToOther);
    }
    

    public void OnSwitchCameraMode(CameraMode mode)
    {
        _currentCameraMode.Value = mode;
        switch (_currentCameraMode.Value)
        {
            case CameraMode.RTS:
                CurrentCamera = _rtsCamera;
                break;
            case CameraMode.FirstPerson:
                CurrentCamera = _firstPersonCamera;
                break;
            case CameraMode.ThirdPerson:
                CurrentCamera = _thirdPersonCamera;
                break;
        }

        if (IsLocalControlled || EntityManager.IsServer)
        {
            CurrentCamera.Current = true;
        }
    }


    private void UpdateRotation(Vector3 direction)
    {
        switch (_currentCameraMode.Value)
        {
            case CameraMode.RTS:
            case CameraMode.ThirdPerson:
                if (direction != Vector3.Zero)
                {
                    Body.GlobalRotation = new Vector3(Body.GlobalRotation.X, -Mathf.Atan2(direction.X, -direction.Z), Body.GlobalRotation.Z);
                }
                break;
            case CameraMode.FirstPerson:
                Body.GlobalRotation = new Vector3(Body.GlobalRotation.X, CurrentCamera.HorizontalRotation, Body.GlobalRotation.Z);
                break;
        }
    }

    public void Shoot(Node3D target)
    {
        IMovable[] movable = new IMovable[2];
        foreach (var obj in _pool)
        {
            if (!obj.IsVisibleInTree())
            {
                if (movable[0] == null)
                {
                    movable[0] = obj;
                }
                else if (movable[1] == null)
                {
                    movable[1] = obj;
                    break;
                }
            }
        }
        // Range indicator.
        float range = fts.ballistic_range(_speed, GameManager.Gravity, Body.MuzzlePosition.GlobalPosition.Y);

        Vector3[] solutions = new Vector3[2];
        int numSolutions = 0;
        if (target is IMovable movableObject)
        {
            if (movableObject.Velocity.LengthSquared() > 0)
            {
                numSolutions = fts.solve_ballistic_arc(Body.MuzzlePosition.GlobalPosition, _speed, target.GlobalPosition, movableObject.Velocity, GameManager.Gravity, out solutions[0], out solutions[1]);
            }
        }
        else
        {
            numSolutions = fts.solve_ballistic_arc(Body.MuzzlePosition.GlobalPosition, _speed, target.GlobalPosition, GameManager.Gravity, out solutions[0], out solutions[1]);
        }

        if (numSolutions > 0)
        {
            int index = 0;
            movable[0].Velocity = solutions[index];
            movable[0].Start();
            movable[0].GlobalPosition = Body.MuzzlePosition.GlobalPosition;
            ++index;
            movable[1].Velocity = solutions[index];
            movable[1].Start();
            movable[1].GlobalPosition = Body.MuzzlePosition.GlobalPosition;
        }
        else
        {
            Debug.Log("There is no solution");
        }

    }

    public void ExecuteSkillZero(Vector2 direction)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: {EntityManager.InNormalState}");
        if (EntityManager.InNormalState)
        {
            ExecuteRPC(_skillZeroRemoteCall, new SkillPacket
            {
                Direction = direction
            });
        }
    }

    public void ExecuteSkillOne(Vector2 direction)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: {EntityManager.InNormalState}");
        if (EntityManager.InNormalState)
        {
            ExecuteRPC(_skillOneRemoteCall, new SkillPacket
            {
                Direction = direction
            });
        }
    }

    public void ExecuteSkillTwo(Vector2 direction)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: {EntityManager.InNormalState}");
        if (EntityManager.InNormalState)
        {
            ExecuteRPC(_skillTwoRemoteCall, new SkillPacket
            {
                Direction = direction
            });
        }
    }

    public void ExecuteSkillThree(Vector2 direction)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: {EntityManager.InNormalState}");
        if (EntityManager.InNormalState)
        {
            ExecuteRPC(_skillThreeRemoteCall, new SkillPacket
            {
                Direction = direction
            });
        }
    }

    public void OnSkillZeroExecuted(SkillPacket skillPacket)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}(Direction: {skillPacket.Direction})");
    }

    private void OnSkillOneExecuted(SkillPacket skillPacket)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}(Direction: {skillPacket.Direction})");
    }

    private void OnSkillTwoExecuted(SkillPacket skillPacket)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}(Direction: {skillPacket.Direction})");
    }

    private void OnSkillThreeExecuted(SkillPacket skillPacket)
    {
        Debug.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}(Direction: {skillPacket.Direction})");
    }
}
