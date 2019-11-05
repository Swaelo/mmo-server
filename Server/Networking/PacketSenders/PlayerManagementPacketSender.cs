// ================================================================================================================================
// File:        PlayerManagementPacketSender.cs
// Description: Sends packets to the game clients giving them updated information on the current status of other remote players
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;

namespace Server.Networking.PacketSenders
{
    public class PlayerManagementPacketSender
    {
        //Tells a client the updated position value of one of the other ingame player characters
        public static void SendPlayerUpdate(int ClientID, string CharacterName, Vector3 CharacterPosition)
        {
            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.PlayerUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to spawn a remote player character into their game world
        public static void SendAddOtherPlayer(int ClientID, string CharacterName, Vector3 CharacterPosition)
        {
            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.SpawnPlayer);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to remove a remote player character from their game world
        public static void SendRemoveOtherPlayer(int ClientID, string CharacterName)
        {
            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.RemovePlayer);
            Packet.WriteString(CharacterName);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client they have been added into the game world physics simulation and they may now start playing
        public static void SendPlayerBegin(int ClientID)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerBegin);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}