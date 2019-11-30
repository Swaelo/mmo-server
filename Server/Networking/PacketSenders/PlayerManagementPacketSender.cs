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
        public static void SendTeleportLocalPlayer(int ClientID, Vector3 Position)
        {
            CommunicationLog.LogOut(ClientID + " Teleport Local Player.");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.TeleportLocalPlayer);
            Packet.WriteVector3(Position);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        public static void SendTeleportRemotePlayer(int ClientID, string Name, Vector3 Position)
        {
            CommunicationLog.LogOut(ClientID + " Teleport Remote Player.");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.TeleportRemotePlayer);
            Packet.WriteString(Name);
            Packet.WriteVector3(Position);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Tells a client the updated character values for one of the other remote player characters in their game world
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="Name">Name of the character to be updated</param>
        /// <param name="Position">The characters new position</param>
        /// <param name="Movement">The characters new movement input</param>
        /// <param name="Rotation">The characters new rotation</param>
        public static void SendUpdateRemotePlayer(int ClientID, string Name, Vector3 Position, Vector3 Movement, Quaternion Rotation)
        {
            //Log what we are doing here
            CommunicationLog.LogOut(ClientID + " Remote Player Update.");

            //Create a new packet with the enum identifier
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.UpdateRemotePlayer);

            //Fill the rest of the packet data
            Packet.WriteString(Name);
            Packet.WriteVector3(Position);
            Packet.WriteVector3(Movement);
            Packet.WriteQuaternion(Rotation);

            //Queue the packet for network transmission
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
        public static void SendAddRemotePlayer(int ClientID, string CharacterName, Vector3 CharacterPosition, Vector3 CharacterMovement, Quaternion CharacterRotation)
        {
            CommunicationLog.LogOut(ClientID + " add other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.AddRemotePlayer);
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
        public static void SendRemoveRemotePlayer(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " remove other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.RemoveRemotePlayer);
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
            Packet.WriteType(ServerPacketType.AllowPlayerBegin);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        public static void SendPlayAnimationAlert(int ClientID, string CharacterName, string AnimationName)
        {
            CommunicationLog.LogOut(ClientID + " play animation alert");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.RemotePlayerPlayAnimationAlert);
            Packet.WriteString(CharacterName);
            Packet.WriteString(AnimationName);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}