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
            string AccountQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            return !CommandManager.ExecuteRowCheck(AccountQuery, "Checking if account name is available");
        }

        //Saves a brand new user account into the database
        //NOTE: Assumes this account doesnt already exist and the login credentials provided are valid
        public static void RegisterNewAccount(string AccountName, string AccountPassword)
        {
            string RegisterQuery = "INSERT INTO accounts(Username,Password) VALUES('" + AccountName + "','" + AccountPassword + "')";
            CommandManager.ExecuteNonQuery(RegisterQuery, "Registering a new user account");
        }

        //Checks if the account name and password provided are valid login credentials
        //NOTE: Assumes this account already exists
        public static bool IsPasswordCorrect(string AccountName, string AccountPassword)
        {
            string PasswordQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "' AND Password='" + AccountPassword + "'";
            return CommandManager.ExecuteRowCheck(PasswordQuery, "Checking is user has provided the correct login password");
        }
    }
}
