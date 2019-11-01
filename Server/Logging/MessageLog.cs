// ================================================================================================================================
// File:        MessageLogger.cs
// Description: Maintains instance of the Serilog.Log.Logger object, used to save all outputted server messages into a local file
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using Serilog;
using MySql.Data.MySqlClient;

namespace Server.Logging
{
    public class MessageLog
    {
        private static bool LoggerInitialized = false;  //Tracks whether the message logger has been setup yet or not

        //Initializes the Logger object with a new .log output file
        private static void Initialize()
        {
            //Use the current system time to create a new filename where debug/crash messages will be saved to for this lifetime of the application
            string LogFileName = "logs//ServerLog" + DateTime.Now.ToString("dd-MM-yyyy-h-mm-tt") + ".txt";

            //Initialize the Logger object with this new filename
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(LogFileName, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            //Note taht the logger has now been initialized
            LoggerInitialized = true;
        }

        //Outputs a new to the console window, and also saves the message into the active .log output file
        public static void Print(string Message)
        {
            //If the logger has yet to be initialized we need to set it up first
            if (!LoggerInitialized)
                Initialize();

            //The logger is now setup (or already has been previously), now we just add the new message to it
            Log.Debug(Message);
        }

        //Outputs MySQL exception information / stack trace when an exception is caught by the application
        public static void Error(MySqlException Error, string Information)
        {
            //Setup the logger if it isnt ready to be used yet
            if (!LoggerInitialized)
                Initialize();

            //Add the error information to the log file
            Log.Error(Error, Information);
        }

        //Saves and closes the Logger current output .log file if its currently active, then shuts down the logger
        public static void Close()
        {
            //If the logger has yet to be initialized then nothing needs to be done here
            if (!LoggerInitialized)
                return;

            //Otherwise we need to save and close the logger, then note that it needs to be reinitialized if it wants to be used again
            Log.CloseAndFlush();
            LoggerInitialized = false;
        }
    }
}
