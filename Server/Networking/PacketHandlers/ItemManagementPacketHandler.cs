// ================================================================================================================================
// File:        ItemManagementPacketHandler.cs
// Description: Handles packets from game clients regarding item pickups
// ================================================================================================================================

using System.Numerics;
using Server.Database;
using Server.GameItems;
using Server.Interface;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class ItemManagementPacketHandler
    {
        //User is trying to pick up an item from the ground
        public static void HandlePlayerTakeItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterTakeItem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Find the information about the item the character is trying to pick up
            string CharacterName = Reader.ReadString();
            int ItemNumber = Reader.ReadInt();
            int ItemID = Reader.ReadInt();

            //Ignore the request entirely if the character has no free space in their inventory
            if (InventoriesDatabase.IsInventoryFull(CharacterName))
                return;

            //Get the information about the item the character is picking up, place it in their inventory
            ItemData GroundItem = ItemList.MasterItemList[ItemNumber];
            InventoriesDatabase.GiveCharacterItem(CharacterName, GroundItem);
            //Send them a UI update

            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
            //Remove the item pickup from the game world, automatically telling all active clients to do the same on their worlds
            ItemManager.RemoveItemPickup(ItemID);
        }

        //Removes an item from a players inventory
        public static void HandleRemoveInventoryItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.RemoveInventoryItem");

            //Read the information from the packet data
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string CharacterName = Reader.ReadString();
            int BagSlot = Reader.ReadInt();

            //Remove the item from the players inventory
            InventoriesDatabase.RemoveCharacterItem(CharacterName, BagSlot);

            //Update the player on their new inventory contents
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Moves an item from the players inventory to their equipment screen
        public static void HandleEquipInventoryItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.EquipInventoryItem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Find the characters name, which bag slot holds the item, and which gear slot its being equipped to
            string CharacterName = Reader.ReadString();
            int BagSlot = Reader.ReadInt();
            EquipmentSlot GearSlot = (EquipmentSlot)Reader.ReadInt();

            //Get the information about the item in the players bag, remove it from their possession then equip it
            ItemData InventoryItem = InventoriesDatabase.GetInventorySlot(CharacterName, BagSlot);
            InventoryItem.ItemEquipmentSlot = GearSlot;
            InventoriesDatabase.RemoveCharacterItem(CharacterName, BagSlot);
            EquipmentsDatabase.CharacterEquipItem(CharacterName, InventoryItem);
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Moves an item from the players equipment to their inventory
        public static void HandleUnequipItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.UnequipItem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Find the characters name, which slot they want to remove the item from, and which bag slot they want to move it to
            string CharacterName = Reader.ReadString();
            EquipmentSlot EquipmentSlot = (EquipmentSlot)Reader.ReadInt();
            int BagSlot = Reader.ReadInt();

            //Find the items data, move it from the characters equipment to their inventory, send them UI update
            ItemData EquippedItem = EquipmentsDatabase.GetEquipmentSlot(CharacterName, EquipmentSlot);
            EquipmentsDatabase.CharacterRemoveItem(CharacterName, EquipmentSlot);
            InventoriesDatabase.GiveCharacterItem(CharacterName, EquippedItem, BagSlot);
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Changes the position of one of the items in a players inventory
        public static void HandleMoveInventoryItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.MoveInventoryItem");

            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string CharacterName = Reader.ReadString();
            int OriginalBagSlot = Reader.ReadInt();
            int DestinationBagSlot = Reader.ReadInt();
            InventoriesDatabase.MoveInventoryItem(CharacterName, OriginalBagSlot, DestinationBagSlot);
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Swaps the positions of two items in a players inventory
        public static void HandleSwapInventoryItems(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.SwapInventoryItems");

            //Extract all the info we need from the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            string CharacterName = Reader.ReadString();
            int FirstBagSlot = Reader.ReadInt();
            int SecondBagSlot = Reader.ReadInt();

            //Swap the positions of these two items in the players inventory
            InventoriesDatabase.SwapInventoryItem(CharacterName, FirstBagSlot, SecondBagSlot);

            //Send back to the player up to date information on the inventory and equipment state
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Swaps the position of an equipped item and an item in the players inventory
        public static void HandleSwapEquipmentItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.SwapEquipmentItem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Extract the nessacery information
            string CharacterName = Reader.ReadString();
            int BagSlot = Reader.ReadInt();
            EquipmentSlot EquipSlot = (EquipmentSlot)Reader.ReadInt();

            //Get each items information that is going to be swapped around
            ItemData InventoryItemData = InventoriesDatabase.GetInventorySlot(CharacterName, BagSlot);
            InventoryItemData.ItemEquipmentSlot = EquipSlot;
            ItemData EquippedItemData = EquipmentsDatabase.GetEquipmentSlot(CharacterName, EquipSlot);

            //Update the characters inventory and equipment, overwriting each item with the other one
            InventoriesDatabase.GiveCharacterItem(CharacterName, EquippedItemData, BagSlot);
            EquipmentsDatabase.CharacterEquipItem(CharacterName, InventoryItemData);

            //Send the player a UI update now
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Removes an item from a players posession and drops it into the game world for anyone to take
        public static void HandlePlayerDropItem(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterDropItem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Handle the item drop diferent depending on where its coming from (inventory, equipment or ability bar)
            int DropSource = Reader.ReadInt();
            switch (DropSource)
            {
                //DropSource 1 = Characters Inventory
                case (1):
                    {
                        //Find out who dropped the item, what item was dropped, and where they dropped it
                        string CharacterName = Reader.ReadString();
                        int BagSlot = Reader.ReadInt();
                        Vector3 DropLocation = Maths.VectorTranslate.ConvertVector(Reader.ReadVector3());
                        //Find the items information and remove it from the characters possession
                        ItemData InventoryItem = InventoriesDatabase.GetInventorySlot(CharacterName, BagSlot);
                        InventoriesDatabase.RemoveCharacterItem(CharacterName, BagSlot);
                        InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
                        //FInally, add the item into the game world as a new pickup object
                        ItemManager.AddItemPickup(InventoryItem.ItemNumber, DropLocation);
                    }
                    break;

                //DropSource 2 = Characters Equipment
                case (2):
                    {
                        //Find out who dropped the item, what slot its equipped in, and where they dropped it
                        string CharacterName = Reader.ReadString();
                        EquipmentSlot GearSlot = (EquipmentSlot)Reader.ReadInt();
                        Vector3 DropLocation = Maths.VectorTranslate.ConvertVector(Reader.ReadVector3());
                        //Find the items information and remove it from the characters possession
                        ItemData EquipmentItem = EquipmentsDatabase.GetEquipmentSlot(CharacterName, GearSlot);
                        EquipmentsDatabase.CharacterRemoveItem(CharacterName, GearSlot);
                        InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
                        //Finally add the item into the game world as a new pickup object
                        ItemManager.AddItemPickup(EquipmentItem.ItemNumber, DropLocation);
                    }
                    break;

                //DropSource 3 = Characters Action Bar
                case (3):
                    {
                        //Find out who dropped the item, which action bar slot it came from, and where they dropped it
                        string CharacterName = Reader.ReadString();
                        int ActionBarSlot = Reader.ReadInt();
                        Vector3 DropLocation = Maths.VectorTranslate.ConvertVector(Reader.ReadVector3());
                        //Get the items information and remove it from the characters possession
                        ItemData AbilityItem = ActionBarsDatabase.GetActionBarItem(CharacterName, ActionBarSlot);
                        ActionBarsDatabase.TakeCharacterAbility(CharacterName, ActionBarSlot);
                        InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
                        //Add the item into the game world as a new pickup object
                        ItemManager.AddItemPickup(AbilityItem.ItemNumber, DropLocation);
                    }
                    break;
            }
        }

        //Moves an ability gem from a characters inventory onto their action bar
        public static void HandlePlayerEquipAbility(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterEquipAbility");

            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Get the characters name, what bag slot has the ability gem inside, and what action bar slot its going to be equipped to
            string CharacterName = Reader.ReadString();
            int GemBagSlot = Reader.ReadInt();
            int ActionBarSlot = Reader.ReadInt();

            //Read the item data from the players inventory
            ItemData AbilityGem = InventoriesDatabase.GetInventorySlot(CharacterName, GemBagSlot);
            //Place it onto their action bar
            ActionBarsDatabase.GiveCharacterAbility(CharacterName, AbilityGem, ActionBarSlot);
            //Remove it from their inventory
            InventoriesDatabase.RemoveCharacterItem(CharacterName, GemBagSlot);
            //Give player total item update
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Swaps an ability gem from a characters inventory with one that is already equipped
        public static void HandlePlayerSwapEquipAbility(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterSwapEquipAbility");

            //Open up the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Extract the required information out of the packet data
            string CharacterName = Reader.ReadString();
            int BagSlot = Reader.ReadInt();
            int ActionBarSlot = Reader.ReadInt();

            //Read the information for both the item in the players inventory and the item currently on their action bar
            ItemData InventoryItem = InventoriesDatabase.GetInventorySlot(CharacterName, BagSlot);
            ItemData ActionBarItem = ActionBarsDatabase.GetActionBarItem(CharacterName, ActionBarSlot);

            //Remove the current ability gem from the players action bar and place it into the players inventory
            ActionBarsDatabase.TakeCharacterAbility(CharacterName, ActionBarSlot);
            InventoriesDatabase.GiveCharacterItem(CharacterName, ActionBarItem, BagSlot);
            //Add the newly equipped ability gem onto the players action bar and send them their updated item data
            ActionBarsDatabase.GiveCharacterAbility(CharacterName, InventoryItem, ActionBarSlot);
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Unequips an ability gem from a character and places it into their inventory
        public static void HandlePlayerUnequipAbility(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterUnequipAbility");

            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            string CharacterName = Reader.ReadString();
            int BagSlot = Reader.ReadInt();
            int ActionBarSlot = Reader.ReadInt();

            //Look up the item, place it into the players inventory
            ItemData EquippedAbility = ActionBarsDatabase.GetActionBarItem(CharacterName, ActionBarSlot);
            InventoriesDatabase.GiveCharacterItem(CharacterName, EquippedAbility, BagSlot);

            //Remove it from their ability bar
            ActionBarsDatabase.TakeCharacterAbility(CharacterName, ActionBarSlot);

            //Update players items status
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Swaps the positions of the gems currently on the action bar
        public static void HandlePlayerSwapAbilities(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterSwapAbilities");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Read the required information from the packet data
            string CharacterName = Reader.ReadString();
            int FirstActionBarSlot = Reader.ReadInt();
            int SecondActionBarSlot = Reader.ReadInt();

            //Look up the items in each slot of the action bar
            ItemData FirstAbility = ActionBarsDatabase.GetActionBarItem(CharacterName, FirstActionBarSlot);
            ItemData SecondAbility = ActionBarsDatabase.GetActionBarItem(CharacterName, SecondActionBarSlot);

            //Place each item in the other items position
            ActionBarsDatabase.GiveCharacterAbility(CharacterName, FirstAbility, SecondActionBarSlot);
            ActionBarsDatabase.GiveCharacterAbility(CharacterName, SecondAbility, FirstActionBarSlot);

            //Send the player a full update now
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }

        //Moves the position of one of the gems on the characters action bar
        public static void HandlePlayerMoveAbility(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": ItemManagement.CharacterMoveAbilityGem");

            //Open the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Read the data needed from the packet
            string CharacterName = Reader.ReadString();
            int AbilityBarSlot = Reader.ReadInt();
            int DestinationBarSlot = Reader.ReadInt();

            //Get the data regarding the item that needs to be moved
            ItemData AbilityGem = ActionBarsDatabase.GetActionBarItem(CharacterName, AbilityBarSlot);
            //Remove this gem from the characters ability bar, then re add it at the new location
            ActionBarsDatabase.TakeCharacterAbility(CharacterName, AbilityBarSlot);
            ActionBarsDatabase.GiveCharacterAbility(CharacterName, AbilityGem, DestinationBarSlot);

            //Send updated UI info to the player now that the ability gem has been moved to where they wanted it
            InventoryEquipmentManagementPacketSender.SendCharacterEverything(ClientID, CharacterName);
        }
    }
}
