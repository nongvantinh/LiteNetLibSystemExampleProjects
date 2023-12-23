using Godot;
using System;
using System.Net;
using System.Net.Sockets;

public enum SupportedOS
{
    Android = 1,
    Haiku = 1 << 1,
    iOS = 1 << 2,
    HTML5 = 1 << 3,
    OSX = 1 << 4,
    Server = 1 << 5,
    Windows = 1 << 6,
    UWP = 1 << 7,
    X11 = 1 << 8,
    Desktop = Windows | UWP | X11 | OSX,
    Mobile = Android | iOS,
    All = Desktop | Mobile | HTML5
}

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    public readonly SupportedOS CurrentOS = Enum.Parse<SupportedOS>(OS.GetName());
    public static Node3D World { get; set; }
    public static float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    public static bool IsPlaying = true;
    [Export] public bool IsServer = false;
    [Export] public string ServerIPAddress = GetLocalIPAddress();

    public GameManager()
    {
        Instance = this;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = IsPlaying ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        if (IsServer)
        {
            AddChild(GameServer.Instance);
        }
        else
        {
            AddChild(GameClient.Instance);
            GameClient.Instance.Connect(ServerIPAddress);
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    // TODO: GameManager.cs class manages various aspects of the game, and handle input from game play here is not correct,
    // consider move this method to a new class with the same is something like GamePlayManager instead.
    public override void _Input(InputEvent inputEvent)
    {
        if (Input.IsActionJustPressed("open_settings"))
        {
            IsPlaying = !IsPlaying;
            if ((CurrentOS & SupportedOS.Desktop) != 0)
            {
                Input.MouseMode = IsPlaying ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            }
        }
    }
}
