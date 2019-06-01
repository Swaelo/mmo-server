// ================================================================================================================================
// File:        DatabaseManager.cs
// Description: Allows the server to interact with the local SQL database which is used to store all user account and character info
// ================================================================================================================================

using MySql.Data.MySqlClient;
using Server.Interface;

namespace Server.Database
{
    public static class DatabaseManager
    {
        //Current connection to the database server
        public static MySqlConnection DatabaseConnection;

        //Helps define the connection string used to establish the initial connection to the database server
        private static string CreateConnectionString(string IP, string Port)
        {
            string ConnectionString =
                "Server=" + IP + ";" +
                "Port=" + Port + ";" +
                "Database=;" +
                "User=root;" +
                "Password=Fuckyahoo420;";

            Log.PrintSQLCommand(ConnectionString);

            return ConnectionString;
        }

        //Initializes the connection to the database server
        public static void InitializeDatabaseConnection(string ServerIP, string ServerPort)
        {
            //Open connection to the database server
            DatabaseConnection = new MySqlConnection(CreateConnectionString(ServerIP, ServerPort));
            DatabaseConnection.Open();

            //Tell the database we want to use the gamedatabase
            MySqlCommand DatabaseCommand = new MySqlCommand("USE gameserverdatabase", DatabaseConnection);
            DatabaseCommand.ExecuteNonQuery();
        }
    }
}
