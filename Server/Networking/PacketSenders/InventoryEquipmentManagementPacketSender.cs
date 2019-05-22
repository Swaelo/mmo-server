using System.Collections.Generic;
using Server.Networking;
using Server.Interface;
using Server.Data;
using Server.Database;
using Server.GameItems;

namespace Server.Networking.PacketSenders
{
    public static class InventoryEquipmentManagementPacketSender
    {
        //Sends a user a complete list of every item currently stored in their inventory
        public static void SendCharacterInventoryItems(int ClientID, string CharacterName)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.PlayerInventoryItems);
            Log.PrintOutgoingPacketMessage(ClientID + ": InventoryEquipmentManagement.SendCharacterInventoryItems");

            //Fetch the complete list of items currently in this characters inventory
            List<ItemData> InventoryItems = InventoriesDatabase.GetAllInventorySlots(CharacterName);

            //Loop through this list and write each items information into the packet data
            foreach(ItemData InventoryItem in InventoryItems)
            {
                QueueWriter.WriteInt(InventoryItem.ItemNumber);
                QueueWriter.WriteInt(InventoryItem.ItemID);
            }
        }

        //Sends a user a complete list of every piece of equipped their character is currently wearing
        public static void SendCharacterEquipmentItems(int ClientID, string CharacterName)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.PlayerEquipmentItems);
            Log.PrintOutgoingPacketMessage(ClientID + ": InventoryEquipmentManagement.SendCharacterEquipmentItems");

            //Fetch the list of items currently equipped on their character
            List<ItemData> EquipmentItems = EquipmentsDatabase.GetAllEquipmentSlots(CharacterName);

            //Loop through and write each equipments information into the packet data
            foreach(ItemData EquippedItem in EquipmentItems)
            {
                QueueWriter.WriteInt((int)EquippedItem.ItemEquipmentSlot);
                QueueWriter.WriteInt(EquippedItem.ItemNumber);
                QueueWriter.WriteInt(EquippedItem.ItemID);
            }
        }

        //Sends a client a complete list of every ability gem currently socketed onto their action bar
        public static void SendCharacterActionBarAbilities(int ClientID, string CharacterName)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.PlayerActionBarAbilities);
            Log.PrintOutgoingPacketMessage(ClientID + ": InventoryEquipmentManagement.SendCharacterActionBarAbilities");

            //Fetch the list of abilities currently socketed onto this characters action bar
            List<ItemData> AbilityItems = ActionBarsDatabase.GetEveryActionBarItem(CharacterName);

            //Write each items information into the packet data
            foreach(ItemData SocketedAbility in AbilityItems)
            {
                QueueWriter.WriteInt(SocketedAbility.ItemNumber);
                QueueWriter.WriteInt(SocketedAbility.ItemID);
            }
        }

        //Sends a client a complete list of every item in their characters inventory, every item currently equipped and every ability currently socketed
        public static void SendCharacterEverything(int ClientID, string CharacterName)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.PlayerTotalItemUpdate);
            Log.PrintOutgoingPacketMessage(ClientID + ": InventoryEquipmentManagement.SendCharacterEverything");

            //Fetch and write the inventory item contents into the packet data
            List<ItemData> InventoryItems = InventoriesDatabase.GetAllInventorySlots(CharacterName);
            foreach(ItemData StoredItem in InventoryItems)
            {
                QueueWriter.WriteInt(StoredItem.ItemNumber);
                QueueWriter.WriteInt(StoredItem.ItemID);
            }

            //Fetch and write the equipped items contents into the packet data
            List<ItemData> EquippedItems = EquipmentsDatabase.GetAllEquipmentSlots(CharacterName);
            foreach(ItemData EquippedItem in EquippedItems)
            {
                QueueWriter.WriteInt((int)EquippedItem.ItemEquipmentSlot);
                QueueWriter.WriteInt(EquippedItem.ItemNumber);
                QueueWriter.WriteInt(EquippedItem.ItemID);
            }

            //Fetch and write all the socketed abilities info into the packet data
            List<ItemData> SocketedAbilities = ActionBarsDatabase.GetEveryActionBarItem(CharacterName);
            foreach(ItemData Ability in SocketedAbilities)
            {
                QueueWriter.WriteInt(Ability.ItemNumber);
                QueueWriter.WriteInt(Ability.ItemID);
            }
        }
    }
}
