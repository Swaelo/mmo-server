// ================================================================================================================================
// File:        CharactersDatabase.cs
// Description: Allows the server to interact with the local SQL database characters tables
// ================================================================================================================================

using System;
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
            //define the sql query to look up the current table of characters
            string CharactersQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";

            //Execute the command and start reading the characters table to check if this name is being used or not
            MySqlCommand CharactersCommand = new MySqlCommand(CharactersQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader CharacterReader = CharactersCommand.ExecuteReader();

            //Read the table to check if this name is being used or not
            CharacterReader.Read();
            bool CharacterNameAvailable = !CharacterReader.HasRows;
            CharacterReader.Close();

            //Return the final value
            return CharacterNameAvailable;
        }

        //Returns the number of characters that exist under a given user account
        public static int GetCharacterCount(string AccountName)
        {
            //Define the query to check how many characters this user has created so far
            string CharacterCountQuery = "SELECT CharactersCreated FROM accounts WHERE Username='" + AccountName + "'";

            //Execute the command and return the final value
            MySqlCommand CharacterCountCommand = new MySqlCommand(CharacterCountQuery, DatabaseManager.DatabaseConnection);
            return Convert.ToInt32(CharacterCountCommand.ExecuteScalar());
        }

        //Saves a brand new player character and all of its relevant information into the characters database
        public static void SaveNewCharacter(CharacterData NewCharacterData)
        {
            //Insert a new row into the characters table storing this new characters information
            //values are string:OwnerAccountName float:XPosition float:YPosition float:ZPosition string:CharacterName int:ExperiencePoints int:ExperienceToLevel int:Level int:IsMale
            string NewCharacterQuery = "INSERT INTO characters(OwnerAccountName,CharacterName,IsMale) VALUES('" + NewCharacterData.Account + "','" + NewCharacterData.Name + "','" + (NewCharacterData.IsMale ? 1 : 0) + "')";
            MySqlCommand NewCharacterCommand = new MySqlCommand(NewCharacterQuery, DatabaseManager.DatabaseConnection);
            NewCharacterCommand.ExecuteNonQuery();

            //Update the characters account table to store the new number of characters this player has created under their account so far
            int NewCharacterCount = GetCharacterCount(NewCharacterData.Account) + 1;
            string NewCharacterCountQuery = "UPDATE accounts SET CharactersCreated='" + NewCharacterCount + "' WHERE Username='" + NewCharacterData.Account + "'";
            MySqlCommand NewCharacterCountCommand = new MySqlCommand(NewCharacterCountQuery, DatabaseManager.DatabaseConnection);
            NewCharacterCountCommand.ExecuteNonQuery();

            //Store a refence in the users account table to note that this character belongs to them
            string NewCharacterReference = NewCharacterCount == 1 ? "FirstCharacterName" :
                NewCharacterCount == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            string NewCharacterReferenceQuery = "UPDATE accounts SET " + NewCharacterReference + "' WHERE Username='" + NewCharacterData.Account + "'";
            MySqlCommand NewCharacterReferenceCommand = new MySqlCommand(NewCharacterReferenceQuery, DatabaseManager.DatabaseConnection);
            NewCharacterReferenceCommand.ExecuteNonQuery();

            //Next, create a new entry into the inventory database to keep track of this new characters inventory
            string NewInventoryQuery = "INSERT INTO inventories(CharacterName) VALUES('" + NewCharacterData.Name + "')'";
            MySqlCommand NewInventoryCommand = new MySqlCommand(NewInventoryQuery, DatabaseManager.DatabaseConnection);
            NewInventoryCommand.ExecuteNonQuery();
            //As well as a new entry into the equipments database to track what items they have equipped
            string NewEquipmentQuery = "INSERT INTO equipments(CharacterName) VALUES('" + NewCharacterData.Name + "')'";
            MySqlCommand NewEquipmentCommand = new MySqlCommand(NewEquipmentQuery, DatabaseManager.DatabaseConnection);
            NewEquipmentCommand.ExecuteNonQuery();
            //and finally, a new entry into the actionbars database to track what abilities they have equipped
            string NewActionBarQuery = "INSERT INTO actionbars(CharacterName) VALUES('" + NewCharacterData.Name + "')'";
            MySqlCommand NewActionBarCommand = new MySqlCommand(NewActionBarQuery, DatabaseManager.DatabaseConnection);
            NewActionBarCommand.ExecuteNonQuery();
        }

        //Returns the name of the users character which exists in the given character slot number
        public static string GetCharacterName(string AccountName, int CharacterSlot)
        {
            //First open up the accounts database and start reading the names of characters in this users account
            string CharacterNameQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            MySqlCommand CharacterNameCommand = new MySqlCommand(CharacterNameQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader CharacterNameReader = CharacterNameCommand.ExecuteReader();
            CharacterNameReader.Read();

            //Read from this table the name of the character in the given character slot number
            string SlotName = CharacterSlot == 1 ? "FirstCharacterName" :
                CharacterSlot == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            string CharacterName = CharacterNameReader[SlotName].ToString();
            CharacterNameReader.Close();

            //return the final character name value
            return CharacterName;
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
            CharacterDataReader.Read();

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

        //Backs up the location of a player character into the database
        public static void SaveCharacterLocation(string CharacterName, Vector3 CharacterLocation)
        {
            string CharacterLocationQuery = "UPDATE characters SET XPosition='" + CharacterLocation.X + "', YPosition='" + CharacterLocation.Y + "', ZPosition='" + CharacterLocation.Z + "' WHERE CharacterName='" + CharacterName + "'";
            MySqlCommand CharacterLocationCommand = new MySqlCommand(CharacterLocationQuery, DatabaseManager.DatabaseConnection);
            CharacterLocationCommand.ExecuteNonQuery();
        }
    }
}
