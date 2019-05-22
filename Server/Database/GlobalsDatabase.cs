// ================================================================================================================================
// File:        GlobalsDatabase.cs
// Description: Allows the server to interact with the local SQL database globals table
// ================================================================================================================================

using System;
using MySql.Data.MySqlClient;

namespace Server.Database
{
    class GlobalsDatabase
    {
        //Queries the server to find out what the next ItemID will be for when the ItemManager next adds a new item into the game
        public static int GetNextItemID()
        {
            string ItemIDQuery = "SELECT NextItemID FROM globals";
            MySqlCommand ItemIDCommand = new MySqlCommand(ItemIDQuery, DatabaseManager.DatabaseConnection);
            return Convert.ToInt32(ItemIDCommand.ExecuteScalar());
        }

        //Updates the value in the database for what the next ItemID will be for the ItemManager to add a new item into the game
        public static void SaveNextItemID(int NextItemID)
        {
            string NewItemIDQuery = "UPDATE globals SET NextItemID='" + NextItemID + "'";
            MySqlCommand NewItemIDCommand = new MySqlCommand(NewItemIDQuery, DatabaseManager.DatabaseConnection);
            NewItemIDCommand.ExecuteNonQuery();
        }
    }
}
