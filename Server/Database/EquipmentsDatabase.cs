// ================================================================================================================================
// File:        EquipmentsDatabase.cs
// Description: Allows the server to interact with the local SQL database equipments tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Server.GameItems;

namespace Server.Database
{
    class EquipmentsDatabase
    {
        //Removes all equipment entries from the database
        public static void PurgeEquipments()
        {
            string PurgeQuery = "DELETE FROM equipments";
            CommandManager.ExecuteNonQuery(PurgeQuery, "Purging all entires from the equipments database.");
        }

        //Adds an item into a characters equipment
        public static void CharacterEquipItem(string CharacterName, ItemData NewItem)
        {
            //Define a new query, execute it to update the characters equipment table to contain the new item
            string Query = "UPDATE equipments SET " + NewItem.ItemEquipmentSlot.ToString() + "ItemNumber='" + NewItem.ItemNumber + "', " + NewItem.ItemEquipmentSlot.ToString() + "ItemID='" + NewItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";
            CommandManager.ExecuteNonQuery(Query, "Equipping " + NewItem.ItemName + " onto " + CharacterName);
        }

        //Removes an item from a characters equipment
        public static void CharacterRemoveItem(string CharacterName, EquipmentSlot EquipmentSlot)
        {
            //Define a new query, execute it in a command to update the characters equipment table to remove what item is in the specified equipment slot
            string Query = "UPDATE equipments SET " + EquipmentSlot.ToString() + "ItemNumber='0' WHERE Charactername='" + CharacterName + "'";
            CommandManager.ExecuteNonQuery(Query, "Unequipping item from " + CharacterName + "s " + EquipmentSlot + " equipment slot");
        }

        //Returns an ItemData object detailing the current state of one of a characters equipment slots
        public static ItemData GetEquipmentSlot(string CharacterName, EquipmentSlot EquipmentSlot)
        {
            //Create a new ItemData object to store all the items information
            ItemData EquippedItem = new ItemData();
            EquippedItem.ItemEquipmentSlot = EquipmentSlot;

            //Fetch the ItemNumber and ItemID values from the database and store these within the new ItemData object
            string ItemNumberQuery = "SELECT " + EquipmentSlot.ToString() + "ItemNumber FROM equipments WHERE CharacterName='" + CharacterName + "'";
            EquippedItem.ItemNumber = CommandManager.ExecuteScalar(ItemNumberQuery, "Fetching ItemNumber value in " + CharacterName + "s " + EquipmentSlot + " equipment slot");
            string ItemIDQuery = "SELECT " + EquipmentSlot.ToString() + "ItemID FROM equipments WHERE CharacterName='" + CharacterName + "'";
            EquippedItem.ItemID = CommandManager.ExecuteScalar(ItemIDQuery, "Fetching ItemID value in " + CharacterName + "s " + EquipmentSlot + " equipment slot");

            //Return the final ItemData object with all the equipped items information
            return EquippedItem;
        }

        //Returns a list of ItemData objects, detailing the current state of every one of the characters equipment slots
        public static List<ItemData> GetAllEquipmentSlots(string CharacterName)
        {
            //Create a new list to store the ItemData object for each of the characters equipment slots
            List<ItemData> EquipmentItems = new List<ItemData>();

            //Add the contents of every single equipment slot into the list
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.Head));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.Back));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.Neck));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.LeftShoulder));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.RightShoulder));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.Chest));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.LeftGlove));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.RightGlove));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.Legs));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.LeftHand));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.RightHand));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.LeftFoot));
            EquipmentItems.Add(GetEquipmentSlot(CharacterName, EquipmentSlot.RightFoot));

            //Return the final list of ItemData objects listening the current state of each equipment slot
            return EquipmentItems;
        }
    }
}
