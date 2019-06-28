// ================================================================================================================================
// File:        CharactersDatabase.cs
// Description: Allows the server to interact with the local SQL database characters tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using MySql.Data.MySqlClient;
using Server.Data;
using Server.Interface;

namespace Server.Database
{
    class CharactersDatabase
    {
        //Checks if the given character name has already been taken by someone else or is free to use
        //NOTE: assumes the character name provided is valid
        public static bool IsCharacterNameAvailable(string CharacterName)
        {
            //Search the characters database for a column with this name
            string Query = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            MySqlDataReader Reader = Command.ExecuteReader();

            //If no column was found the name is available to use
            bool NameAvailable = !Reader.HasRows;
            Reader.Close();
            return NameAvailable;
        }

        //Returns the number of characters that exist under a given user account
        public static int GetCharacterCount(string AccountName)
        {
            //Define the query to check how many characters this user has created so far
            string CharacterCountQuery = "SELECT CharactersCreated FROM accounts WHERE Username='" + AccountName + "'";
            Log.PrintSQLCommand(CharacterCountQuery);

            //Execute the command and return the final value
            MySqlCommand CharacterCountCommand = new MySqlCommand(CharacterCountQuery, DatabaseManager.DatabaseConnection);
            return Convert.ToInt32(CharacterCountCommand.ExecuteScalar());
        }

        //Saves a brand new player character and all of its relevant information into the characters database
        public static void SaveNewCharacter(CharacterData NewCharacterData)
        {
            //Insert a new row into the characters table to store this new characters information
            string Query = "INSERT INTO characters(OwnerAccountName,CharacterName,IsMale) VALUE('" + NewCharacterData.Account + "','" + NewCharacterData.Name + "','" + (NewCharacterData.IsMale ? 1 : 0) + "')";
            Log.PrintSQLCommand(Query);
            MySqlCommand Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();

            //Update the accounts table to reference this users new number of owned characters
            int NewCharacterCount = GetCharacterCount(NewCharacterData.Account) + 1;
            Query = "UPDATE accounts SET CharactersCreated='" + NewCharacterCount + "' WHERE Username='" + NewCharacterData.Account + "'";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();

            //Update the accounts table to reference this character under the owners account details
            string NewCharacterReference = NewCharacterCount == 1 ? "FirstCharacterName" :
                NewCharacterCount == 2 ? "SecondCharacterName" : "ThirdCharacterName";
            Query = "UPDATE accounts SET " + NewCharacterReference + "='" + NewCharacterData.Name + "' WHERE Username='" + NewCharacterData.Account + "'";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();

            //Create new blank entry into the inventory, equipments and actionbars database for the new character
            Query = "INSERT INTO inventories(CharacterName) VALUE('" + NewCharacterData.Name + "')";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
            Query = "INSERT INTO equipments(CharacterName) VALUE('" + NewCharacterData.Name + "')";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
            Query = "INSERT INTO actionbars(CharacterName) VALUE('" + NewCharacterData.Name + "')";
            Log.PrintSQLCommand(Query);
            Command = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
            Command.ExecuteNonQuery();
        }

        //Returns the name of the users character which exists in the given character slot number
        public static string GetCharacterName(string AccountName, int CharacterSlot)
        {
            //First open up the accounts database and start reading the names of characters in this users account
            string CharacterNameQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            Log.PrintSQLCommand(CharacterNameQuery);
            MySqlCommand CharacterNameCommand = new MySqlCommand(CharacterNameQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader CharacterNameReader = CharacterNameCommand.ExecuteReader();
            //Read from this table the name of the character in the given character slot number
            if(CharacterNameReader.Read())
            {
                string SlotName = CharacterSlot == 1 ? "FirstCharacterName" :
                CharacterSlot == 2 ? "SecondCharacterName" : "ThirdCharacterName";
                string CharacterName = CharacterNameReader[SlotName].ToString();
                CharacterNameReader.Close();

                //return the final character name value
                return CharacterName;
            }

            Log.PrintDebugMessage("CharactersDatabase.GetCharacterName Error reading characters name, returning empty string.");
            return "";
        }

        //Loads all of a characters information from the database
        public static CharacterData GetCharacterData(string CharacterName)
        {
            //Create a new CharacterData object to store all the data we are going to retrieve from the database
            CharacterData CharacterData = new CharacterData();

            //First open up this characters table and start reading all the data from it
            string CharacterDataQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(CharacterDataQuery);
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

            Log.PrintDebugMessage("CharactersDatabase.GetCharacterData Error reading character data, returning null.");
            return null;
        }

        //Backs up the location of a player character into the database
        public static void SaveCharacterLocation(string CharacterName, Vector3 CharacterLocation)
        {
            string CharacterLocationQuery = "UPDATE characters SET XPosition='" + CharacterLocation.X + "', YPosition='" + CharacterLocation.Y + "', ZPosition='" + CharacterLocation.Z + "' WHERE CharacterName='" + CharacterName + "'";
            Log.PrintSQLCommand(CharacterLocationQuery);
            MySqlCommand CharacterLocationCommand = new MySqlCommand(CharacterLocationQuery, DatabaseManager.DatabaseConnection);
            CharacterLocationCommand.ExecuteNonQuery();
        }
    }
}
