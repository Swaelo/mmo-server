// ================================================================================================================================
// File:        Log.cs
// Description: Quickly and easily print messages to the chat window from anywhere in the code by typing l.og("hello world");
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;

namespace Server.Interface
{
    public class Log
    {
        public static MessageDisplayWindow DebugMessageWindow = new MessageDisplayWindow("Debug Messages");

        //Prints a new message to the debug message window
        public static void Chat(string Message, bool PrintToConsole = false)
        {
            //Send the message contents to the debug message window
            DebugMessageWindow.DisplayNewMessage(Message);

            //Also print the message to the console window if we have been asked to
            if (PrintToConsole)
                Console.WriteLine(Message);
        }
    }
}
