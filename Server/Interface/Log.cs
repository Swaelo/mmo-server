// ================================================================================================================================
// File:        Log.cs
// Description: Quickly and easily print messages to the chat window from anywhere in the code by typing l.og("hello world");
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Text;
using Server.Scenes;
using Server.Misc;

namespace Server.Interface
{
    public class Log
    {
        public static MessageDisplayWindow DebugMessageWindow = new MessageDisplayWindow("Debug Messages");
        public static MessageDisplayWindow IncomingPacketsWindow = new MessageDisplayWindow("Incoming Packets");
        public static MessageDisplayWindow OutgoingPacketsWindow = new MessageDisplayWindow("Outgoing Packets");

        //Prints a new message to the DebugMessagesDisplayWindow
        public static void PrintDebugMessage(string DebugMessage)
        {
            DebugMessageWindow.DisplayNewMessage(DebugMessage);
        }

        //Prints a new message to the IncomingPacketsDisplayWindow
        public static void PrintIncomingPacketMessage(string IncomingPacketMessage)
        {
            IncomingPacketsWindow.DisplayNewMessage(IncomingPacketMessage);
        }

        //Prints a new message to the OutgoingPacketsDisplayWindow
        public static void PrintOutgoingPacketMessage(string OutgoingPacketMessage)
        {
            OutgoingPacketsWindow.DisplayNewMessage(OutgoingPacketMessage);
        }
    }
}
