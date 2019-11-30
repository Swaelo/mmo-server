// ================================================================================================================================
// File:        PlayerCommunicationPacketHandler.cs
// Description: Handles any client messages received regarding communication inside the game
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using System.Collections.Generic;
using Server.Logging;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class PlayerCommunicationPacketHandler
    {
        public static void HandleClientChatMessage(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn("Handle " + ClientID + " chat message");

            //Fetch this ClientConnection and make sure they were able to be found
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to handle chat message.");
                return;
            }

            //Extract the message content from the network packet
            string ChatMessage = Packet.ReadString();

            //Check if any commands can be executed from the message they sent
            ExecuteChatCommands(Client, ChatMessage);

            //Get the list of all the other game clients who are already ingame
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);

            //Pass this chat message on to all the other clients that are ingame
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerCommunicationPacketSender.SendChatMessage(OtherClient.NetworkID, Client.Character.Name, ChatMessage);
        }

        private static void ExecuteChatCommands(ClientConnection Client, string Message)
        {
            //Split each argument from one another
            string[] Split = Message.Split(" ");

            if (ExecuteTeleportCommand(Client, Split))
                MessageLog.Print("Teleporting player");
        }

        private static bool ExecuteTeleportCommand(ClientConnection Client, string[] Arguments)
        {
            //with just 1 argument they may be trying to teleport to the name of a location
            if(Arguments.Length == 2)
            {
                string Destination = Arguments[1].ToLower();

                if(Destination == "spawn")
                {
                    Vector3 Spawn = new Vector3(14.6838f, 0.5799f, 23.21202f);
                    Client.Character.Position = Spawn;
                    PlayerManagementPacketSender.SendTeleportLocalPlayer(Client.NetworkID, Spawn);
                    List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(Client.NetworkID);
                    foreach (ClientConnection OtherClient in OtherClients)
                        PlayerManagementPacketSender.SendTeleportRemotePlayer(OtherClient.NetworkID, Client.Character.Name, Spawn);
                    return true;
                }
            }

            return false;
        }
    }
}