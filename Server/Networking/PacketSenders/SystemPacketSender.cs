// ================================================================================================================================
// File:        SystemPacketSender.cs
// Description: Sends low level system messages to the game clients
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Logging;

namespace Server.Networking.PacketSenders
{
    public class SystemPacketSender
    {
        /// <summary>
        /// //Asks a client if they are still connected, requesting for them to immediately reply to us letting us know
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        public static void SendStillConnectedCheck(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " still alive request");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.StillConnectedCheck);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Sends a message to a client letting them know we have missed some packets and need them to be resent to us again
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="FirstMissedPacket">Number of the first packet you need resent, all the way to the last</param>
        public static void SendMissingPacketsRequest(int ClientID, int FirstMissedPacket)
        {
            CommunicationLog.LogOut(ClientID + " Missing Packets Request.");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.MissingPacketsRequest);
            Packet.WriteInt(FirstMissedPacket);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Sends a message to a client (immediately) letting them know they have been kicked from the game
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="Reason">Reason this client is being kicked from the server</param>
        public static void SendKickedFromServer(int ClientID, string Reason = "No reason given")
        {
            //Get the client who we are kicking from the server
            ClientConnection TargetClient = ConnectionManager.GetClientConnection(ClientID);

            //Make sure we were actually able to find this client
            if(TargetClient == null)
            {
                MessageLog.Print("ERROR: Cant kick client #" + ClientID + " as they could not be found in the connections");
                return;
            }

            //Log what is happening here
            MessageLog.Print("Kicking " + TargetClient.NetworkID + " from the server...");

            //Create a new packet to send letting them know they have been kicked
            NetworkPacket Packet = new NetworkPacket();

            //Give it the next packet number they are expecting, then write the identifying packet type
            Packet.WriteInt(TargetClient.GetNextOutgoingPacketNumber());
            Packet.WriteType(ServerPacketType.KickedFromServer);
            Packet.WriteString(Reason);

            //Send this packet off to them immediately to ensure its sent to them before we clean up their network connection ourselves
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}