// ================================================================================================================================
// File:        MessageDisplayWindow.cs
// Description: Maintains a little window in the UI where messages can be displayed to, helpful for debugging purposes
// ================================================================================================================================

using System.Numerics;
using ContentRenderer;
using ContentRenderer.UI;
using ServerUtilities;

namespace Server.Interface
{
    public class MessageDisplayWindow
    {
        public string WindowName = "";      //This display windows name
        public string[] MessageContents;    //The current contents of each line in the display window

        //Sets up the array of strings each with an empty string value, stored the window name in the class
        public MessageDisplayWindow(string WindowName)
        {
            this.WindowName = WindowName;
            MessageContents = new string[10];
            for (int i = 0; i < 10; i++)
                MessageContents[i] = "";
        }

        //Moves all the previous messages back a line and displays the new message on the first line
        public void DisplayNewMessage(string NewMessage)
        {
            for (int i = 9; i > 0; i--)
                MessageContents[i] = MessageContents[i - 1];

            MessageContents[0] = NewMessage;
        }

        //Renders all the current messages to the server application window
        public void RenderMessages(Renderer renderer, TextBuilder textBuilder, Font font, Vector2 RenderLocation)
        {
            //Define a new vector passed to the render call for each message line in the window, offset after each render
            Vector2 MessageLocation = RenderLocation;

            //Display the window title at the top so the user knows what types of messages these are
            renderer.TextBatcher.Write(textBuilder.Clear().Append("---" + WindowName + "---"), MessageLocation, 16, new Vector3(1), font);
            //Offset the message location so the rest of the messages are displayed below this title
            MessageLocation.Y += 17;

            //Display the rest of the messages in the window
            for(int i = 0; i < 10; i++)
            {
                //Display each message to the window as we loop through the whole list
                renderer.TextBatcher.Write(textBuilder.Clear().Append(MessageContents[i]), MessageLocation, 16, new Vector3(1), font);
                //Offset the render location for the next message in the window
                MessageLocation.Y += 17;
            }
        }
    }
}
