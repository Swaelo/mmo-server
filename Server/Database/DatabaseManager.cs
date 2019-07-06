// ================================================================================================================================
// File:        DatabaseManager.cs
// Description: Allows the server to interact with the local SQL database which is used to store all user account and character info
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using MySql.Data.MySqlClient;

namespace Server.Database
{
    public static class DatabaseManager
    {
        //Current connection to the database server
        public static MySqlConnection DatabaseConnection;

        //Helps define the connection string used to establish the initial connection to the database server
        private static string CreateConnectionString(string IP, int Port, string Username = "root", string Password = "")
        {
            string ConnectionString =
                "Server=" + IP + ";" +
                "Port=" + Port.ToString() + ";" +
                "Database=;" +
                "User=" + Username + ";" +
                "Password=" + Password + ";";

            return ConnectionString;
        }

        //Initializes the connection to the database server
        public static bool InitializeDatabaseConnection(string ServerIP = "localhost", int ServerPort = 3306, string Database = "", string Username = "root", string Password = "")
        {
            //Open connection to the database server
            DatabaseConnection = new MySqlConnection(CreateConnectionString(ServerIP, ServerPort, Username, Password));
            DatabaseConnection.Open();

            //Check if the database connection failed to connect
            if(DatabaseConnection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("Failed to connect to the SQL server, shutting down.");
                return false;
            }

            //Tell the server which database we want to use
            MySqlCommand DatabaseCommand = new MySqlCommand("USE " + Database, DatabaseConnection);
            DatabaseCommand.ExecuteNonQuery();
            return true;
        }
    }
}
