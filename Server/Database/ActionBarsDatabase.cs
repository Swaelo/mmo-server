// ================================================================================================================================
// File:        ActionBarsDatabase.cs
// Description: Allows the server to interact with the local SQL database actionbars tables
// ================================================================================================================================

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Server.GameItems;

namespace Server.Database
{
    public static class ActionBarsDatabase
    {
        //Returns the slot number of the first available action bar slot (assumes there is one available)
        private static int GetFirstFreeActionBarSlot(string CharacterName)
        {
            //Fetch the current status of every slot in the characters action bar
            List<ItemData> CharactersActionBars = GetEveryActionBarItem(CharacterName);

            //Look through them all trying to find any which is empty
            for (int i = 1; i < 6; i++)
            {
                //Return this action bars slot number if its empty
                if (CharactersActionBars[i].ItemNumber == 0)
                    return i;
            }

            //Return garbage value if no action bars were free
            return -1;
        }

        //Purges a characters action bar of all items, leaving it completely empty
        public static void PurgeCharactersActionBar(string CharacterName)
        {
            for (int i = 1; i < 6; i++)
            {
                string Query = GetPurgeQuery(CharacterName, i);
                MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
                Command.ExecuteNonQuery();
            }
        }

        private static string GetPurgeQuery(string CharacterName, int ActionBarSlot)
        {
            return "UPDATE actionbars SET ActionBarSlot" + ActionBarSlot + "ItemNumber='0', ActionBarSlot" + ActionBarSlot + "ItemID='0' WHERE CharacterName='" + CharacterName + "'";
        }

        //string Query = "UPDATE actionbars SET ActionBarSlot" + GetFirstFreeActionBarSlot(CharacterName) + "ItemNumber='" + AbilityItem.ItemNumber + "', ActionBarSlot" + GetFirstFreeActionBarSlot(CharacterName) + "ItemID='" + AbilityItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";

        //Returns ItemData object detailing the current state of one of the characters action bar slots
        public static ItemData GetActionBarItem(string CharacterName, int ActionBarSlot)
        {
            //Create a new ItemData object to store the items information
            ItemData ActionBarItem = new ItemData();

            //Read the items values from the database into the ActionBarItem object
            string Query = "SELECT ActionBarSlot" + ActionBarSlot + "ItemNumber FROM actionbars WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            ActionBarItem.ItemNumber = Convert.ToInt32(Command.ExecuteScalar());
            Query = "SELECT ActionBarSlot" + ActionBarSlot + "ItemID FROM actionbars WHERE CharacterName='" + CharacterName + "'";
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            ActionBarItem.ItemID = Convert.ToInt32(Command.ExecuteScalar());

            //Return the final object containing all the action bars current data
            return ActionBarItem;
        }

        //Returns a list of ItemData objects detailing the current state of every one of the characters action bar slots
        public static List<ItemData> GetEveryActionBarItem(string CharacterName)
        {
            //Create a new list to store all the action bar items information
            List<ItemData> ActionBarItems = new List<ItemData>();

            //Add every one of the characters action bar items into the list
            for (int i = 1; i < 6; i++)
            {
                ActionBarItems.Add(GetActionBarItem(CharacterName, i));
                ActionBarItems[i - 1].ItemActionBarSlot = i;
            }

            //Return the final list of all the characters action bar slots
            return ActionBarItems;
        }

        //Moves an ability gem from one slot of the characters action bar, to one of the other free slots on the action bar
        public static void MoveActionBarItem(string CharacterName, int ActionBarSlot, int DestinationActionBarSlot)
        {
            //Read the items information that is being moved around the characters action bar
            ItemData MovingAbility = GetActionBarItem(CharacterName, ActionBarSlot);
            //Remove it from the characters possession, then re add it to the new action bar slot destination
            TakeCharacterAbility(CharacterName, ActionBarSlot);
            GiveCharacterAbility(CharacterName, MovingAbility, DestinationActionBarSlot);
        }

        //Swaps an ability gem from one slot, with another ability gem in a different slot on the characters action bar
        public static void SwapActionBarItems(string CharacterName, int FirstActionBarSlot, int SecondActionBarSlot)
        {
            //Get the information about the item currently equipped in the two action bar slots we are dealing with
            ItemData FirstAbility = GetActionBarItem(CharacterName, FirstActionBarSlot);
            ItemData SecondAbility = GetActionBarItem(CharacterName, SecondActionBarSlot);
            //Move each ability over to the other abilities action bar slot
            GiveCharacterAbility(CharacterName, FirstAbility, SecondActionBarSlot);
            GiveCharacterAbility(CharacterName, SecondAbility, FirstActionBarSlot);
        }

        //Equips an ability gem onto the first available slot on the characters action bar
        public static void GiveCharacterAbility(string CharacterName, ItemData AbilityItem)
        {
            //Define/execute command to update the database placing this ability onto the characters first available action bar slot
            string Query = "UPDATE actionbars SET ActionBarSlot" + GetFirstFreeActionBarSlot(CharacterName) + "ItemNumber='" + AbilityItem.ItemNumber + "', ActionBarSlot" + GetFirstFreeActionBarSlot(CharacterName) + "ItemID='" + AbilityItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Equips an ability gem onto a specific slot of the characters action bar
        public static void GiveCharacterAbility(string CharacterName, ItemData AbilityItem, int ActionBarSlot)
        {
            string Query = "UPDATE actionbars SET ActionBarSlot" + ActionBarSlot + "ItemNumber='" + AbilityItem.ItemNumber + "', ActionBarSlot" + ActionBarSlot + "ItemID='" + AbilityItem.ItemID + "' WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Removes an ability gem from a specific slot of the characters action bar
        public static void TakeCharacterAbility(string CharacterName, int ActionBarSlot)
        {
            string Query = "UPDATE actionbars SET ActionBarSlot" + ActionBarSlot + "ItemNumber='0', ActionBarSlot" + ActionBarSlot + "ItemID='0' WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }
    }
}
