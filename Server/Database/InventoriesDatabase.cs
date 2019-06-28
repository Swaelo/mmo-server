// ================================================================================================================================
// File:        InventoriesDatabase.cs
// Description: Allows the server to interact with the local SQL database inventories tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Server.GameItems;
using Server.Interface;

namespace Server.Database
{
    class InventoriesDatabase
    {
        //Gets the bag slot number of the first free inventory slot available in a characters inventory
        private static int GetFirstFreeInventorySlot(string CharacterName)
        {
            //Fetch the current state of the characters inventory
            List<ItemData> CharactersInventory = GetAllInventorySlots(CharacterName);

            //Search through all the inventory slots until an empty one is found
            for (int i = 0; i < CharactersInventory.Count; i++)
            {
                //Return this bag slots index if its empty
                if (CharactersInventory[i].ItemNumber == 0 || CharactersInventory[i].ItemNumber == -1)
                    return (i + 1);
            }

            //No empty inventory slot could be found
            return -1;
        }

        //Returns an ItemData object detailing what is currently being stored in a characters inventory slot
        public static ItemData GetInventorySlot(string CharacterName, int InventorySlot)
        {
            //Create a new ItemData object to store all the data about the characters inventory slot
            ItemData InventoryItem = new ItemData();

            //Extract the items information from the inventory database into the new ItemData object
            string Query = "SELECT ItemSlot" + InventorySlot + "ItemNumber FROM inventories WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            InventoryItem.ItemNumber = Convert.ToInt32(Command.ExecuteScalar());
            Query = "SELECT ItemSlot" + InventorySlot + "ItemID FROM inventories WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            InventoryItem.ItemID = Convert.ToInt32(Command.ExecuteScalar());
            InventoryItem.ItemEquipmentSlot = ItemInfoDatabase.GetItemSlot(InventoryItem.ItemNumber);

            //Return the final InventoryItem object
            return InventoryItem;
        }

        //Returns a list of ItemData objects detailing the current state of every slot in a characters inventory
        public static List<ItemData> GetAllInventorySlots(string CharacterName)
        {
            //Create a new list to store all the characters inventory slots
            List<ItemData> InventorySlots = new List<ItemData>();

            //Extract all 9 inventory slots information from the database, into the InventorySlots list
            for (int i = 1; i < 10; i++)
            {
                InventorySlots.Add(GetInventorySlot(CharacterName, i));
                InventorySlots[i - 1].ItemInventorySlot = i;
            }

            //Return the final list of all the characters inventory slots
            return InventorySlots;
        }

        //Places an item into the first available slot in a characters inventory
        public static void GiveCharacterItem(string CharacterName, ItemData NewItem)
        {
            //Places the new item into the first free slot in the characters inventory
            string Query = "UPDATE inventories SET ItemSlot" + GetFirstFreeInventorySlot(CharacterName) + "ItemNumber='" + NewItem.ItemNumber + "', ItemSlot" + GetFirstFreeInventorySlot(CharacterName) + "ItemID='" + NewItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Places an item into a specific slot of a characters inventory
        public static void GiveCharacterItem(string CharacterName, ItemData NewItem, int InventorySlot)
        {
            string Query = "UPDATE inventories SET ItemSlot" + InventorySlot + "ItemNumber='" + NewItem.ItemNumber + "', ItemSlot" + InventorySlot + "ItemID='" + NewItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Removes an item from a characters inventory
        public static void RemoveCharacterItem(string CharacterName, int InventorySlot)
        {
            string Query = "UPDATE inventories SET ItemSlot" + InventorySlot + "ItemNumber='0', ItemSlot" + InventorySlot + "ItemID='0' WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Moves an item from one inventory slot to another
        //NOTE: Assumes its moving the item to an empty slot, it will overwrite anything if its there
        public static void MoveInventoryItem(string CharacterName, int ItemBagSlot, int DestinationBagSlot)
        {
            //Get the items information that is being moved around the characters inventory
            ItemData InventoryItem = GetInventorySlot(CharacterName, ItemBagSlot);
            //Remove it from the players possession, then readd it in the new target bag slot
            RemoveCharacterItem(CharacterName, ItemBagSlot);
            GiveCharacterItem(CharacterName, InventoryItem, DestinationBagSlot);
        }

        //Swaps the positions of two items currently in the players inventory
        public static void SwapInventoryItem(string CharacterName, int FirstBagSlot, int SecondBagSlot)
        {
            //Get the information of the two items that are being swapped around in the inventory
            ItemData FirstItem = GetInventorySlot(CharacterName, FirstBagSlot);
            ItemData SecondItem = GetInventorySlot(CharacterName, SecondBagSlot);
            //Readd the items into the players inventory, overwritting the other items slot
            GiveCharacterItem(CharacterName, FirstItem, SecondBagSlot);
            GiveCharacterItem(CharacterName, SecondItem, FirstBagSlot);
        }

        //Checks if the player has any free space remaining in their inventory
        public static bool IsInventoryFull(string CharacterName)
        {
            //Grab the current state of every slot in the characters inventory
            List<ItemData> InventorySlots = GetAllInventorySlots(CharacterName);

            //Run through them all looking for an which is empty
            foreach (ItemData InventorySlot in InventorySlots)
            {
                if (InventorySlot.ItemNumber == 0 || InventorySlot.ItemNumber == -1)
                    return false;
            }

            //Inventory is full is no empty spaces could be found
            return true;
        }
    }
}
