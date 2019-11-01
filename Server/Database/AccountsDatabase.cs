// ================================================================================================================================
// File:        AccountsDatabase.cs
// Description: Allows the server to interact with the local SQL database accounts tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using MySql.Data.MySqlClient;

namespace Server.Database
{
    class AccountsDatabase
    {
        //Checks if the given account name is available for use or if its already been taken by someone else
        public static bool IsAccountNameAvailable(string AccountName)
        {
            //Define a new query for searching the database for the given account name, then use it to create a new command object
            string AccountQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            MySqlCommand AccountCommand = CommandManager.CreateCommand(AccountQuery);
            //Execute the newly created command to check if this account name is still available or not
            return !CommandManager.ExecuteRowCheck(AccountCommand, "Error checking if the account name " + AccountName + " is still available.");
        }

        //Saves a brand new user account into the database
        //NOTE: Assumes this account doesnt already exist and the login credentials provided are valid
        public static void RegisterNewAccount(string AccountName, string AccountPassword)
        {
            //Define a new query for registering a new account into the database with the given username and password, then create a new command object with it
            string RegisterQuery = "INSERT INTO accounts(Username,Password) VALUES('" + AccountName + "','" + AccountPassword + "')";
            MySqlCommand RegisterCommand = CommandManager.CreateCommand(RegisterQuery);
            //Execute the newly created command
            CommandManager.ExecuteNonQuery(RegisterCommand, "Error trying to register a new account into the database, username: " + AccountName + " password: " + AccountPassword);
        }

        //Checks if the account name and password provided are valid login credentials
        //NOTE: Assumes this account already exists
        public static bool IsPasswordCorrect(string AccountName, string AccountPassword)
        {
            //Define a new query for checking if this user has provided the correct password, use it to create a new command
            string PasswordQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "' AND Password='" + AccountPassword + "'";
            MySqlCommand PasswordCommand = CommandManager.CreateCommand(PasswordQuery);
            //Execute the newly created command to check if the given password is correct or not
            return CommandManager.ExecuteRowCheck(PasswordCommand, "Error checking if " + AccountPassword + " is the correct password for the account " + AccountName);
        }
    }
}
