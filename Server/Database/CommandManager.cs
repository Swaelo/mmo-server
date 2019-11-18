// ================================================================================================================================
// File:        CommandManager.cs
// Description: Used to quickly and easily create/execute new MySqlCommands for use with the database, exception handling included
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using MySql.Data.MySqlClient;
using Server.Logging;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Database
{
    public static class CommandManager
    {
        //Used to establish a new connection with the SQL Database Server
        private static string ConnectionString =
                "Server=203.221.43.175;" +
                "Port=3306;" +
                "Database=serverdatabase;" +
                "User=swaelo;" +
                "Password=2beardmore;";

        //Opens a new connection with the SQL Database Server
        private static MySqlConnection OpenConnection()
        {
            MySqlConnection NewConnection = new MySqlConnection(ConnectionString);
            NewConnection.Open();
            return NewConnection;
        }

        //Creates a new command used for executing on the SQL Database Server
        private static MySqlCommand CreateCommand(string CommandQuery, MySqlConnection DatabaseConnection, string Context)
        {
            MySqlCommand NewCommand = null;
            try { NewCommand = new MySqlCommand(CommandQuery, DatabaseConnection); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error creating new command: " + Context); }
            return NewCommand;
        }

        //Creates a new DataReader object for checking what values are stored on the SQL Database Server
        private static MySqlDataReader CreateReader(MySqlCommand Command)
        {
            MySqlDataReader NewReader = Command.ExecuteReader();
            NewReader.Read();
            return NewReader;
        }

        //Uses the given Query to create a new command and run it on the database
        public static void ExecuteNonQuery(string CommandQuery, string Context = "Unknown error executing non query command")
        {
            //Open a new connection and create a new command
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);

            //Execute the command if it was created successfully
            if(Command != null)
            {
                try { Command.ExecuteNonQuery(); }
                catch (MySqlException Exception) { MessageLog.Error(Exception, "Error executing non query command: " + Context); }
            }

            //Close the connection
            Connection.Close();
        }

        //Checks if some table in the database contains a specific row that we are searching for
        public static bool ExecuteRowCheck(string CommandQuery, string Context = "Unknown error executing row check command")
        {
            //Create variable we will flag if the row check is successful
            bool RowCheck = false;

            //Open a new connection, create a new command and use that to open a new datareader
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to check for the rows that were looking for
            try { RowCheck = Reader.HasRows; }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error using reader to perform row check: " + Context); }

            //Close the reader and the sql connection, then return the boolean value
            Reader.Close();
            Connection.Close();
            return RowCheck;
        }

        //Reads out an integer value from the database
        public static int ExecuteScalar(string CommandQuery, string Context = "Unknown error executing scalar value check command")
        {
            //Create variable we will fill with the scalar value when its read from the database
            int ScalarValue = 0;

            //Open a new connection, create a new command
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);

            //Execute the command to read the scalar value if the command was created successfully
            if(Command != null)
            {
                try { ScalarValue = Convert.ToInt32(Command.ExecuteScalar()); }
                catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }
            }

            //Close the database connection and return the final scalar value we are left with
            Connection.Close();
            return ScalarValue;
        }

        //Uses a data reader to read out an integer value from the database
        public static int ReadIntegerValue(string CommandQuery, string IntegerName, string Context = "Unknown error using data reader to get integer value")
        {
            //Create variable we will fill with the integer value when its read from the database
            int IntegerValue = 0;

            //Open a new connection, create a new command then use that to open a new datareader object
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to read out the integer value that were looking for
            try { IntegerValue = Convert.ToInt32(Reader[IntegerName]); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error using data reader to get integer value: " + Context); }

            //Close the reader and the SQL connection, then return the integer value
            Reader.Close();
            Connection.Close();
            return IntegerValue;
        }

        //Uses a data reader object to read out a floating point value from the database
        public static float ReadFloatValue(string CommandQuery, string FloatName, string Context = "Unknown error using data reader to get float value")
        {
            //Create local variable to store the float value
            float FloatValue = 0f;

            //Open a new connection, create a new command then use that to open a new datareader object
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to get the float value that were looking for
            try { FloatValue = Convert.ToSingle(Reader[FloatName]); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error using data reader to get floating point value: " + Context); }

            //Close the reader/SQL connection then return the float value
            Reader.Close();
            Connection.Close();
            return FloatValue;
        }

        //Uses a data reader to read out a boolean value from the database
        public static bool ReadBooleanValue(string CommandQuery, string BooleanName, string Context = "Unknown error using reader to get boolean value")
        {
            //Create variable we will fill with the boolean value when its read from the database
            bool BooleanValue = false;

            //Open a new connection, create a new command then use that to open a new datareader
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to read out the boolean value that were looking for
            try { BooleanValue = Convert.ToBoolean(Reader[BooleanName]); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error using data reader to get boolean value: " + Context); }

            //Close the data reader and the SQL connection, then return the boolean value
            Reader.Close();
            Connection.Close();
            return BooleanValue;
        }

        //Uses a data reader to read out a string value from the database
        public static string ReadStringValue(string CommandQuery, string StringName, string Context = "Unknown error using reader to get string value")
        {
            //Create variable we will fill with the string value when its read from the database
            string StringValue = "";

            //Open a new connection, create a new command and then use that to open a new datareader
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to read out the string value that were looking for
            try { StringValue = Reader[StringName].ToString(); }
            catch (MySqlException Exception) { MessageLog.Error(Exception, "Error using data reader to get string value: " + Context); }

            //Close the data reader and the SQL connection, then return the string value
            Reader.Close();
            Connection.Close();
            return StringValue;
        }

        //Uses a data reader to read out a vector3 value from the database
        public static Vector3 ReadVectorValue(string CommandQuery, string Context = "Unknown error using reader to get vector value")
        {
            //Create variable we will fill with the vector values when they are read from the database
            Vector3 VectorValue = new Vector3();

            //Open a new connection, create a new command then use that to open a new datareader
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Use the reader to read out the vector values that were looking for
            try
            {
                VectorValue.X = Convert.ToSingle(Reader["XPosition"]);
                VectorValue.Y = Convert.ToSingle(Reader["YPosition"]);
                VectorValue.Z = Convert.ToSingle(Reader["ZPosition"]);
            }
            catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the data reader and the SQL connection, then return the vector value
            Reader.Close();
            Connection.Close();
            return VectorValue;
        }

        //Uses a data reader to read out a quaternion value from the database
        public static Quaternion ReadQuaternionValue(string CommandQuery, string Context = "Unknown error using reader to get quaternion value")
        {
            //Create a variable we will fill with the quaternion values when they are read from the databse
            Quaternion QuaternionValue = Quaternion.Identity;

            //Open a new connection with the base, then a datareader to get information from it
            MySqlConnection Connection = OpenConnection();
            MySqlCommand Command = CreateCommand(CommandQuery, Connection, Context);
            MySqlDataReader Reader = CreateReader(Command);

            //Try using the reader to get the quaternion values that were looking for
            try
            {
                QuaternionValue.X = Convert.ToSingle(Reader["XRotation"]);
                QuaternionValue.Y = Convert.ToSingle(Reader["YRotation"]);
                QuaternionValue.Z = Convert.ToSingle(Reader["ZRotation"]);
                QuaternionValue.W = Convert.ToSingle(Reader["WRotation"]);
            }
            catch (MySqlException Exception) { MessageLog.Error(Exception, Context); }

            //Close the data reader, database connection then return the final quaternion values
            Reader.Close();
            Connection.Close();
            return QuaternionValue;
        }
    }
}
