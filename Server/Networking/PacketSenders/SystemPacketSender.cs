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
        //Creates and returns a new network packet telling a client to update their next expected packet number
        public static NetworkPacket GetNewNextPacketNumber(int ClientID, int NextPacketNumber)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.NewNextPacketNumber);
            Packet.WriteInt(NextPacketNumber);
            return Packet;
        }

        //Asks a client if they are still connected, requesting for them to immediately reply to us letting us know
        public static void SendStillAliveRequest(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " still alive request");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.StillConnectedCheck);
            PacketQueue.QueuePacket(ClientID, Packet);
            //ConnectionManager.SendPacket(ClientID, Packet);
        }

        //Tells a client their connection has been desynchronized so it can be reset and fixed
        public static NetworkPacket GetDeSyncAlert(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " DeSync Alert");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.ConnectionDeSync);
            return Packet;
        }

        //Gives the PacketType telling a client this is their set of missing packets being resent
        public static NetworkPacket GetMissingPacketsReply(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " Missing Packets Reply.");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.MissingPacketsReply);
            return Packet;
        }

        //Sends a message to a client letting them know we have missed some packets and need them to be resent to us again
        public static void SendMissedPacketAlert(int ClientID, int NextExpectedPacketNumber)
        {
            CommunicationLog.LogOut(ClientID + " Missing Packets Request.");
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.MissedPackets);
            Packet.WriteInt(NextExpectedPacketNumber);
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Sends a message to a client (immediately) letting them know they have been kicked from the game
        public static void SendKickPlayer(int ClientID)
        {
            //Get the client who we are kicking from the server
            ClientConnection TargetClient = ConnectionManager.GetClientConnection(ClientID);

            //Log what is happening here
            MessageLog.Print("Kicking " + TargetClient.NetworkID + " from the server...");

            //Create a new packet to send letting them know they have been kicked
            NetworkPacket Packet = new NetworkPacket();

            //Give it the next packet number they are expecting, then write the identifying packet type
            Packet.WriteInt(TargetClient.GetNextOutgoingPacketNumber());
            Packet.WriteType(ServerPacketType.KickedFromServer);

            //Send this packet off to them immediately to ensure its sent to them before we clean up their network connection ourselves
            TargetClient.SendPacketImmediately(Packet);
        }
    }
}