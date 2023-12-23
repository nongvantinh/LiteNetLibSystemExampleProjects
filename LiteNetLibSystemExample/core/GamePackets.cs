using Godot;
using LiteNetLib.Utils;
using System;

public enum PacketType : byte
{
    EntitySystem,
    Serialized
}

//Auto serializable packets
public class JoinPacket
{
    public string UserName { get; set; }
}

[Flags]
public enum MovementKeys : byte
{
    Left = 1,
    Right = 1 << 1,
    Up = 1 << 2,
    Down = 1 << 3,
    Fire = 1 << 4,
    Projectile = 1 << 5
}

public struct SkillPacket
{
    public Vector2 Direction;
}
public struct CameraModePacket : INetSerializable
{
    public CameraMode CameraMode;

    public void Deserialize(NetDataReader reader)
    {
        CameraMode = (CameraMode)reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CameraMode);
    }
}
