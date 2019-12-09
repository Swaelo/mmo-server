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
        private static List<Message> OutgoingLog = new List<Message>();
        private static List<Message> IncomingLog = new List<Message>();

        //USed to render the communication logs contents to the window UI
        private static TextBuilder OutgoingLogText = new TextBuilder(2048);
        private static TextBuilder IncomingLogText = new TextBuilder(2048);

        private static string PreviousOutMessage = "";
        private static string PreviousInMessage = "";
        private static int OutCombo = 0;
        private static int InCombo = 0;

        //Renders the current contents of each log to the window UI
        public static void RenderOutgoingLog(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            //Create a reversed copy of the outgoing messages log
            List<Message> ReversedOutLog = new List<Message>();
            OutgoingLog.ForEach((Message) => { ReversedOutLog.Add(new Message(Message)); });
            ReversedOutLog.Reverse();

            Renderer.TextBatcher.Write(OutgoingLogText.Clear().Append("---Outgoing Packets Log---"), Position, FontSize, FontColor, FontType);
            Position.Y += FontSize * 1.5f;
            foreach(Message Message in ReversedOutLog)
            {
                Renderer.TextBatcher.Write(OutgoingLogText.Clear().Append(Message.MessageContent), Position, FontSize, FontColor, FontType);
                Position.Y += FontSize * 1.2f;
            }
        }
        public static void RenderIncomingLog(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            List<Message> ReversedInLog = new List<Message>();
            IncomingLog.ForEach((Message) => { ReversedInLog.Add(new Message(Message)); });
            ReversedInLog.Reverse();

            Renderer.TextBatcher.Write(IncomingLogText.Clear().Append("---Incoming Packets Log---"), Position, FontSize, FontColor, FontType);
            Position.Y += FontSize * 1.5f;
            foreach(Message Message in ReversedInLog)
            {
                Renderer.TextBatcher.Write(IncomingLogText.Clear().Append(Message.MessageContent), Position, FontSize, FontColor, FontType);
                Position.Y += FontSize * 1.2f;
            }
        }

        //Stores a new message into the outgoing packet messages log
        public static void LogOut(string Message)
        {
            //Check if this is a repeat of the previous message
            bool Combo = Message == PreviousOutMessage;
            
            //Increase the combo counter if its a repeat
            if(Combo)
            {
                OutCombo++;
                OutgoingLog[OutgoingLog.Count - 1].MessageContent = Message + " x " + OutCombo;
            }
            //Reset the combo counter and add the log as normal otherwise
            else
            {
                OutCombo = 0;
                Message NewMessage = new Message(Message);
                OutgoingLog.Add(NewMessage);
                if (OutgoingLog.Count > 15)
                    OutgoingLog.RemoveAt(0);
                PreviousOutMessage = Message;
            }
        }

        //Stores a new message into the incoming packet messages log
        public static void LogIn(string Message)
        {
            bool Combo = Message == PreviousInMessage;
            if(Combo)
            {
                InCombo++;
                IncomingLog[IncomingLog.Count - 1].MessageContent = Message + " x " + InCombo;
            }
            else
            {
                InCombo = 0;
                Message NewMessage = new Message(Message);
                IncomingLog.Add(NewMessage);
                if (IncomingLog.Count > 15)
                    IncomingLog.RemoveAt(0);
                PreviousInMessage = Message;
            }
        }
    }
}
