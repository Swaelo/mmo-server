// ================================================================================================================================
// File:        PlayerCommunicationPacketHandler.cs
// Description: Handles any client messages received regarding communication inside the game
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Logging;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class PlayerCommunicationPacketHandler
    {
        public static void HandleClientChatMessage(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " chat message");

            //Fetch this ClientConnection and make sure they were able to be found
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Extract the message content from the network packet
            string ChatMessage = Packet.ReadString();

            //Get the list of all the other game clients who are already ingame
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);

            //Pass this chat message on to all the other clients that are ingame
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerCommunicationPacketSender.SendChatMessage(OtherClient.NetworkID, Client.CharacterName, ChatMessage);
        }
    }
}