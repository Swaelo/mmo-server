// ================================================================================================================================
// File:        DatabaseManager.cs
// Description: Allows the server to interact with the local SQL database which is used to store all user account and character info
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using MySql.Data.MySqlClient;
using Server.Logging;

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
            //Create the new connection string we will use to establish a connection to the database server
            string ConnectionString = CreateConnectionString(ServerIP, ServerPort, Username, Password);
            DatabaseConnection = new MySqlConnection(ConnectionString);
            DatabaseConnection.Open();

            //Check to make sure we didnt fail to connect to the database
            if(DatabaseConnection.State != System.Data.ConnectionState.Open)
            {
                MessageLog.Print("ERROR: Failed to connect to the MySQL server, server cannot function without it.");
                return false;
            }

            //Define/Execute a Query/Command telling the server which database we want to be using
            string DatabaseQuery = "USE " + Database;
            MySqlCommand DatabaseCommand = CommandManager.CreateCommand(DatabaseQuery);
            CommandManager.ExecuteNonQuery(DatabaseCommand);

            //Print a message showing that the SQL server connection has been opened correctly
            MessageLog.Print("MySQL Server Connection Established");
            return true;
        }
    }
}
