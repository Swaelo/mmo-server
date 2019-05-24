// ================================================================================================================================
// File:        PlayerCommunicationPacketHanlder.cs
// Description: Manages packets from game clients related to communication between players
// ================================================================================================================================

using System.Collections.Generic;
using Server.Interface;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class PlayerCommunicationPacketHandler
    {
        //Recieves a chat message from a player
        public static void HandlePlayerChatMessage(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": PlayerCommunication.PlayerChatMessage");

            //Get the message contents from the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string MessageSender = Reader.ReadString();
            string MessageContent = Reader.ReadString();
            //Now send this to everyone else
            List<ClientConnection> OtherClients = ConnectionManager.GetActiveClientsExceptFor(ClientID);
            PlayerCommunicationPacketSender.SendListPlayerChatMessage(OtherClients, MessageSender, MessageContent);
        }
    }
}
