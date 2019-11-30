// ================================================================================================================================
// File:        AccountsDatabase.cs
// Description: Allows the server to interact with the local SQL database accounts tables
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using MySql.Data.MySqlClient;
using Server.Data;

namespace Server.Database
{
    class AccountsDatabase
    {
        //Purges all entries from the accounts database
        public static void PurgeAccounts()
        {
            string PurgeQuery = "DELETE FROM accounts";
            CommandManager.ExecuteNonQuery(PurgeQuery, "Purging all entries from the accounts database.");
        }

        //Sets some string value in the table of every account in the database
        public static void SetAllStringValue(string VariableName, string VariableValue)
        {
            string UpdateQuery = "UPDATE accounts SET " + VariableName + "='" + VariableValue + "'";
            CommandManager.ExecuteNonQuery(UpdateQuery, "Setting value of " + VariableName + " to " + VariableValue + " in all existing account tables.");
        }

        public static void SetAllIntegerValue(string VariableName, int IntegerValue)
        {
            string UpdateQuery = "UPDATE accounts SET " + VariableName + "='" + IntegerValue + "'";
            CommandManager.ExecuteNonQuery(UpdateQuery, "Setting value of " + VariableName + " to " + IntegerValue + " in all existing account tables.");
        }

        //Checks if there is an existing account that uses the given name
        public static bool DoesAccountExist(string AccountName)
        {
            return CommandManager.ExecuteRowCheck(
                "SELECT * FROM accounts WHERE Username='" + AccountName + "'",
                "Checking if any account exists with the name " + AccountName);
        }

        //Returns a new AccountData object with all the information about the requested account
        public static AccountData GetAccountData(string AccountName)
        {
            //Create a new AccountData object to store all the data being requested
            AccountData AccountData = new AccountData();

            //Fetch all the info about the account from the database and store it in the AccountData object we just created
            string AccountDataQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            AccountData.Username = AccountName;
            AccountData.Password = CommandManager.ReadStringValue(AccountDataQuery, "Password", "Reading " + AccountName + "s account password.");
            AccountData.CharacterCount = CommandManager.ReadIntegerValue(AccountDataQuery, "CharactersCreated", "Reading " + AccountName + "s character count.");
            AccountData.FirstCharacterName = CommandManager.ReadStringValue(AccountDataQuery, "FirstCharacterName", "Reading " + AccountName + "s first character name.");
            AccountData.SecondCharacterName = CommandManager.ReadStringValue(AccountDataQuery, "SecondCharacterName", "Reading " + AccountName + "s second character name.");
            AccountData.ThirdCharacterName = CommandManager.ReadStringValue(AccountDataQuery, "ThirdCharacterName", "Reading " + AccountName + "s third character name.");

            //Return the AccountData object full of all the requested information
            return AccountData;
        }

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
