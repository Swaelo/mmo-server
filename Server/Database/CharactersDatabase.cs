// ================================================================================================================================
// File:        CharactersDatabase.cs
// Description: Allows the server to interact with the local SQL database characters tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using MySql.Data.MySqlClient;
using Server.Data;
using Server.Logging;

namespace Server.Database
{
    class CharactersDatabase
    {
        //Checks if the given character name has already been taken by someone else or is free to use
        //NOTE: assumes the character name provided is valid
        public static bool IsCharacterNameAvailable(string CharacterName)
        {
            //Define query/command for checking if the given character name is still available
            string CharacterNameQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand CharacterNameCommand = CommandManager.CreateCommand(CharacterNameQuery);

            //Execute the command checking if this character name is still available to be used
            return !CommandManager.ExecuteRowCheck(CharacterNameCommand, "Error checking if " + CharacterName + " is an available character name that can still be used.");
        }

        //Returns the number of characters that exist under a given user account
        public static int GetCharacterCount(string AccountName)
        {
            //Define query/command used to check how many characters this user has created so far
            string CharacterCountQuery = "SELECT CharactersCreated FROM accounts WHERE Username='" + AccountName + "'";
            MySqlCommand CharacterCountCommand = new MySqlCommand(CharacterCountQuery);

            //Execute the command and return the value we are given, which shows how many characters this account owns
            return CommandManager.ExecuteScalar(CharacterCountCommand, "Error checking how many characters " + AccountName + " has created so far");
        }

        //Saves a brand new player character into the characters database
        public static void SaveNewCharacter(string AccountName, string CharacterName)
        {
            //Define new query/command for inserting a new row into the characters table where this new characters information will be stored
            string InsertRowQuery = "INSERT INTO characters(OwnerAccountname,CharacterName,IsMale) VALUE('" + AccountName + "','" + CharacterName + "','" + 1 + "')";
            MySqlCommand InsertRowCommand = CommandManager.CreateCommand(InsertRowQuery);
            //Execute this command, inserting a new row into the characters table to save this new characters information
            CommandManager.ExecuteNonQuery(InsertRowCommand, "Error inserting new row into the characters table in preperation for saving new character information");

            //Define and execute a new query/command for updating the accounts table to reference this users new character count
            int NewCharacterCount = GetCharacterCount(AccountName) + 1;
            string CharacterCountQuery = "UPDATE accounts SET CharactersCreated='" + NewCharacterCount + "' WHERE Username='" + AccountName + "'";
            MySqlCommand CharacterCountCommand = CommandManager.CreateCommand(CharacterCountQuery);
            CommandManager.ExecuteNonQuery(CharacterCountCommand);

            //Define and execute a new query/command for updating the accounts table to reference this character under the owners account details
            string NewCharacterReference = NewCharacterCount == 1 ? "FirstCharacterName" :
                NewCharacterCount == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            string CharacterReferenceQuery = "UPDATE accounts SET " + NewCharacterReference + "='" + CharacterName + "' WHERE Username='" + AccountName + "'";
            MySqlCommand CharacterReferenceCommand = CommandManager.CreateCommand(CharacterReferenceQuery);
            CommandManager.ExecuteNonQuery(CharacterReferenceCommand);

            //Define and execute new queries/commands for inserting a new blank entry into each of the Inventory, Equipments and Actionbars tables for this newly created character
            string InventoryQuery = "INSERT INTO inventories(CharacterName) VALUE('" + CharacterName + "')";
            MySqlCommand InventoryCommand = CommandManager.CreateCommand(InventoryQuery);
            CommandManager.ExecuteNonQuery(InventoryCommand);
            string EquipmentQuery = "INSERT INTO equipments(CharacterName) VALUE('" + CharacterName + "')";
            MySqlCommand EquipmentCommand = CommandManager.CreateCommand(EquipmentQuery);
            CommandManager.ExecuteNonQuery(EquipmentCommand);
            string ActionBarQuery = "INSERT INTO actionbars(CharacterName) VALUE('" + CharacterName + "')";
            MySqlCommand ActionBarCommand = CommandManager.CreateCommand(ActionBarQuery);
            CommandManager.ExecuteNonQuery(ActionBarCommand);
        }

        //Returns the name of the users character which exists in the given character slot number
        public static string GetCharacterName(string AccountName, int CharacterSlot)
        {
            //Define a new query/command for reading the character names inside someones user account
            string CharacterNameQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            MySqlCommand CharacterNameCommand = CommandManager.CreateCommand(CharacterNameQuery);
            //Define the name of the string value we want to extract from the database
            string DatabaseStringName = CharacterSlot == 1 ? "FirstCharacterName" :
                CharacterSlot == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            //Execute the sql command with this string name to extract and return the character name we are looking for
            return CommandManager.ExecuteString(CharacterNameCommand, DatabaseStringName, "Error trying to read the character name of " + AccountName + "s " + DatabaseStringName);
        }

        //Loads all of a characters information from the database
        public static CharacterData GetCharacterData(string CharacterName)
        {
            //Create a new CharacterData object to store all the data we are going to retrieve from the database
            CharacterData CharacterData = new CharacterData();

            //First open up this characters table and start reading all the data from it
            string CharacterDataQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand CharacterDataCommand = new MySqlCommand(CharacterDataQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader CharacterDataReader = CharacterDataCommand.ExecuteReader();
            if(CharacterDataReader.Read())
            {
                //Extract and store all of this characters information into the new CharacterData object
                CharacterData.Account = CharacterDataReader["OwnerAccountName"].ToString();
                CharacterData.Position = new Vector3(Convert.ToInt64(CharacterDataReader["XPosition"]), Convert.ToInt64(CharacterDataReader["YPosition"]), Convert.ToInt64(CharacterDataReader["ZPosition"]));
                CharacterData.Name = CharacterName;
                CharacterData.Experience = Convert.ToInt32(CharacterDataReader["ExperiencePoints"]);
                CharacterData.ExperienceToLevel = Convert.ToInt32(CharacterDataReader["ExperienceToLevel"]);
                CharacterData.Level = Convert.ToInt32(CharacterDataReader["Level"]);
                CharacterData.IsMale = Convert.ToBoolean(CharacterDataReader["IsMale"]);

                //Return the final CharacterData object which has all the relevant information stored within
                CharacterDataReader.Close();
                return CharacterData;
            }

            CharacterDataReader.Close();
            MessageLog.Print("CharactersDatabase.GetCharacterData Error reading character data, returning null.");
            return null;
        }

        //Backs up the location of a player character into the database
        public static void SaveCharacterLocation(string CharacterName, Vector3 CharacterLocation)
        {
            //Define a new query/command to store the characters updated position values into the database
            string CharacterLocationQuery = "UPDATE characters SET XPosition='" + CharacterLocation.X + "', YPosition='" + CharacterLocation.Y + "', ZPosition='" + CharacterLocation.Z + "' WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand CharacterLocationCommand = CommandManager.CreateCommand(CharacterLocationQuery);
            //Execute this new command, updating the characters postion values inside the database
            CommandManager.ExecuteNonQuery(CharacterLocationCommand);
        }
    }
}
