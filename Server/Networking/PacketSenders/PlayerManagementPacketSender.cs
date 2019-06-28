// ================================================================================================================================
// File:        PlayerManagementPacketSender.cs
// Description: Formats and delivers network packets to game clients regarding the state of all players currently in the game world
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using Server.Interface;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking.PacketSenders
{
    public static class PlayerManagementPacketSender
    {
        //Sends instructions to a list of clients to update the status of one of the other players ingame characters
        public static void SendListPlayerUpdate(List<ClientConnection> Clients, string CharacterName, Vector3 CharacterPosition, Quaternion CharacterRotation)
        {
            //Log a message to the display window
            //Log.PrintOutgoingPacketMessage("PlayerManagement.SendListPlayerUpdate");

            //Loop through the list of clients which need to have this information delivered to them
            foreach (ClientConnection Client in Clients)
            {
                //Fetch each clients packet writer and write the packet type into it
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.PlayerUpdate);

                //Write the characters updated information into the packet data
                QueueWriter.WriteString(CharacterName);
                QueueWriter.WriteVector3(CharacterPosition);
                QueueWriter.WriteQuaternion(CharacterRotation);
            }
        }

        //Sends a message to a list of clients to spawn someone elses character into their game world
        public static void SendListSpawnOtherCharacter(List<ClientConnection> Clients, string CharacterName, Vector3 CharacterLocation)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("PlayerManagement.SendListSpawnOtherCharacter");

            //Loop through the list of clients and deliver this message to them all
            foreach (ClientConnection Client in Clients)
            {
                //Fetch each clients QueueWriter and write in the packet type
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.SpawnPlayer);

                //Write the characters information into the packet data
                QueueWriter.WriteString(CharacterName);
                QueueWriter.WriteVector3(CharacterLocation);
            }
        }

        //Sends a message to a whole list of clients to remove some other players character from their game world
        public static void SendListRemoveOtherCharacter(List<ClientConnection> Clients, string CharacterName)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("PlayerManagement.SendListRemoveOtherCharacter");

            //Loop through the list of clients and deliver this message to all of them
            foreach(ClientConnection Client in Clients)
            {
                //Fetch the clients QueueWriter and write the packet type into it
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.RemovePlayer);

                //Write the characters name who is to be removed from the clients game world
                QueueWriter.WriteString(CharacterName);
            }
        }
    }
}
