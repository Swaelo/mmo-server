// ================================================================================================================================
// File:        MessageLog.cs
// Description: Maintains instance of the Serilog.Log.Logger object, used to save all outputted server messages into a local file
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Serilog;
using MySql.Data.MySqlClient;
using ServerUtilities;
using ContentRenderer;
using ContentRenderer.UI;

namespace Server.Logging
{
    public class MessageLog
    {
        private static bool LoggerInitialized = false;  //Tracks whether the message logger has been setup yet or not
        private static List<Message> LogMessages = new List<Message>(); //Log of previous messages sent to the log
        public static List<Message> GetMessages() { return LogMessages; }   //Returns the list of previous messages

        //Used to render the message log contents to the window UI
        private static TextBuilder LogText = new TextBuilder(2048);

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

        //Moves everything in LogMessages back 1 line, then stores the new message at the front
        private static void StoreMessage(string Message)
        {
            //Create a new LogMessage object to store the new message that was sent
            Message NewMessage = new Message(Message);

            //Add it to the list of messages
            LogMessages.Add(NewMessage);

            //Maintain a maximum list size of 15
            if (LogMessages.Count > 15)
                LogMessages.RemoveAt(0);
        }

        //Renders all the messages to the window UI
        public static void RenderLog(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            //Create a reversed copy of the previous messages log
            List<Message> ReversedMessages = new List<Message>();
            LogMessages.ForEach((Message) => { ReversedMessages.Add(new Message(Message)); });
            ReversedMessages.Reverse();

            //Loop through and render each message to the UI
            foreach(Message Message in ReversedMessages)
            {
                //Display each message on its own line, then move down to the next line for the next message
                Renderer.TextBatcher.Write(LogText.Clear().Append(Message.MessageContent), Position, FontSize, FontColor, FontType);
                Position.Y += FontSize * 1.2f;
            }
        }

        //Outputs a new to the console window, and also saves the message into the active .log output file
        public static void Print(string Message)
        {
            //If the logger has yet to be initialized we need to set it up first
            if (!LoggerInitialized)
                Initialize();

            //Store the message for window rendering
            StoreMessage(Message);

            //The logger is now setup (or already has been previously), now we just add the new message to it
            Log.Debug(Message);
        }

        //Outputs MySQL exception information / stack trace when an exception is caught by the application
        public static void Error(MySqlException Error, string Information)
        {
            //Setup the logger if it isnt ready to be used yet
            if (!LoggerInitialized)
                Initialize();

            //Store the message for window rendering
            StoreMessage(Error.Message + " " + Information);

            //Add the error information to the log file
            Log.Error(Error, Information);
        }

        //Overload of Error for passing in IOException
        public static void Error(IOException Error, string Information)
        {
            //Setup the logger if it isnt ready yet
            if (!LoggerInitialized)
                Initialize();

            //Store the message for window rendering
            StoreMessage(Error.Message + " " + Information);

            Log.Error(Error, Information);
        }

        //Overload of Error for passing in IndexOutOfRangeException
        public static void Error(IndexOutOfRangeException Error, string Information)
        {
            if (!LoggerInitialized)
                Initialize();

            StoreMessage(Error.Message + " " + Information);

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
