// ================================================================================================================================
// File:        CommunicationLog.cs
// Description: Maintains a list of the last 10 packets that were sent out and recieved from clients so it can be displayed on the UI
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Logging
{
    public class CommunicationLog
    {
        private static bool LogInitialized = false; //Tracks whether the message logger has been setup yet or not
        private static string[] OutgoingPacketMessages; //The last 10 out messages that have been sent to the log
        private static string[] IncomingPacketMessages; //The last 10 in messages that have been sent to the log

        private static string PreviousOutgoingMessage;  //Store whatever message was last sent to each message log
        private static string PreviousIncomingMessage;
        private static int OutgoingMessageCombo = 0;    //Count how many times each the same message has repeatedly sent to each log
        private static int IncomingMessageCombo = 0;

        //Initializes the communication log
        private static void Initialize()
        {
            //Set the out and in message arrays with 10 empty strings each
            OutgoingPacketMessages = new string[10];
            IncomingPacketMessages = new string[10];
            for(int i = 0; i < 10; i++)
            {
                OutgoingPacketMessages[i] = "";
                IncomingPacketMessages[i] = "";
            }

            //NOte that logger is not initialized
            LogInitialized = true;
        }

        //Stores a new message into the outgoing packet messages log
        public static void LogOut(string Message)
        {
            //Setup the logger if its not been initialized yet
            if (!LogInitialized)
                Initialize();

            //Check if this is the same message that was previously sent to the log
            if(Message == PreviousOutgoingMessage)
            {
                //Increase the combo counter
                OutgoingMessageCombo++;
                //Update the message at the front to display the same message, showing the amount of repeats its had
                OutgoingPacketMessages[0] = Message + " x" + OutgoingMessageCombo;
            }
            else
            {
                //Reset the combo counter
                OutgoingMessageCombo = 0;
                //Move all the previous messages back 1 line
                for (int i = 9; i > 0; i--)
                    OutgoingPacketMessages[i] = OutgoingPacketMessages[i - 1];
                //Store the new message in the first line
                OutgoingPacketMessages[0] = Message;
            }

            //Store the new message as what was previously sent into the log
            PreviousOutgoingMessage = Message;
        }

        //Stores a new message into the incoming packet messages log
        public static void LogIn(string Message)
        {
            //Setup the logger if its not been initialized yet
            if (!LogInitialized)
                Initialize();

            //Check if this is the same message that was previosly sent
            if(Message == PreviousIncomingMessage)
            {
                //Increase combo, update front message
                IncomingMessageCombo++;
                IncomingPacketMessages[0] = Message + " x" + IncomingMessageCombo;
            }
            else
            {
                //Reset combo, move previous messages back 1 line, store new message at front
                IncomingMessageCombo = 0;
                for (int i = 9; i > 0; i--)
                    IncomingPacketMessages[i] = IncomingPacketMessages[i - 1];
                IncomingPacketMessages[0] = Message;
            }
            //Store message as previously sent
            PreviousIncomingMessage = Message;
        }

        //Returns the current list of the 10 previous outgoing packet messages which have been sent to the log
        public static string[] GetOutgoingMessages()
        {
            //Setup the logger if its not been initialized yet
            if (!LogInitialized)
                Initialize();

            //Return the list of outgoing packet messages
            return OutgoingPacketMessages;
        }

        //Returns the current list of the 10 previous incoming packet messages which have been sent to the log
        public static string[] GetIncomingMessages()
        {
            //Setup the logger if its not been initialized yet
            if (!LogInitialized)
                Initialize();

            //Return the list of incoming packet messages
            return IncomingPacketMessages;
        }
    }
}
