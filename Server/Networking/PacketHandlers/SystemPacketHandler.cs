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
        //Retrives values for an account login request
        public static NetworkPacket GetValuesMissedPacketsRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.MissedPacketsRequest);
            Packet.WriteInt(ReadFrom.ReadInt());
            return Packet;
        }

        //Handle alert from client letting us know they have missed some packets and need to be resent
        public static void HandleMissedPacketsRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Log what is happening here
            CommunicationLog.LogIn(ClientID + " Missed Packets Request.");

            //Find the client who sent this alert, and the number of the packet they want sent back to them
            ClientConnection Client = ConnectionManager.GetClient(ClientID);
            if(Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle missing packets request.");
                return;
            }


            //Flag the client as needing to have a bunch of missing packets resent back to it again
            Client.PacketsToResend = true;
            Client.ResendFrom = Packet.ReadInt();
            MessageLog.Print("Client requested missing packets starting from packet #" + Client.ResendFrom);
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesStillConnectedReply(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.StillConnectedReply);
            return Packet;
        }

        //Handle reply from client letting us know they are still connected through the network
        public static void HandleStillConnectedReply(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " Still Connected Reply");
            ClientConnection Client = ConnectionManager.GetClient(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle still connected reply.");
                return;
            }

            //Update the timer tracking how long since we last heard from this client
            Client.LastCommunication = new Time.PointInTime();
        }
    }
}