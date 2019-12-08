// ================================================================================================================================
// File:        CombatPacketSenders.cs
// Description: Formats and delivers network packets to game clients while they are performing combat actions
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using Server.Logging;
using Server.Data;

namespace Server.Networking.PacketSenders
{
    public static class CombatPacketSenders
    {
        //Tells a client to update their players character with a new HP value
        public static void SendLocalPlayerTakeHit(int ClientID, int NewHPValue)
        {
            CommunicationLog.LogOut(ClientID + " local player take hit alert");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.LocalPlayerTakeHit);
            Packet.WriteInt(NewHPValue);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to update some remote players character with a new HP value
        public static void SendRemotePlayerTakeHit(int ClientID, string CharacterName, int NewHPValue)
        {
            CommunicationLog.LogOut(ClientID + " remote player take hit alert");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.RemotePlayerTakeHit);
            Packet.WriteString(CharacterName);
            Packet.WriteInt(NewHPValue);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client their character is now dead so it turns into a ragdoll and they lose control
        public static void SendLocalPlayerDead(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " Local Player Dead");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.LocalPlayerDead);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to update some remote players character as being dead, so it turn into a ragdoll
        public static void SendRemotePlayerDead(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " Remote Player Dead");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.RemotePlayerDead);
            Packet.WriteString(CharacterName);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to respawn their character back into the game world with all these values
        public static void SendLocalPlayerRespawn(int ClientID, CharacterData Data)
        {
            CommunicationLog.LogOut(ClientID + " Local Player Respawn");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.LocalPlayerRespawn);
            Packet.WriteVector3(Data.Position);
            Packet.WriteQuaternion(Data.Rotation);
            Packet.WriteFloat(Data.CameraZoom);
            Packet.WriteFloat(Data.CameraXRotation);
            Packet.WriteFloat(Data.CameraYRotation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to respawn some other remote players character into the game world with all these values
        public static void SendRemotePlayerRespawn(int ClientID, CharacterData Data)
        {
            CommunicationLog.LogOut(ClientID + " Remote Player Respawn");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.RemotePlayerRespawn);
            Packet.WriteString(Data.Name);
            Packet.WriteVector3(Data.Position);
            Packet.WriteQuaternion(Data.Rotation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}
