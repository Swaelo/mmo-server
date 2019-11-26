// ================================================================================================================================
// File:        PlayerManagementPacketSender.cs
// Description: Sends packets to the game clients giving them updated information on the current status of other remote players
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using Server.Enums;
using Server.Logging;

namespace Server.Networking.PacketSenders
{
    public class PlayerManagementPacketSender
    {
        //Tells a client the updated Position/Rotation/Movement values of one of the other players characters
        public static void SendPlayerPosition(int ClientID, string CharacterName, Vector3 CharacterPosition)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Position Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.CharacterPositionUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }
        public static void SendPlayerRotation(int ClientID, string CharacterName, Quaternion CharacterRotation)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Rotation Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.CharacterRotationUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteQuaternion(CharacterRotation);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }
        public static void SendPlayerMovement(int ClientID, string CharacterName, Vector3 CharacterMovement)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Movement Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.CharacterMovementUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterMovement);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to spawn a remote player character into their game world
        public static void SendAddOtherPlayer(int ClientID, string CharacterName, Vector3 CharacterPosition, Quaternion CharacterRotation)
        {
            CommunicationLog.LogOut(ClientID + " add other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.SpawnPlayer);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);
            Packet.WriteQuaternion(CharacterRotation);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to remove a remote player character from their game world
        public static void SendRemoveOtherPlayer(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " remove other player");

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
            CommunicationLog.LogOut(ClientID + " player begin");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerBegin);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to force move their character to a new location
        public static void SendForceMovePlayer(int ClientID, Vector3 NewLocation)
        {
            CommunicationLog.LogOut(ClientID + " force move");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.ForceCharacterMove);
            Packet.WriteVector3(NewLocation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to force move someone elses character to a new location
        public static void SendForceMoveOtherPlayer(int ClientID, string CharacterName, Vector3 NewLocation)
        {
            CommunicationLog.LogOut(ClientID + " force move other");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.ForceOtherCharacterMove);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(NewLocation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}