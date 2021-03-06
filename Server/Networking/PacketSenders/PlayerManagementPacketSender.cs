﻿// ================================================================================================================================
// File:        PlayerManagementPacketSender.cs
// Description: Sends packets to the game clients giving them updated information on the current status of other remote players
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using Server.Enums;
using Server.Logging;
using Server.Data;

namespace Server.Networking.PacketSenders
{
    public class PlayerManagementPacketSender
    {
        public static void SendPlayerPositionUpdate(int ClientID, CharacterData Character)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerPositionUpdate);
            Packet.WriteString(Character.Name);
            Packet.WriteVector3(Character.Position);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
        public static void SendPlayerRotationUpdate(int ClientID, CharacterData Character)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.PlayerRotationUpdate);
            Packet.WriteString(Character.Name);
            Packet.WriteQuaternion(Character.Rotation);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client to spawn a remote player character into their game world
        public static void SendAddRemotePlayer(int ClientID, CharacterData Character)
        {
            //Log what we are doing here
            CommunicationLog.LogOut(ClientID + " Add Remote Player.");
            //Create a new NetworkPacket filled with all the nessacery character data values
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.AddPlayer);
            Packet.WriteString(Character.Name);
            Packet.WriteBool(Character.IsAlive);
            Packet.WriteVector3(Character.Position);
            Packet.WriteQuaternion(Character.Rotation);
            Packet.WriteInt(Character.CurrentHealth);
            Packet.WriteInt(Character.MaxHealth);
            //Queue the packet for network transmission
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client to remove a remote player character from their game world
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="CharacterName">Name of the character to be removed</param>
        public static void SendRemoveRemotePlayer(int ClientID, string CharacterName, bool IsAlive)
        {
            CommunicationLog.LogOut(ClientID + " remove other player");

            //Create a new NetworkPacket object to store all the data inside
            NetworkPacket Packet = new NetworkPacket();

            //Write all the relevant data into the network packet
            Packet.WriteType(ServerPacketType.RemovePlayer);
            Packet.WriteString(CharacterName);
            Packet.WriteBool(IsAlive);

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
            Packet.WriteType(ServerPacketType.PlayAnimationAlert);
            Packet.WriteString(CharacterName);
            Packet.WriteString(AnimationName);
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}