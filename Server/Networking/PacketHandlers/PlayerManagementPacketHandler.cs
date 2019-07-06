// ================================================================================================================================
// File:        PlayerManagementPacketHandler.cs
// Description: Manages packets sent from game clients regarding the current state of the player characters
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public class PlayerManagementPacketHandler
    {
        //Handles a clients player character updated position/rotation values
        public static void HandlePlayerUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Read the characters name and their new updated position value
            string CharacterName = Packet.ReadString();
            Vector3 CharacterPosition = Packet.ReadVector3();

            //Get the ClientConnection object for the client who send us this update
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];

            //Set the clients new position flag and store the new position value in it
            Client.NewPositionReceived = true;
            Client.NewPosition = CharacterPosition;

            //Get a list of all the other ingame clients so we can share this clients new character position with them
            List<ClientConnection> OtherInGameClients = ConnectionManager.GetInGameClientsExceptFor(ClientID);

            //Loop through all of these other ingame clients, sending each of them this clients updated player location
            foreach(ClientConnection OtherClient in OtherInGameClients)
                PlayerManagementPacketSender.SendPlayerUpdate(OtherClient.NetworkID, CharacterName, CharacterPosition);
        }
    }
}