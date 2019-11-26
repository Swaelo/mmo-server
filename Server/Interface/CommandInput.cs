// ================================================================================================================================
// File:        CommandInput.cs
// Description: Allows user to type messages into the server application window to execute custom commands during runtime
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using ServerUtilities;
using ContentRenderer;
using ContentRenderer.UI;
using Server.Logic;
using Server.Logging;

namespace Server.Interface
{
    public class CommandInput
    {
        private TextBuilder TextBuilder; //Used by the Renderer the draw the text to the UI
        private string PreInput = "CMD: ";   //Text shown to show where the user can type commands into
        private string MessageInput = "";    //Current contents of the command input field
        private Vector2 UIPosition = new Vector2(10, 425);  //Window Position where the CMD Input Field will be drawn to the UI
        private InputControls Controls; //Input Controls definitions

        public bool InputEnabled = false;   //Input is only read for this window if the field is enabled

        private bool BackSpaceHeld = false; //Tracks when the BackSpace is being held for continuous backspacing
        private float BackSpaceCoolDown = 0.025f; //How often to perform a backspace when its being held down
        private float NextBackSpace = 0.025f;   //How long until the next backspace is to be performed

        //Whenever the Enter key is pressed, and input is detected as a valid command input, it simply activates one of these flags
        //These are then checked by the GameWorld Update function, and commands are executed on its end, before it resets the flags
        public bool ShutdownEvent = false;

        //Where event is for looking up the location of some character
        public bool WhereEvent = false;
        public string WhereTarget = "";

        //Relocate event is for moving a character to a new location in the game world
        public bool RelocateToPlayerEvent = false;
        public bool RelocateToPositionEvent = false;
        public string RelocateTarget = "";
        public string RelocatePlayerDestination = "";
        public Vector3 RelocatePositionDestination = Vector3.Zero;

        //Event for updating the location of all characters inside the database to a new location
        public bool RelocateAllEvent = false;
        public Vector3 RelocateAllLocation = Vector3.Zero;

        public void Initialize()
        {
            TextBuilder = new TextBuilder(512);
            Controls = InputControls.Default;
        }

        //Read user input, updating contents of the cmd input field accordingly
        public void Update(Input Input, float DeltaTime)
        {
            //CMD Input Field is enabled by pressing the enter key
            if (!InputEnabled && Controls.Enter.WasTriggered(Input))
                InputEnabled = true;

            //Only allow typing into the field, or trying to execute its contents when the field is active
            if(InputEnabled)
            {
                //Check if the user is holding shift, for typing uppercase letters
                bool Shift = Controls.LShift.IsDown(Input) || Controls.RShift.IsDown(Input);

                //Check if the user press any of the letter/number keys, adding that onto the command input
                PollLetters(Shift, Input);
                PollNumbers(Shift, Input);

                //Check for spacebar
                if (Controls.Space.WasTriggered(Input))
                    MessageInput += " ";

                //Check for comma's and periods
                if (Controls.Comma.WasTriggered(Input))
                    MessageInput += Shift ? "<" : ",";
                if (Controls.Period.WasTriggered(Input))
                    MessageInput += Shift ? ">" : ".";

                //Check if the user press backspace for removing letters from the end of the command input field
                PollBackSpace(Input, DeltaTime);

                //If the user presses the Enter key, and the command input field is not empty, try executing a command with it
                if (Controls.Enter.WasTriggered(Input))
                    TryExecute();
            }
        }

        //Try using the current contents of the command input field to execute a new command
        private void TryExecute()
        {
            //Do nothing if there is no content in the input field
            if (MessageInput == "")
                return;

            //Check for the server shutdown event
            if (MessageInput == "shutdown")
                ShutdownEvent = true;

            //Try splitting the string from its spaces to check for multi word / argument requiring commands
            string[] InputSplit = MessageInput.Split(" ");

            //If the first word of the input split is where, we want to display the location of a certain character
            if(InputSplit[0] == "where")
            {
                //For the Where function, there must only be a 2nd word in the array, ignore this input if thats not the case
                if(InputSplit.Length != 2)
                {
                    Reset();
                    return;
                }

                //Otherwise, we want to use the 2nd word as the name of the character we want to find the location of
                WhereEvent = true;
                WhereTarget = InputSplit[1];

                Reset();
                return;
            }
            //if the first word of the pslit is relocateall, we want to update the locations of all characters inside the database to the new location typed in
            else if (InputSplit[0] == "relocateall")
            {
                //Only listen with an argument count of 4
                if (InputSplit.Length != 4)
                {
                    MessageLog.Print("Relocateall invalid argument count: " + InputSplit.Length + ", expecting 4");
                    Reset();
                    return;
                }

                //Get the new location where we want to position all characters inside the database
                RelocateAllLocation = new Vector3(float.Parse(InputSplit[1]), float.Parse(InputSplit[2]), float.Parse(InputSplit[3]));
                RelocateAllEvent = true;
            }
            //If the first word of the split is relocate, we want to move one of the characters to a new location in the game world
            else if (InputSplit[0] == "relocate")
            {
                //For the Relocate function, we only listen if there is only 3 total items in the split (CMD, PlayerToMove, PlayerToMoveTo), or 5 items (CMD, PlayerToMove, NewXPos, NewYPos, NewZPos)
                if(InputSplit.Length != 3 && InputSplit.Length != 5)
                {
                    MessageLog.Print("Relocate invalid argument count: " + InputSplit.Length);
                    Reset();
                    return;
                }

                //If the length was 3, we want to move the first character to the location of the second character
                if(InputSplit.Length == 3)
                {
                    RelocateToPlayerEvent = true;
                    RelocateTarget = InputSplit[1];
                    RelocatePlayerDestination = InputSplit[2];
                }
                //If the length was 5, we want to move the character to the vector location that was entered
                else if(InputSplit.Length == 5)
                {
                    RelocateToPositionEvent = true;
                    RelocateTarget = InputSplit[1];
                    RelocatePositionDestination = new Vector3(float.Parse(InputSplit[2]), float.Parse(InputSplit[3]), float.Parse(InputSplit[4]));
                }

                Reset();
                return;
            }

            //Reset the contents of the input field
            Reset();
        }

