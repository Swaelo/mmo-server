// ================================================================================================================================
// File:        NetworkingPacketHandler.cs
// Description: Handles recieving chat messages from connected game clients
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Interface;

namespace Server.Networking.WebSocketPacketHandlers
{
    public static class NetworkingPacketHandler
    {
        public static void HandleClientMessage(int ClientID, string PacketMessage)
        {
            //Isolate the author of the message from the content of the message itself
            string Author = PacketMessage.Substring(0, PacketMessage.IndexOf(' '));
            string Message = PacketMessage.Substring(PacketMessage.IndexOf(' '));
            Log.PrintDebugMessage(Author + ": " + Message);

            List<WebSocketClientConnection> OtherPlayers = WebSocketConnectionManager.GetAllOtherClients(ClientID);
            string ClientMessage = (int)ServerPacketType.PlayerChatMessage + " " + Author + " " + Message;
            foreach (WebSocketClientConnection OtherPlayer in OtherPlayers)
                OtherPlayer.SendPacket(ClientMessage);
        }
    }
}