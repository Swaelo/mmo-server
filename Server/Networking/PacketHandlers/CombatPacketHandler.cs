// ================================================================================================================================
// File:        CombatPacketHandler.cs
// Description: Handles any client packets which are recieved regarding any combat actions that are performed
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Logging;

namespace Server.Networking.PacketHandlers
{
    public static class CombatPacketHandler
    {
        //Retrives values for an account login request
        public static NetworkPacket GetValuesPlayerAttackAlert(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayerAttackAlert);
            Packet.WriteVector3(ReadFrom.ReadVector3());
            return Packet;
        }

        //Handles alert from player letting us know they have performed an attack, checks if it hits anyone then updated their health
        public static void HandlePlayerAttackAlert(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + "Player Attack Alert.");

            //Get the client who performed this attack, make sure their connection is still active
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                //Ignore the request if we cant find their connection open
                MessageLog.Print("ERROR: Connection to this client could not be found, unable to handle their player attack alert.");
                Client.ClientDead = true;
                return;
            }

            //Flag the client as needing to perform an attack on the next physics update
            Client.AttackPerformed = true;
            Client.AttackPosition = Packet.ReadVector3();
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesPlayerRespawnRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayerRespawnRequest);
            return Packet;
        }

        //Handles request from the player asking to have their character respawned
        public static void HandlePlayerRespawnRequest(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + "Player Respawn Request.");
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                MessageLog.Print("ERROR: Connection to this client could not be found, unable to handle their respawn request.");
                return;
            }
            Client.WaitingToRespawn = true;
        }
    }
}
