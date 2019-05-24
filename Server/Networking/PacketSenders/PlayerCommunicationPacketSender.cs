// ================================================================================================================================
// File:        PlayerCommunicationPacketSender.cs
// Description: Formats and delivers network packets to game clients with messages sent from other players
// ================================================================================================================================

using System.Collections.Generic;
using Server.Interface;

namespace Server.Networking.PacketSenders
{
    public static class PlayerCommunicationPacketSender
    {
        //Sends instructions to a list of game clients to have someone elses chat message display in their chat window
        public static void SendListPlayerChatMessage(List<ClientConnection> Clients, string MessageSender, string MessageContent)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("PlayerCommunication.SendListPlayerChatMessage");

            //Loop through the list of clients and deliver these instructions to each of them
            foreach(ClientConnection Client in Clients)
            {
                //Fetch each clients QueueWriter and write the packet type into it
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.PlayerChatMessage);

                //Write the message contents into the packet data
                QueueWriter.WriteString(MessageSender);
                QueueWriter.WriteString(MessageContent);
            }
        }
    }
}
