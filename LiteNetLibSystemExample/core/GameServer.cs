using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;

public partial class GameServer : Node, INetEventListener
{
    public ushort Tick => _serverEntityManager.Tick;
    public static GameServer Instance => _lazy.Value;

    private static readonly Lazy<GameServer> _lazy = new Lazy<GameServer>(() => new GameServer());
    private NetManager _netManager;
    private NetPacketProcessor _packetProcessor;
    private ServerEntityManager _serverEntityManager;

    private GameServer()
    {
        LiteEntitySystem.Logger.LoggerImpl = Debug.Logger.Instance;
        NetDebug.Logger = Debug.Logger.Instance;
    }

    public override void _EnterTree()
    {
        EntityManager.RegisterFieldType<Vector2>((first, second, weight) => first.Lerp(second, weight));
        EntityManager.RegisterFieldType<Vector3>((first, second, weight) => first.Lerp(second, weight));
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            PacketPoolSize = 1000,
            SimulateLatency = true,
            SimulationMinLatency = 50,
            SimulationMaxLatency = 60,
            SimulatePacketLoss = false,
            SimulationPacketLossChance = 10
        };

        _packetProcessor = new NetPacketProcessor();
        _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);

        var typesMap = new EntityTypesMap<GameEntities>()
                            .Register(GameEntities.Player, e => new BaseEntityPawn(e))
                            .Register(GameEntities.PlayerController, e => new BaseEntityController(e));

        _serverEntityManager = new ServerEntityManager(typesMap, new InputProcessor<UserCommandData>(),
                                    (byte)PacketType.EntitySystem, NetworkGeneral.GameFPS, ServerSendRate.EqualToFPS);


        _netManager.Start(NetworkGeneral.ServerPort);
        Debug.Log($"Server is listening on port: {NetworkGeneral.ServerPort}");
    }

    public override void _ExitTree()
    {
        _netManager?.Stop();
        _netManager = null;
        _serverEntityManager.Reset();
        _serverEntityManager = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        _netManager.PollEvents();
        _serverEntityManager?.Update();
    }

    private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
    {
        Debug.Log("[S] Join packet received: " + joinPacket.UserName);

        var serverPlayer = _serverEntityManager.AddPlayer(new LiteNetLibNetPeer(peer, true));

        var player = _serverEntityManager.AddEntity<BaseEntityPawn>();
        _serverEntityManager.AddController<BaseEntityController>(serverPlayer, e => e.StartControl(player));
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[S] Player connected: " + peer.EndPoint);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

        if (peer.Tag != null)
        {
            _serverEntityManager.RemovePlayer((LiteNetLibNetPeer)peer.Tag);
        }
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.Log("[S] NetworkError: " + socketError);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        byte packetType = reader.PeekByte();
        switch ((PacketType)packetType)
        {
            case PacketType.EntitySystem:
                _serverEntityManager.Deserialize((LiteNetLibNetPeer)peer.Tag, reader.AsReadOnlySpan());
                break;
            case PacketType.Serialized:
                reader.GetByte();
                _packetProcessor.ReadAllPackets(reader, peer);
                break;
            default:
                Debug.Log("Unhandled packet: " + packetType);
                break;
        }
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        //Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        //Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("ExampleGame");
    }
}
