// ================================================================================================================================
// File:        CharactersDatabase.cs
// Description: Allows the server to interact with the local SQL database characters tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using MySql.Data.MySqlClient;
using Server.Data;

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
            return !CommandManager.ExecuteRowCheck(CharacterNameQuery, "Checking if the character name " + CharacterName + " is still available");
        }

        //Returns the number of characters that exist under a given user account
        public static int GetCharacterCount(string AccountName)
        {
            //Define query/command used to check how many characters this user has created so far
            string CharacterCountQuery = "SELECT CharactersCreated FROM accounts WHERE Username='" + AccountName + "'";
            return CommandManager.ExecuteScalar(CharacterCountQuery, "Checking how many characters " + AccountName + " has created so far");
        }

        //Saves a brand new player character into the characters database
        public static void SaveNewCharacter(string AccountName, string CharacterName)
        {
            //Define new query/command for inserting a new row into the characters table where this new characters information will be stored
            string InsertRowQuery = "INSERT INTO characters(OwnerAccountname,CharacterName,IsMale) VALUE('" + AccountName + "','" + CharacterName + "','" + 1 + "')";
            CommandManager.ExecuteNonQuery(InsertRowQuery, "Inserting new row into characters table to store data for new character " + CharacterName);

            //Define and execute a new query/command for updating the accounts table to reference this users new character count
            int NewCharacterCount = GetCharacterCount(AccountName) + 1;
            string CharacterCountQuery = "UPDATE accounts SET CharactersCreated='" + NewCharacterCount + "' WHERE Username='" + AccountName + "'";
            CommandManager.ExecuteNonQuery(CharacterCountQuery, "Updating the number of characters that exist under " + AccountName + "s account");

            //Define and execute a new query/command for updating the accounts table to reference this character under the owners account details
            string NewCharacterReference = NewCharacterCount == 1 ? "FirstCharacterName" :
                NewCharacterCount == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            string CharacterReferenceQuery = "UPDATE accounts SET " + NewCharacterReference + "='" + CharacterName + "' WHERE Username='" + AccountName + "'";
            CommandManager.ExecuteNonQuery(CharacterReferenceQuery, "Updating " + AccountName + "s table to reference that the new character " + CharacterName + " belongs to them");

            //Define and execute new queries/commands for inserting a new blank entry into each of the Inventory, Equipments and Actionbars tables for this newly created character
            string InventoryQuery = "INSERT INTO inventories(CharacterName) VALUE('" + CharacterName + "')";
            CommandManager.ExecuteNonQuery(InventoryQuery, "Inserting a new entry into the Inventory tables for the new character " + CharacterName);
            string EquipmentQuery = "INSERT INTO equipments(CharacterName) VALUE('" + CharacterName + "')";
            CommandManager.ExecuteNonQuery(EquipmentQuery, "Inserting a new entry into the Equipment tables for the new character " + CharacterName);
            string ActionBarQuery = "INSERT INTO actionbars(CharacterName) VALUE('" + CharacterName + "')";
            CommandManager.ExecuteNonQuery(ActionBarQuery, "Inserting a new entry into the ActionBar tables for the new character " + CharacterName);
        }

        //Returns the name of the users character which exists in the given character slot number
        public static string GetCharacterName(string AccountName, int CharacterSlot)
        {
            //Define a new query/command for reading the character names inside someones user account
            string CharacterNameQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            string DatabaseStringName = CharacterSlot == 1 ? "FirstCharacterName" :
                CharacterSlot == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            return CommandManager.ReadStringValue(CharacterNameQuery, DatabaseStringName, "Reading " + AccountName + "s " + DatabaseStringName);
        }

        //Loads all of a characters information from the database
        public static CharacterData GetCharacterData(string CharacterName)
        {
            //Create a new CharacterData object which will store all the data we read out from the database
            CharacterData CharacterData = new CharacterData();

            //Open up this characters table in the database and start reading data from it
            string CharacterDataQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            CharacterData.Account = CommandManager.ReadStringValue(CharacterDataQuery, "OwnerAccountName", "Reading " + CharacterName + "s OwnerAccountName");
            CharacterData.Position = CommandManager.ReadVectorValue(CharacterDataQuery, "Reading " + CharacterName + "s world location values");
            CharacterData.Name = CharacterName;
            CharacterData.Experience = CommandManager.ReadIntegerValue(CharacterDataQuery, "ExperiencePoints", "Reading " + CharacterName + "s ExperiencePoints value");
            CharacterData.ExperienceToLevel = CommandManager.ReadIntegerValue(CharacterDataQuery, "ExperienceToLevel", "Reading " + CharacterName + "s ExperienceToLevel value");
            CharacterData.Level = CommandManager.ReadIntegerValue(CharacterDataQuery, "Level", "Reading " + CharacterName + "s Level value");
            CharacterData.IsMale = CommandManager.ReadBooleanValue(CharacterDataQuery, "IsMale", "Reading " + CharacterName + "s IsMale value");

            //Return the final CharacterData object that has now been filled with all the data weve been looking for
            return CharacterData;
        }

        //Backs up the location of a player character into the database
        public static void SaveCharacterLocation(string CharacterName, Vector3 CharacterLocation)
        {
            //Define a new query/command to store the characters updated position values into the database
            string CharacterLocationQuery = "UPDATE characters SET XPosition='" + CharacterLocation.X + "', YPosition='" + CharacterLocation.Y + "', ZPosition='" + CharacterLocation.Z + "' WHERE CharacterName='" + CharacterName + "'";
            CommandManager.ExecuteNonQuery(CharacterLocationQuery, "Backing up " + CharacterName + "s world position values");
        }
    }
}
