// ================================================================================================================================
// File:        CommandManager.cs
// Description: Used to quickly and easily create/execute new MySqlCommands for use with the database, exception handling included
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using MySql.Data.MySqlClient;
using Server.Logging;

namespace Server.Database
{
    public static class CommandManager
    {
        /// <summary>
        /// Creates a new command with the given query string, then returns the final MySqlCommand object
        /// </summary>
        /// <param name="Query">The MySQL string query which defines what this command will do</param>
        /// <returns></returns>
        public static MySqlCommand CreateCommand(string Query)
        {
            //Try creating the new command with the given string query
            try
            {
                //Return the new command if its created successfully
                MySqlCommand NewCommand = new MySqlCommand(Query, DatabaseManager.DatabaseConnection);
                return NewCommand;
            }
            catch (MySqlException Exception)
            {
                //Display exception details to the output log and return null if the command failed to be created
                MessageLog.Error(Exception, "Error creating new command");
                return null;
            }
        }

        /// <summary>
        /// Executes the given MySqlCommand
        /// </summary>
        /// <param name="Command">The command you want to execute</param>
        /// <param name="Context">Describe here what you were trying to do by executing this command, this string will be saved into the log files if an exception occurs.</param>
        public static void ExecuteNonQuery(MySqlCommand Command, string Context = "Unknown error executing non query command")
        {
            //Ensure the command is being executed with the correct database connection being reference
            Command.Connection = DatabaseManager.DatabaseConnection;

            try
            {
                Command.ExecuteNonQuery();
            }
            catch(MySqlException Exception)
            {
                MessageLog.Error(Exception, Context);
            }
        }

        /// <summary>
        /// Opens a reader with the given command, checks if it has rows, closes the reader then returns the final result
        /// </summary>
        /// <param name="Command">The command you want to execute</param>
        /// <param name="Context">Describe here what you were trying to do by executing this command, this string will be saved into the log files if an exception occurs.</param>
        /// <returns></returns>
        public static bool ExecuteRowCheck(MySqlCommand Command, string Context = "Unknown error executing row check command")
        {
            //Ensure the command is being executed with the correct database connection being reference
            Command.Connection = DatabaseManager.DatabaseConnection;

            try
            {
                //Open a datareader with the given command object, then check if it has any rows
                MySqlDataReader RowReader = Command.ExecuteReader();
                bool HasRows = RowReader.HasRows;
                //Close the datareader and return the HasRows value
                RowReader.Close();
                return HasRows;
            }
            catch (MySqlException Exception)
            {
                //Add details to the output log what went wrong if we werent able to perform this search correctly
                MessageLog.Error(Exception, Context);
                return false;
            }
        }

        /// <summary>
        /// Extracts the scalar value stored within using the given command object, then returns the final value
        /// </summary>
        /// <param name="Command">The command you want to execute</param>
        /// <param name="Context">Describe here what you were trying to do by executing this command, this string will be saved into the log files if an exception occurs.</param>
        /// <returns></returns>
        public static int ExecuteScalar(MySqlCommand Command, string Context = "Unknown error executing scalar value check command")
        {
            //Ensure the command is being executed with the correct database connection being reference
            Command.Connection = DatabaseManager.DatabaseConnection;

            try
            {
                //Extract, store and return the scalar value from within the database
                int ScalarValue = Convert.ToInt32(Command.ExecuteScalar());
                return ScalarValue;
            }
            catch (MySqlException Exception)
            {
                //Add details to the output log what went wrong if we werent able to read out this value correctly
                MessageLog.Error(Exception, Context);
                return 0;
            }
        }

        /// <summary>
        /// Returns a string value from the database using the given sql command object
        /// </summary>
        /// <param name="Command">The command you want to execute</param>
        /// <param name="Name">The name of the string value that you want to read from the database</param>
        /// <param name="Context">Describe here what you were trying to do by executing this command, this string will be saved into the log files if an exception occurs.</param>
        /// <returns></returns>
        public static string ExecuteString(MySqlCommand Command, string Name, string Context = "Unknown error executing string value check command")
        {
            //Ensure the command is being executed with the correct database connection being reference
            Command.Connection = DatabaseManager.DatabaseConnection;

            try
            {
                //Open a new data reader object, then read out and store the string value that we are looking for
                MySqlDataReader StringReader = Command.ExecuteReader();
                StringReader.Read();
                string StringValue = StringReader[Name].ToString();
                //Close the data reader object and return the string value that was extracted
                StringReader.Close();
                return StringValue;
            }
            catch (MySqlException Exception)
            {
                //Add details to the output log what went wrong if we werent able to read out this value correctly
                MessageLog.Error(Exception, Context);
                return "";
            }
        }
    }
}
