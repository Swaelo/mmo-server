// ================================================================================================================================
// File:        AccountsDatabase.cs
// Description: Allows the server to interact with the local SQL database accounts tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using MySql.Data.MySqlClient;
using Server.Interface;

namespace Server.Database
{
    class AccountsDatabase
    {
        //Checks if the given account name is available for use or if its already been taken by someone else
        public static bool IsAccountNameAvailable(string AccountName)
        {
            //Define the query to search for an account with this name in the database
            string AccountQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";

            //Execute the command and start reading from the accounts table
            MySqlCommand AccountCommand = new MySqlCommand(AccountQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader AccountReader = AccountCommand.ExecuteReader();

            //Store the value if the account name exists or not and close the data reader
            bool AccountNameAvailable = !AccountReader.HasRows;
            AccountReader.Close();

            //Return the final value
            return AccountNameAvailable;
        }

        //Saves a brand new user account into the database
        //NOTE: Assumes this account doesnt already exist and the login credentials provided are valid
        public static void RegisterNewAccount(string AccountName, string AccountPassword)
        {
            string RegisterQuery = "INSERT INTO accounts(Username,Password) VALUES('" + AccountName + "','" + AccountPassword + "')";

            MySqlCommand RegisterCommand = new MySqlCommand(RegisterQuery, DatabaseManager.DatabaseConnection);
            RegisterCommand.ExecuteNonQuery();
        }

        //Checks if the account name and password provided are valid login credentials
        //NOTE: Assumes this account already exists
        public static bool IsPasswordCorrect(string AccountName, string AccountPassword)
        {
            //Define the query to check this accounts login credentials
            string PasswordQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "' AND Password='" + AccountPassword + "'";

            //Execute this command to open up the table for this account name
            MySqlCommand PasswordCommand = new MySqlCommand(PasswordQuery, DatabaseManager.DatabaseConnection);
            MySqlDataReader PasswordReader = PasswordCommand.ExecuteReader();

            //Read the table to check if the password provided is correct
            if(PasswordReader.Read())
            {
                bool PasswordMatches = PasswordReader.HasRows;
                PasswordReader.Close();

                //Return the final value
                return PasswordMatches;
            }

            //Print error and close the data reader
            PasswordReader.Close();
            Log.Chat("AccountsDatabase.IsPasswordCorrect Error reading password, returning false.");
            return false;
        }
    }
}
