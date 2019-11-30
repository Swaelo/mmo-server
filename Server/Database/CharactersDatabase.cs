// ================================================================================================================================
// File:        CharactersDatabase.cs
// Description: Allows the server to interact with the local SQL database characters tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using MySql.Data.MySqlClient;
using Server.Data;
using Server.Logging;
using Server.Networking;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Database
{
    class CharactersDatabase
    {
        //Sets some integer value in the table of every character in the database
        public static void SetAllIntegerValue(string VariableName, int IntegerValue)
        {
            string UpdateQuery = "UPDATE characters SET " + VariableName + "='" + IntegerValue + "'";
            CommandManager.ExecuteNonQuery(UpdateQuery, "Setting value of " + VariableName + " to " + IntegerValue + " in all existing character tables.");
        }

        //Sets the vector3 position value of all characters in the database
        public static void SetAllPositions(Vector3 Position)
        {
            //Define the SQL query for applying the new position values to the database
            string PositionQuery = "UPDATE characters SET " +
                "XPosition='" + Position.X + "', " +
                "YPosition='" + Position.Y + "', " +
                "ZPosition='" + Position.Z + "'";

            //Pass the query on to be executed in a new SQL command
            CommandManager.ExecuteNonQuery(PositionQuery, "Setting the position of all characters in the database to " + Position.ToString() + ".");
        }

        //Sets the quaternion rotation value of all characters in the database
        public static void SetAllRotations(Quaternion Rotation)
        {
            //Define the SQL query for applying the new rotation values to the database
            string RotationQuery = "UPDATE characters SET " +
                "XRotation='" + Rotation.X + "', " +
                "YRotation='" + Rotation.Y + "', " +
                "ZRotation='" + Rotation.Z + "', " +
                "WRotation='" + Rotation.W + "'";

            //Pass the query on to be executed in a new SQL command
            CommandManager.ExecuteNonQuery(RotationQuery, "Setting the rotation of all characters in the database to " + Rotation.ToString() + ".");
        }

        //Checks if the given character name has already been taken by someone else or is free to use
        //NOTE: assumes the character name provided is valid
        public static bool IsCharacterNameAvailable(string CharacterName)
        {
            //Define query/command for checking if the given character name is still available
            string CharacterNameQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            return !CommandManager.ExecuteRowCheck(CharacterNameQuery, "Checking if the character name " + CharacterName + " is still available");
        }

        //Checks if there is an existing character that goes by the given name
        public static bool DoesCharacterExist(string CharacterName)
        {
            return CommandManager.ExecuteRowCheck(
                "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'",
                "Checking if there exists a character named " + CharacterName + " in the database");
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

            //Now the database has been setup to store this new characters information, we want to set their location onto the spawning platform
            SetCharacterPosition(CharacterName, new Vector3(14.166f, 0.079f, 23.286f));
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

        //Lodas a characters position from the database
        public static Vector3 GetCharactersPosition(string CharacterName)
        {
            string CharacterQuery = "SELECT * FROM characters WHERE CharacterName='" + CharacterName + "'";
            return CommandManager.ReadVectorValue(CharacterQuery, "Reading " + CharacterName + "'s position values.");
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
            CharacterData.Rotation = CommandManager.ReadQuaternionValue(CharacterDataQuery, "Reading " + CharacterName + "s world rotation values");
            CharacterData.Name = CharacterName;
            CharacterData.CameraZoom = CommandManager.ReadFloatValue(CharacterDataQuery, "CameraZoom", "Reading " + CharacterName + "s camera zoom distance value");
            CharacterData.CameraXRotation = CommandManager.ReadFloatValue(CharacterDataQuery, "CameraXRotation", "Reading " + CharacterName + "s camera X rotation value");
            CharacterData.CameraYRotation = CommandManager.ReadFloatValue(CharacterDataQuery, "CameraYRotation", "Reading " + CharacterName + "s camera Y rotation value");
            CharacterData.CurrentHealth = CommandManager.ReadIntegerValue(CharacterDataQuery, "CurrentHealth", "Reading " + CharacterName + "s current health value");
            CharacterData.MaxHealth = CommandManager.ReadIntegerValue(CharacterDataQuery, "MaxHealth", "Reading " + CharacterName + "s maximum health value");
            CharacterData.Experience = CommandManager.ReadIntegerValue(CharacterDataQuery, "ExperiencePoints", "Reading " + CharacterName + "s ExperiencePoints value");
            CharacterData.ExperienceToLevel = CommandManager.ReadIntegerValue(CharacterDataQuery, "ExperienceToLevel", "Reading " + CharacterName + "s ExperienceToLevel value");
            CharacterData.Level = CommandManager.ReadIntegerValue(CharacterDataQuery, "Level", "Reading " + CharacterName + "s Level value");
            CharacterData.IsMale = CommandManager.ReadBooleanValue(CharacterDataQuery, "IsMale", "Reading " + CharacterName + "s IsMale value");

            //Return the final CharacterData object that has now been filled with all the data weve been looking for
            return CharacterData;
        }

        //Backs up all of a characters information into the database
        public static void SaveCharacterData(CharacterData CharacterData)
        {
            //Define a new SQL query to apply all the characters current values
            string SaveDataQuery = "UPDATE characters SET " +
                /*Position*/    "XPosition='" + CharacterData.Position.X + "', YPosition='" + CharacterData.Position.Y + "', ZPosition='" + CharacterData.Position.Z + "'" +
                /*Rotation*/    ", XRotation='" + CharacterData.Rotation.X + "', YRotation='" + CharacterData.Rotation.Y + "', ZRotation='" + CharacterData.Rotation.Z + "', WRotation='" + CharacterData.Rotation.W + "'" +
                /*Camera*/      ", CameraZoom='" + CharacterData.CameraZoom + "', CameraXRotation='" + CharacterData.CameraXRotation + "', CameraYRotation='" + CharacterData.CameraYRotation + "'" +
                /*Health*/      ", CurrentHealth='" + CharacterData.CurrentHealth + "', MaxHealth='" + CharacterData.MaxHealth + "'" +
                /*WHO*/         " WHERE CharacterName='" + CharacterData.Name + "'";

            //Execute the command to update the database with the new values
            CommandManager.ExecuteNonQuery(SaveDataQuery, "Saving " + CharacterData.Name + "'s character data values into the database.");
        }

        private static void SetCharacterPosition(string CharacterName, Vector3 CharacterPosition)
        {
            string UpdateQuery = "UPDATE characters SET XPosition='" + CharacterPosition.X + "', YPosition='" + CharacterPosition.Y + "', ZPosition='" + CharacterPosition.Z + "' WHERE CharacterName='" + CharacterName + "'";
            CommandManager.ExecuteNonQuery(UpdateQuery, "Setting " + CharacterName + "s location in the database to " + CharacterPosition.ToString());
        }

        //Sets the location of all characters inside the database to a new value
        public static void MoveAllCharacters(Vector3 CharacterPosition)
        {
            string UpdateQuery = "UPDATE characters SET XPosition='" + CharacterPosition.X + "', YPosition='" + CharacterPosition.Y + "', ZPosition='" + CharacterPosition.Z + "'";
            CommandManager.ExecuteNonQuery(UpdateQuery, "Setting position of all characters in the database to " + CharacterPosition.ToString());
        }
    }
}
