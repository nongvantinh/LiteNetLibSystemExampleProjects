using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;

public partial class GameClient : Node, INetEventListener
{
    public static GameClient Instance => _lazy.Value;

    public BaseEntityPawn MyPlayer { get; private set; }

    private static readonly Lazy<GameClient> _lazy = new Lazy<GameClient>(() => new GameClient());
    private NetManager _netManager;
    private NetDataWriter _writer;
    private NetPacketProcessor _packetProcessor;
    private NetPeer _server;
    private ClientEntityManager _entityManager;
    private int _ping;
    private int PacketsInPerSecond;
    private int BytesInPerSecond;
    private int PacketsOutPerSecond;
    private int BytesOutPerSecond;
    private float _secondTimer;

    private GameClient()
    {
        LiteEntitySystem.Logger.LoggerImpl = Debug.Logger.Instance;
        NetDebug.Logger = Debug.Logger.Instance;
    }

    public override void _EnterTree()
    {
        EntityManager.RegisterFieldType<Vector2>((first, second, weight) => first.Lerp(second, weight));
        EntityManager.RegisterFieldType<Vector3>((first, second, weight) => first.Lerp(second, weight));
        //EntityManager.RegisterFieldType<UserMovementData>(UserMovementData.Lerp);
        _writer = new NetDataWriter();

        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            EnableStatistics = true,
            IPv6Enabled = false,
            SimulateLatency = true,
            SimulationMinLatency = 50,
            SimulationMaxLatency = 60,
            SimulatePacketLoss = false,
            SimulationPacketLossChance = 10
        };

        _netManager.Start();
    }

    public override void _ExitTree()
    {
        _netManager?.Stop();
        _netManager = null;
        _entityManager?.Reset();
        _entityManager = null;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        _netManager.PollEvents();
        _secondTimer += (float)delta;
        if (_secondTimer >= 1f)
        {
            _secondTimer -= 1f;
            var stats = _netManager.Statistics;
            BytesInPerSecond = (int)(stats.BytesReceived);
            PacketsInPerSecond = (int)(stats.PacketsReceived);
            BytesOutPerSecond = (int)(stats.BytesSent);
            PacketsOutPerSecond = (int)(stats.PacketsSent);
            stats.Reset();
        }

        if (_entityManager != null)
        {
            _entityManager.Update();
            GUIManager.Instance.DebugText.Text = $@"
C_ServerTick: {_entityManager.ServerTick}
C_Tick: {_entityManager.Tick}
C_LPRCS: {_entityManager.LastProcessedTick}
C_StoredCommands: {_entityManager.StoredCommands}
C_Entities: {_entityManager.EntitiesCount}
C_LerpBuffer: {_entityManager.LerpBufferCount}
Ping: {_ping}
IN: {BytesInPerSecond / 1000f} KB/s({PacketsInPerSecond})
OUT: {BytesOutPerSecond / 1000f} KB/s({PacketsOutPerSecond})";
        }
        else
        {
            GUIManager.Instance.DebugText.Text = "Disconnected";
        }
    }

    private void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
    {
        if (_server == null)
        {
            return;
        }

        _writer.Reset();
        _writer.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_writer, packet);
        _server.Send(_writer, deliveryMethod);
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[C] Connected to server: " + peer.EndPoint);
        _server = peer;

        SendPacket(new JoinPacket { UserName = "Nông Văn Tình" }, DeliveryMethod.ReliableOrdered);

        var typesMap = new EntityTypesMap<GameEntities>()
                            .Register(GameEntities.Player, e => new BaseEntityPawn(e))
                            .Register(GameEntities.PlayerController, e => new BaseEntityController(e));

        _entityManager = new ClientEntityManager(typesMap, new InputProcessor<UserCommandData>(),
            new LiteNetLibNetPeer(peer, true), (byte)PacketType.EntitySystem, NetworkGeneral.GameFPS);

        _entityManager.GetEntities<BaseEntityPawn>().SubscribeToConstructed(player =>
        {
            if (player.IsLocalControlled)
            {
                MyPlayer = player;
                Debug.Log($"IsLocalControlled");
            }
            else
            {
                Debug.Log($"remote controlled");
            }
        }, true);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        _entityManager?.Reset();
        _server = null;
        _entityManager = null;
        Debug.Log($"[C] Disconnected from server: {info.Reason}, {info.SocketErrorCode}, {info.AdditionalData}");
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.Log("[C] NetworkError: " + socketError);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        byte packetType = reader.PeekByte();
        var pt = (PacketType)packetType;
        switch (pt)
        {
            case PacketType.EntitySystem:
                _entityManager.Deserialize(reader.AsReadOnlySpan());
                break;
            case PacketType.Serialized:
                reader.GetByte();
                _packetProcessor.ReadAllPackets(reader);
                break;
            default:
                Debug.Log("Unhandled packet: " + pt);
                break;
        }
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        _ping = latency;
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject();
    }

    public void Connect(string ip)
    {
        _netManager.Connect(ip, NetworkGeneral.ServerPort, "ExampleGame");
    }
}
