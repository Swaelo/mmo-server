// ================================================================================================================================
// File:        MiscellaneousPacketHandler.cs
// Description: Handles alerts from the game clients like when they let us know they are still connected so we dont clean up their
//              connection and log them out / remove them from the game world etc.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Time;
using Server.Logging;

namespace Server.Networking.PacketHandlers
{
    public static class MiscellaneousPacketHandler
    {
        //Clients will let us know in every one of their communication intervals that they are still connected
        public static void HandleStillAliveAlert(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " still alive alert");

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Update the clients communication timer so we know how long since we last heard from them
            ClientConnection.LastCommunication = new PointInTime();
        }
    }
}