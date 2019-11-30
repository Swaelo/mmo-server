// ================================================================================================================================
// File:        Message.cs
// Description: Stores a message contents and the time that it was sent to the log
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Time;

namespace Server.Logging
{
    public class Message
    {
        public string OriginalMessageContent;   //The initial string value that was sent when this message was created
        public string MessageContent;  //Contents of the message itself

        //Constructor
        public Message(string Content)
        {
            //Store the content and set the time this object was created
            OriginalMessageContent = Content;
            MessageContent = Content;
        }
    }
}