        //Empties the input field and disables it
        private void Reset()
        {
            MessageInput = "";
            InputEnabled = false;
        }

        private void PollBackSpace(Input Input, float DeltaTime)
        {
            //Check if the user is holding down the backspace key, removing the last letter every 0.1 seconds while they continue holding it
            if (Controls.BackSpace.IsDown(Input))
            {
                //If backspace was already held, reduce the timer until next backspace event
                if (BackSpaceHeld)
                {
                    NextBackSpace -= DeltaTime;

                    //Reset the timer and perform a backspace when it reaches zero
                    if (NextBackSpace <= 0f)
                    {
                        NextBackSpace = BackSpaceCoolDown;
                        PerformBackSpace();
                    }
                }
                //If the backspace wasnt being held, perform an initial backspace, then start the timer for following subsequent backspaces
                else
                {
                    BackSpaceHeld = true;
                    PerformBackSpace();
                    NextBackSpace = BackSpaceCoolDown;
                }
            }
            else
                BackSpaceHeld = false;
        }

        private void PerformBackSpace()
        {
            if (MessageInput != "")
                MessageInput = MessageInput.Substring(0, MessageInput.Length - 1);
        }

        private void PollLetters(bool Shift, Input Input)
        {
            if (Controls.Q.WasTriggered(Input))
                MessageInput += Shift ? "Q" : "q";
            if (Controls.W.WasTriggered(Input))
                MessageInput += Shift ? "W" : "w";
            if (Controls.E.WasTriggered(Input))
                MessageInput += Shift ? "E" : "e";
            if (Controls.R.WasTriggered(Input))
                MessageInput += Shift ? "R" : "r";
            if (Controls.T.WasTriggered(Input))
                MessageInput += Shift ? "T" : "t";
            if (Controls.Y.WasTriggered(Input))
                MessageInput += Shift ? "Y" : "y";
            if (Controls.U.WasTriggered(Input))
                MessageInput += Shift ? "U" : "u";
            if (Controls.I.WasTriggered(Input))
                MessageInput += Shift ? "I" : "i";
            if (Controls.O.WasTriggered(Input))
                MessageInput += Shift ? "O" : "o";
            if (Controls.P.WasTriggered(Input))
                MessageInput += Shift ? "P" : "p";
            if (Controls.A.WasTriggered(Input))
                MessageInput += Shift ? "A" : "a";
            if (Controls.S.WasTriggered(Input))
                MessageInput += Shift ? "S" : "s";
            if (Controls.D.WasTriggered(Input))
                MessageInput += Shift ? "D" : "d";
            if (Controls.F.WasTriggered(Input))
                MessageInput += Shift ? "F" : "f";
            if (Controls.G.WasTriggered(Input))
                MessageInput += Shift ? "G" : "g";
            if (Controls.H.WasTriggered(Input))
                MessageInput += Shift ? "H" : "h";
            if (Controls.J.WasTriggered(Input))
                MessageInput += Shift ? "J" : "j";
            if (Controls.K.WasTriggered(Input))
                MessageInput += Shift ? "K" : "k";
            if (Controls.L.WasTriggered(Input))
                MessageInput += Shift ? "L" : "l";
            if (Controls.Z.WasTriggered(Input))
                MessageInput += Shift ? "Z" : "z";
            if (Controls.X.WasTriggered(Input))
                MessageInput += Shift ? "X" : "x";
            if (Controls.C.WasTriggered(Input))
                MessageInput += Shift ? "C" : "c";
            if (Controls.V.WasTriggered(Input))
                MessageInput += Shift ? "V" : "v";
            if (Controls.B.WasTriggered(Input))
                MessageInput += Shift ? "B" : "b";
            if (Controls.N.WasTriggered(Input))
                MessageInput += Shift ? "N" : "n";
            if (Controls.M.WasTriggered(Input))
                MessageInput += Shift ? "M" : "m";
        }

        private void PollNumbers(bool Shift, Input Input)
        {
            if (Controls.Zero.WasTriggered(Input))
                MessageInput += Shift ? ")" : "0";
            if (Controls.One.WasTriggered(Input))
                MessageInput += Shift ? "!" : "1";
            if (Controls.Two.WasTriggered(Input))
                MessageInput += Shift ? "@" : "2";
            if (Controls.Three.WasTriggered(Input))
                MessageInput += Shift ? "#" : "3";
            if (Controls.Four.WasTriggered(Input))
                MessageInput += Shift ? "$" : "4";
            if (Controls.Five.WasTriggered(Input))
                MessageInput += Shift ? "%" : "5";
            if (Controls.Six.WasTriggered(Input))
                MessageInput += Shift ? "^" : "6";
            if (Controls.Seven.WasTriggered(Input))
                MessageInput += Shift ? "&" : "7";
            if (Controls.Eight.WasTriggered(Input))
                MessageInput += Shift ? "*" : "8";
            if (Controls.Nine.WasTriggered(Input))
                MessageInput += Shift ? "(" : "9";
        }

        public void Render(Renderer Renderer, float TextHeight, Vector3 TextColor, Font TextFont)
        {
            Renderer.TextBatcher.Write(TextBuilder.Clear().Append(PreInput + MessageInput), UIPosition, TextHeight, TextColor, TextFont);
        }
    }
}
