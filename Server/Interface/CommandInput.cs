// ================================================================================================================================
// File:        CommandInput.cs
// Description: Allows user to type messages into the server application window to execute custom commands during runtime
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using System.Collections.Generic;
using ServerUtilities;
using ContentRenderer;
using ContentRenderer.UI;
using Server.Logic;
using Server.Logging;
using Server.Networking;
using Server.Networking.PacketSenders;
using Server.Database;

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

        //Event for kicking a player from the server
        public bool KickPlayerEvent = false;
        public string KickPlayerName = "";

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

        //Try using the current contents of the command input field to execute a new command
        private void TryExecute()
        {
            //Do nothing if there is no content in the input field
            if (MessageInput == "")
                return;

            //Try splitting the string from its spaces to check for multi word / argument requiring commands
            string[] InputSplit = MessageInput.Split(" ");

            if (TryWherePlayer(InputSplit) ||
                TryShutdown(InputSplit) ||
                TryKickPlayer(InputSplit))
                Reset();
            else
                Reset();
        }

        private bool TryWherePlayer(string[] InputSplit)
        {
            //Check for command key
            if (InputSplit[0] != "where")
                return false;

            //Check argument count
            if (InputSplit.Length != 2)
                return false;

            //Flag this event
            WhereEvent = true;
            WhereTarget = InputSplit[1];

            //Reset and exit
            Reset();
            return true;
        }

        private bool TryShutdown(string[] InputSplit)
        {
            //Check for command key
            if (InputSplit[0] != "shutdown")
                return false;

            //Check argument count
            if (InputSplit.Length != 1)
                return false;

            //Flag this event
            ShutdownEvent = true;

            //Reset and exit
            Reset();
            return true;
        }
        
        private bool TryKickPlayer(string[] InputSplit)
        {
            //Check for command key
            if (InputSplit[0] != "kick")
                return false;
            //Check argument count
            if (InputSplit.Length != 2)
                return false;
            //Flag this event
            KickPlayerEvent = true;
            KickPlayerName = InputSplit[1];
            //Reset and exit
            Reset();
            return true;
        }

        public void TryPerformEvents()
        {
            TryPerformWherePlayer();
            TryPerformShutdown();
            TryPerformKickPlayer();
        }

        private void TryPerformWherePlayer()
        {
            if(WhereEvent)
            {
                //Log what is happening here
                MessageLog.Print("Finding " + WhereTarget + "'s location...");

                //Get the client controller the character we are looking for
                ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(WhereTarget);

                //If the client was unable to be found, the character being searched for isnt in the game right now
                if (Client == null)
                    MessageLog.Print(WhereTarget + " could not be found.");
                //Otherwise, we use that client to find their characters current location then display that
                else
                    MessageLog.Print(WhereTarget + " is located at " + Client.CharacterPosition.ToString());

                //Disable the event flag
                WhereEvent = false;
            }
        }

        private void TryPerformShutdown()
        {
            if(ShutdownEvent)
            {
                //Log what is happening here
                MessageLog.Print("Shutting down the server...");

                //Get a list of all ingame clients who are logged in and playing right now
                List<ClientConnection> ActiveClients = ClientSubsetFinder.GetInGameClients();

                //Loop through all the clients and backup all their characters information into the database
                foreach (ClientConnection ActiveClient in ActiveClients)
                    CharactersDatabase.SaveCharacterValues(ActiveClient);

                //Now everything is backed up, we can shutdown the application
                Program.ApplicationWindow.Close();
            }
        }

        private void TryPerformKickPlayer()
        {
            if(KickPlayerEvent)
            {
                //Log what is happening here
                MessageLog.Print("Kicking " + KickPlayerName + " from the server...");

                //Get the client who this character belongs to
                ClientConnection TargetClient = ClientSubsetFinder.GetClientUsingCharacter(KickPlayerName);

                //Check we were able to find the client
                if(TargetClient == null)
                {
                    //Display an error showing this command could not be performed
                    MessageLog.Print("ERROR: Could not find " + KickPlayerName + ", so they couldnt be kicked.");
                    //Disable the event flag and exit out of the function
                    KickPlayerEvent = false;
                    return;
                }

                //Tell the client they have been kicked from the game and mark them as being dead
                SystemPacketSender.SendKickedFromServer(TargetClient.NetworkID);
                TargetClient.ClientDead = true;

                //Tell all other ingame clients to remove this character from their game worlds
                List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(TargetClient.NetworkID);
                foreach (ClientConnection OtherClient in OtherClients)
                    PlayerManagementPacketSender.SendRemoveRemotePlayer(OtherClient.NetworkID, TargetClient.CharacterName);

                //Disable the event flag
                KickPlayerEvent = false;
            }
        }
    }
}