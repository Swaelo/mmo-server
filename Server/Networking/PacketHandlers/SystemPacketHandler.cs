// ================================================================================================================================
// File:        SystemPacketHandler.cs
// Description: Handles low level system messages sent from the game clients
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Logging;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class SystemPacketHandler
    {
        //Handle alert from client letting us know they have missed some packets and need to be resent
        public static void HandleMissedPacketsRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Find the client who sent this alert, and the number of the packet they want sent back to them
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle missing packets request.");
                return;
            }

            int MissingPacketNumber = Packet.ReadInt();
            Client.SendMissingPacket(Client, MissingPacketNumber);
        }

        //Handle reply from client letting us know they are still connected through the network
        public static void HandleStillConnectedReply(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " Still Connected Reply");
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle still connected reply.");
                return;
            }

            //Update the timer tracking how long since we last heard from this client
            Client.LastCommunication = new Time.PointInTime();
        }

        //Handle alert from server letting us know they are out of sync and need to be removed from the game
        public static void HandleOutOfSyncAlert(int ClientID, ref NetworkPacket Packet)
        {
            //Get the client who is being removed from the game
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle out of sync alert.");
                return;
            }

            //Log a message showing that this client has lost sync and is going to be removed from the game
            MessageLog.Print(ClientID + " has gone too far out of sync with the server and needs to be removed from the game.");

            //Kick this player from the server, letting them know why they have been kicked
            SystemPacketSender.SendKickedFromServer(ClientID, "Gone too far out of sync with the server, log in again to continue playing.");

            //Tell all the other clients to remove this character from their game worlds
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendRemoveRemotePlayer(OtherClient.NetworkID, Client.CharacterName);

            //Set the client as dead to they get cleaned up and have their data saved into the database
            Client.ClientDead = true;
        }
    }
}