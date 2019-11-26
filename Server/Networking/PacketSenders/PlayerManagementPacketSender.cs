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
        /// <summary>
        /// //Tells a client the updated position values of one of the other players characters
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character who's position is being updated</param>
        /// <param name="CharacterPosition">New position values for the character</param>
        public static void SendPlayerPositionUpdate(int ClientID, string CharacterName, Vector3 CharacterPosition)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Position Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerPositionUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Tells a client the updated rotation values of one of the other player characters
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character who's position is being updated</param>
        /// <param name="CharacterRotation">New rotation values for the character</param>
        public static void SendPlayerRotationUpdate(int ClientID, string CharacterName, Quaternion CharacterRotation)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Rotation Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerRotationUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteQuaternion(CharacterRotation);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Tells a client the updated movement values of one of the other player characters
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character who's movement values are being updated</param>
        /// <param name="CharacterRotation">New movement input values for the character</param>
        public static void SendPlayerMovementUpdate(int ClientID, string CharacterName, Vector3 CharacterMovement)
        {
            //Log what we are doing
            CommunicationLog.LogOut(ClientID + " Movement Update");
            //Fill a new NetworkPacket with all the necessary data
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerMovementUpdate);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterMovement);
            //Add this packet to the queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client to spawn a remote player character into their game world
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character to be spawned in</param>
        /// <param name="CharacterPosition">Where to spawn the character</param>
        /// <param name="CharacterMovement">Characters initial movement input values</param>
        /// <param name="CharacterRotation">Characters initial rotation values</param>
        public static void SendSpawnOtherPlayer(int ClientID, string CharacterName, Vector3 CharacterPosition, Vector3 CharacterMovement, Quaternion CharacterRotation)
        {
            CommunicationLog.LogOut(ClientID + " add other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.SpawnOtherPlayer);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(CharacterPosition);
            Packet.WriteVector3(CharacterMovement);
            Packet.WriteQuaternion(CharacterRotation);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client to remove a remote player character from their game world
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character to be removed</param>
        public static void SendRemoveOtherPlayer(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " remove other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.RemoveOtherPlayer);
            Packet.WriteString(CharacterName);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client they have been added into the game world physics simulation and they may now start playing
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        public static void SendPlayerBegin(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " player begin");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerBegin);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client to force move their character to a new location
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="NewLocation">Location where the clients character is to be moved to</param>
        public static void SendForceMovePlayer(int ClientID, Vector3 NewLocation)
        {
            CommunicationLog.LogOut(ClientID + " force move");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.ForceMovePlayer);
            Packet.WriteVector3(NewLocation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client to force move someone elses character to a new location
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character to be moved</param>
        /// <param name="NewLocation">Where the character is to be moved to</param>
        public static void SendForceMoveOtherPlayer(int ClientID, string CharacterName, Vector3 NewLocation)
        {
            CommunicationLog.LogOut(ClientID + " force move other");

            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.ForceMoveOtherPlayer);
            Packet.WriteString(CharacterName);
            Packet.WriteVector3(NewLocation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}