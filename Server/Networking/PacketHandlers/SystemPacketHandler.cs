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
        public static void HandleMissedPacketAlert(int ClientID, ref NetworkPacket Packet)
        {
            //Find the client who sent this alert, and the number of the packet they want sent back to them
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            int MissingPacketNumber = Packet.ReadInt();
            Client.SendMissingPacket(MissingPacketNumber);
        }

        public static void HandleStillConnectedReply(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " Still Connected Reply");
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);

            //Update the timer tracking how long since we last heard from this client
            Client.LastCommunication = new Time.PointInTime();
        }
    }
}