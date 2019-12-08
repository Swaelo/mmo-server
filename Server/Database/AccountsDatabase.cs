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
        /// <summary>
        /// Purges all entries from the accounts database
        /// </summary>
        public static void PurgeAccounts()
        {
            string PurgeQuery = "DELETE FROM accounts";
            CommandManager.ExecuteNonQuery(PurgeQuery, "Purging all entries from the accounts database.");
        }

        /// <summary>
        /// Checks if there is an existing account that uses the given name
        /// </summary>
        /// <param name="AccountName">The account name to check for</param>
        /// <returns></returns>
        public static bool DoesAccountExist(string AccountName)
        {
            return CommandManager.ExecuteRowCheck(
                "SELECT * FROM accounts WHERE Username='" + AccountName + "'",
                "Checking if any account exists with the name " + AccountName);
        }

        /// <summary>
        /// Returns a new AccountData object with all the information about the requested account
        /// </summary>
        /// <param name="AccountName">The name of the account data we are getting</param>
        /// <returns></returns>
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

        /// <summary>
        /// Checks if the given account name is available for use or if its already been taken by someone else
        /// </summary>
        /// <param name="AccountName">The account name to check for</param>
        /// <returns></returns>
        public static bool IsAccountNameAvailable(string AccountName)
        {
            string AccountQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "'";
            return !CommandManager.ExecuteRowCheck(AccountQuery, "Checking if account name is available");
        }

        /// <summary>
        /// Saves a brand new user account into the database
        /// </summary>
        /// <param name="AccountName">The new account name</param>
        /// <param name="AccountPassword">The new accounts password</param>
        public static void RegisterNewAccount(string AccountName, string AccountPassword)
        {
            string RegisterQuery = "INSERT INTO accounts(Username,Password) VALUES('" + AccountName + "','" + AccountPassword + "')";
            CommandManager.ExecuteNonQuery(RegisterQuery, "Registering a new user account");
        }

        /// <summary>
        /// Checks if the account name and password provided are valid login credentials
        /// </summary>
        /// <param name="AccountName">The account name to check</param>
        /// <param name="AccountPassword">The password to check against the account name</param>
        /// <returns></returns>
        public static bool IsPasswordCorrect(string AccountName, string AccountPassword)
        {
            string PasswordQuery = "SELECT * FROM accounts WHERE Username='" + AccountName + "' AND Password='" + AccountPassword + "'";
            return CommandManager.ExecuteRowCheck(PasswordQuery, "Checking is user has provided the correct login password");
        }
    }
}
