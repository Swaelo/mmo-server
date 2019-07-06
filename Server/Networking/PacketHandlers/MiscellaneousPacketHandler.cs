// ================================================================================================================================
// File:        MiscellaneousPacketHandler.cs
// Description: Handles alerts from the game clients like when they let us know they are still connected so we dont clean up their
//              connection and log them out / remove them from the game world etc.
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Time;

namespace Server.Networking.PacketHandlers
{
    public static class MiscellaneousPacketHandler
    {
        //Clients will let us know in every one of their communication intervals that they are still connected
        public static void HandleStillAliveAlert(int ClientID, ref NetworkPacket Packet)
        {
            //Simply update this clients LastCommunication timer so we can accurately track how long since we last heard from them
            ConnectionManager.ActiveConnections[ClientID].LastCommunication = new PointInTime();
        }
    }
}