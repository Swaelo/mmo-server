// ================================================================================================================================
// File:        GameWorldStatePacketHandler.cs
// Description: Handles client packets regarding the current state of the game world
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Database;
using Server.Data;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class GameWorldStatePacketHandler
    {
        //When a client wants to enter the game world, we need to send them a bunch of information to set up their game world before they can enter
        public static void HandleEnterWorldRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Read the characters name the player is going to use, use it to fetch the rest of the characters data from the database
            string CharacterName = Packet.ReadString();
            CharacterData CharacterData = CharactersDatabase.GetCharacterData(CharacterName);

            //Fetch this clients ClientConnection object and update it with the info from CharacterData that we retrieved
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.CharacterName = CharacterName;
            Client.CharacterPosition = CharacterData.Position;

            //Send the clients lists of other players, AI entities, item pickups, inventory contents, equipped items and socketed actionbar abilities
            GameWorldStatePacketSender.SendActivePlayerList(ClientID);
            GameWorldStatePacketSender.SendActiveEntityList(ClientID);
            GameWorldStatePacketSender.SendActiveItemList(ClientID);
            GameWorldStatePacketSender.SendInventoryContents(ClientID, CharacterName);
            GameWorldStatePacketSender.SendEquippedItems(ClientID, CharacterName);
            GameWorldStatePacketSender.SendSocketedAbilities(ClientID, CharacterName);
        }

        //When a client has finished receiving all the setup information they will let us know when they are entering into the game world finally
        public static void HandleNewPlayerReady(int ClientID, ref NetworkPacket Packet)
        {
            //Get this clients information and flag them as needing to be added into the game world
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.WaitingToEnter = true;
        }
    }
}