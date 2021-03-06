﻿// ================================================================================================================================
// File:        PlayerCommunicationPacketSender.cs
// Description: Sends packets out to clients with the other players chat messages to be displayed in their chat logs
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Logging;

namespace Server.Networking.PacketSenders
{
    public static class PlayerCommunicationPacketSender
    {
        /// <summary>
        /// //Sends a chat message out to be displayed in a client chat window
        /// </summary>
        /// <param name="ClientID">NetworkID of target client</param>
        /// <param name="Sender">Name of character who sent the message</param>
        /// <param name="Message">Contents of the message that was sent</param>
        public static void SendChatMessage(int ClientID, string Sender, string Message)
        {
            CommunicationLog.LogOut(ClientID + " player chat message");

            //Create a new NetworkPacket to store the data for this chat message
            NetworkPacket Packet = new NetworkPacket();

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.PlayerChatMessage);
            Packet.WriteString(Sender);
            Packet.WriteString(Message);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}