// ================================================================================================================================
// File:        CommunicationLog.cs
// Description: Maintains a list of the last 10 packets that were sent out and recieved from clients so it can be displayed on the UI
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using ServerUtilities;
using ContentRenderer;
using ContentRenderer.UI;

namespace Server.Logging
{
    public class CommunicationLog
    {
        private static Dictionary<int, Message> OutgoingLog = new Dictionary<int, Message>();    //Dictionary of the last 15 messages that were sent to the outgoing log
        private static Dictionary<int, Message> IncomingLog = new Dictionary<int, Message>();    //Dictionary of the last 15 messages that were send to the incoming log

        //USed to render the communication logs contents to the window UI
        private static TextBuilder OutgoingLogText = new TextBuilder(2048);
        private static TextBuilder IncomingLogText = new TextBuilder(2048);

        private static int NextOutgoingOrderNumber = 0; //Order Number to be assigned to the next messages
        private static int NextIncomingOrderNumber = 0;

        //Store whatever messages was last sent to each log so we can combo when things are sent multiple times in a row
        private static Message PreviousOutgoingMessage = null;
        private static int OutgoingCombo = 0;
        private static Message PreviousIncomingMessage = null;
        private static int IncomingCombo = 0;

        //Returns the current messages being stored in the logs as a list
        public static List<Message> GetOutgoingMessages()
        {
            //Create a new list to store the messages
            List<Message> Messages = new List<Message>();
            //Add all the messages from the dictionary into the list
            foreach (KeyValuePair<int, Message> Message in OutgoingLog)
                Messages.Add(Message.Value);
            //Return the list
            return Messages;
        }
        public static List<Message> GetIncomingMessages()
        {
            List<Message> Messages = new List<Message>();
            foreach (KeyValuePair<int, Message> Message in IncomingLog)
                Messages.Add(Message.Value);
            return Messages;
        }

        //Renders the current contents of each log to the window UI
        public static void RenderOutgoingLog(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            //Display an initial string at the start indicating what is being shown here
            Renderer.TextBatcher.Write(OutgoingLogText.Clear().Append("---Outgoing Packets Log---"), Position, FontSize, FontColor, FontType);
            //Get the current list of messages to be displayed
            List<Message> Messages = GetOutgoingMessages();
            //Offset the Y value before we start drawing the contents of the log
            Position.Y += FontSize * 1.5f;
            //Loop through all the messages in the log
            foreach(Message Message in Messages)
            {
                //Display each message on its own line, then offset the position fo the rendering of the next line
                Renderer.TextBatcher.Write(OutgoingLogText.Clear().Append(Message.MessageContent), Position, FontSize, FontColor, FontType);
                Position.Y += FontSize * 1.2f;
            }
        }
        public static void RenderIncomingLog(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            Renderer.TextBatcher.Write(IncomingLogText.Clear().Append("---Incoming Packets Log---"), Position, FontSize, FontColor, FontType);
            List<Message> Messages = GetIncomingMessages();
            Position.Y += FontSize * 1.5f;
            foreach(Message Message in Messages)
            {
                Renderer.TextBatcher.Write(IncomingLogText.Clear().Append(Message.MessageContent), Position, FontSize, FontColor, FontType);
                Position.Y += FontSize * 1.2f;
            }
        }

        //Stores a new message into the outgoing packet messages log
        public static void LogOut(string Message)
        {
            //Create a new object to store the message
            Message NewMessage = new Message(Message);

            //Check if this message is being repeated
            if(PreviousOutgoingMessage != null && PreviousOutgoingMessage.OriginalMessageContent == Message)
            {
                //Increase the combo counter
                OutgoingCombo++;
                //Update the message as the front of the log showing the amount of repeats its had so far
                OutgoingLog[NextOutgoingOrderNumber].MessageContent = Message + " x" + OutgoingCombo;
            }
            //Otherwise just add the message to the log as normal
            else
            {
                //Reset the combo counter
                OutgoingCombo = 0;
                //Get the new messages order number
                int OrderNumber = ++NextOutgoingOrderNumber;
                //Store the message in the dictionary
                OutgoingLog.Add(OrderNumber, NewMessage);
                //Maintain a maximum amount of 15 messages in the log
                if (OutgoingLog.Count > 15)
                    OutgoingLog.Remove(OrderNumber - 15);
            }

            //Store the new message as the one that was previously sent to the log
            PreviousOutgoingMessage = NewMessage;
        }

        //Stores a new message into the incoming packet messages log
        public static void LogIn(string Message)
        {
            //Create a new object to store the message
            Message NewMessage = new Message(Message);

            //Check if this message is being repeated
            if(PreviousIncomingMessage != null && PreviousIncomingMessage.OriginalMessageContent == Message)
            {
                //Increase the combo counter and update the message at the front of the log
                IncomingCombo++;
                IncomingLog[NextIncomingOrderNumber].MessageContent = Message + " x" + IncomingCombo;
            }
            //Otherwise just add the message to the log as normal
            else
            {
                //Reset the combo counter, get the new order number and use that to store it in the dictionary
                IncomingCombo = 0;
                int OrderNumber = ++NextIncomingOrderNumber;
                IncomingLog.Add(OrderNumber, NewMessage);
                //Maintain a maximum amount of 15 messages in the log
                if (IncomingLog.Count > 15)
                    IncomingLog.Remove(OrderNumber - 15);
            }

            //Store the new message as the one that was previously sent to the log
            PreviousIncomingMessage = NewMessage;
        }
    }
}
