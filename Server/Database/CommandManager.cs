// ================================================================================================================================
// File:        CommandManager.cs
// Description: Used to quickly and easily create/execute new MySqlCommands for use with the database, exception handling included
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
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

            //Open the data reader
            MySqlDataReader RowReader = Command.ExecuteReader();
            RowReader.Read();
            bool HasRows = false;

            //Try reading the values from it
            try { HasRows = RowReader.HasRows; }
            catch(MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the reader and return the value
            RowReader.Close();
            return HasRows;
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

        public static int ExecuteStringToInt(MySqlCommand Command, string Name, string Context = "Unknown error reading string to convert into int value")
        {
            //Make sure the right connection is open
            Command.Connection = DatabaseManager.DatabaseConnection;

            //Open the data reader
            MySqlDataReader StringReader = Command.ExecuteReader();
            StringReader.Read();
            int IntValue = 0;

            //Try reading the values from it
            try { IntValue = Convert.ToInt32(StringReader[Name]); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the reader and return the value
            StringReader.Close();
            return IntValue;
        }

        public static bool ExecuteStringToBool(MySqlCommand Command, string Name, string Context = "Unknown error reading string to convert into boolean value")
        {
            //Make sure the connection is open
            Command.Connection = DatabaseManager.DatabaseConnection;

            //Open the data reader
            MySqlDataReader StringReader = Command.ExecuteReader();
            StringReader.Read();
            bool BooleanValue = false;

            //Try reading the values from it
            try { BooleanValue = Convert.ToBoolean(StringReader[Name]); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the reader and return the value
            StringReader.Close();
            return BooleanValue;
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

            //Open the data reader
            MySqlDataReader StringReader = Command.ExecuteReader();
            StringReader.Read();
            string StringValue = "";

            //try reading the values from it
            try { StringValue = StringReader[Name].ToString(); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //close the reader and return the values
            StringReader.Close();
            return StringValue;
        }

        public static Vector3 ExecuteVector3(MySqlCommand Command, string Context = "Unknown error executing vector3 value check command")
        {
            //Make sure the right connection is open
            Command.Connection = DatabaseManager.DatabaseConnection;

            //Open the data reader
            MySqlDataReader VectorReader = Command.ExecuteReader();
            VectorReader.Read();
            Vector3 Vector = new Vector3();

            //Try reading the values from it
            try
            {
                Vector.X = Convert.ToInt64(VectorReader["XPosition"]);
                Vector.Y = Convert.ToInt64(VectorReader["YPosition"]);
                Vector.Z = Convert.ToInt64(VectorReader["ZPosition"]);
            }
            catch(MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the reader and return the value
            VectorReader.Close();
            return Vector;
        }
    }
}
