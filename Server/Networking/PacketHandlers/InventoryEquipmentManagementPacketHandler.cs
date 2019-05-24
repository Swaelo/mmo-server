// ================================================================================================================================
// File:        InventoryEquipmentPacketHandler.cs
// Description: Handles packets from clients regarding management of their inventory and worn equipment items
// ================================================================================================================================

using System.Collections.Generic;
using Server.GameItems;
using Server.Database;
using Server.Interface;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class InventoryEquipmentManagementPacketHandler
    {
        //User is requesting for a list of all items in their inventory
        public static void HandlePlayerInventoryRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": InventoryEquipmentManagement.CharacterInventoryRequest");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Read the characters name whos inventory is being requested
            string CharacterName = Reader.ReadString();

            //Retrieve the current list of items being stored in this players inventory
            List<ItemData> InventoryContents = InventoriesDatabase.GetAllInventorySlots(CharacterName);

            InventoryEquipmentManagementPacketSender.SendCharacterInventoryItems(ClientID, CharacterName);
        }

        //User is requesting for a list of items they have equipped on their character
        public static void HandlePlayerEquipmentRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": InventoryEquipmentManagement.CharacterEquipmentRequest");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Read the information from the packet
            string CharacterName = Reader.ReadString();

            InventoryEquipmentManagementPacketSender.SendCharacterEquipmentItems(ClientID, CharacterName);
        }

        //Provide a user with a list of all the abilities currently equipped on their characters action bar
        public static void HandlePlayerActionBarRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": InventoryEquipmentManagement.CharacterActionBarRequest");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Find the characters action bar and send it back to the user who requested it
            string CharacterName = Reader.ReadString();
            InventoryEquipmentManagementPacketSender.SendCharacterActionBarAbilities(ClientID, CharacterName);
        }
    }
}
