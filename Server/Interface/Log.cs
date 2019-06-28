// ================================================================================================================================
// File:        Log.cs
// Description: Quickly and easily print messages to the chat window from anywhere in the code by typing l.og("hello world");
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

namespace Server.Interface
{
    public class Log
    {
        public static MessageDisplayWindow DebugMessageWindow = new MessageDisplayWindow("Debug Messages");
        public static MessageDisplayWindow NetworkPackets = new MessageDisplayWindow("Network Packets");
        public static MessageDisplayWindow SQLCommands = new MessageDisplayWindow("SQL Commands");

        //Prints a new message to the DebugMessagesDisplayWindow
        public static void PrintDebugMessage(string DebugMessage)
        {
            DebugMessageWindow.DisplayNewMessage(DebugMessage);
        }

        //Prints out each value in an array of integers
        public static void PrintDebugMessage(string DebugMessage, int[] Bits)
        {
            string Message = DebugMessage + ": ";
            for (int i = 0; i < Bits.Length; i++)
                Message += Bits[i].ToString();
            DebugMessageWindow.DisplayNewMessage(Message);
        }

        //Prints a new message to the IncomingPacketsDisplayWindow
        public static void PrintIncomingPacketMessage(string IncomingPacketMessage)
        {
            NetworkPackets.DisplayNewMessage("IN: " + IncomingPacketMessage);
        }

        //Prints a new message to the OutgoingPacketsDisplayWindow
        public static void PrintOutgoingPacketMessage(string OutgoingPacketMessage)
        {
            NetworkPackets.DisplayNewMessage("OUT: " + OutgoingPacketMessage);
        }

        //Displays an SQL command that was executed to the SQLCommands window
        public static void PrintSQLCommand(string CommandQuery)
        {
            SQLCommands.DisplayNewMessage(CommandQuery);
        }
    }
}
