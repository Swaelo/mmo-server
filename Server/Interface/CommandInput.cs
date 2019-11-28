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
using Server.Misc;
using Server.Data;
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

        //Events for looking up and viewing character and useraccount data
        public bool CharacterInfoEvent = false;
        public string CharacterInfoName = "";
        public bool AccountInfoEvent = false;
        public string AccountInfoName = "";

        //Events for setting the value of some variable for every character/account in the database to the same value
        public bool SetAllCharacterVariableEvent = false;
        public string SetAllCharacterVariableType = "";
        public string SetAllCharacterVariableName = "";
        public string SetAllCharacterVariableValue = "";
        public bool SetAllAccountVariableEvent = false;
        public string SetAllAccountVariableType = "";
        public string SetAllAccountVariableName = "";
        public string SetAllAccountVariableValue = "";

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

        //Tracks the user tapping or holding down the spacebar
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

        //Spacebar taps backspace characters, holding spacebar continuously backspaces
        private void PerformBackSpace()
        {
            if (MessageInput != "")
                MessageInput = MessageInput.Substring(0, MessageInput.Length - 1);
        }

        //Tracks user tapping and of the letter/number keys
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

        //Draws the current content typed into the command input field
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
                TryKickPlayer(InputSplit) ||
                TryGetCharacterInfo(InputSplit) ||
                TryGetAccountInfo(InputSplit) ||
                TrySetAllAccountVariable(InputSplit) ||
                TrySetAllCharacterVariable(InputSplit)
                ) Reset();
            else
                Reset();
        }

        //Checks if input is valid for setting character/account variable
        private bool TrySetAllAccountVariable(string[] InputSplit)
        {
            if (InputSplit[0] != "setallaccountvariable")
                return false;
            if (InputSplit.Length != 4)
                return false;
            SetAllAccountVariableEvent = true;
            SetAllAccountVariableType = InputSplit[1];
            SetAllAccountVariableName = InputSplit[2];
            SetAllAccountVariableValue = InputSplit[3];
            Reset();
            return true;
        }
        private bool TrySetAllCharacterVariable(string[] InputSplit)
        {
            if (InputSplit[0] != "setallcharactervariable")
                return false;
            if (InputSplit.Length != 4)
                return false;
            SetAllCharacterVariableEvent = true;
            SetAllCharacterVariableType = InputSplit[1];
            SetAllCharacterVariableName = InputSplit[2];
            SetAllCharacterVariableValue = InputSplit[3];
            Reset();
            return true;
        }

        //Checks if input is valid for fetching and displays character/useraccount information
        private bool TryGetCharacterInfo(string[] InputSplit)
        {
            //Confirm command key
            if (InputSplit[0] != "characterinfo")
                return false;

            //Confirm correct argument count
            if (InputSplit.Length != 2)
                return false;

            //Flag the event
            CharacterInfoEvent = true;
            CharacterInfoName = InputSplit[1];

            //reset/exit
            Reset();
            return true;
        }
        private bool TryGetAccountInfo(string[] InputSplit)
        {
            if (InputSplit[0] != "accountinfo")
                return false;
            if (InputSplit.Length != 2)
                return false;
            AccountInfoEvent = true;
            AccountInfoName = InputSplit[1];
            Reset();
            return true;
        }

        //Checks if input is valid for finding a players location
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

        //Checks if input is valid for performing a server shutdown
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

        //Checks if input is valid for kicking a player from the server
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

        //Tries performing any events which have been triggered
        public void TryPerformEvents()
        {
            TryPerformWherePlayer();
            TryPerformShutdown();
            TryPerformKickPlayer();
            TryPerformShowCharacterInfo();
            TryPerformShowAccountInfo();
            TryPerformSetAllAccountVariableValue();
            TryPerformSetAllCharacterVariableValue();
        }

        //Tries using entered information to update the value of some variable in the table of every character/account
        private void TryPerformSetAllAccountVariableValue()
        {
            if(SetAllAccountVariableEvent)
            {
                MessageLog.Print("Attempting to set the value of the " + SetAllAccountVariableName + " variable in all account tables to the value of " + SetAllAccountVariableValue);
                
                //Make sure the variable type specified is valid
                if(!ValidInputCheckers.IsValidVariableType(SetAllAccountVariableType))
                {
                    MessageLog.Print("ERROR: " + SetAllAccountVariableType + " is not a valid variable type that can be used for updating database values automatically.");
                    return;
                }

                //Make sure the variable name is one that exists in the accounts tables
                if(!ValidInputCheckers.IsAccountVariableNameValid(SetAllAccountVariableName))
                {
                    MessageLog.Print("ERROR: " + SetAllAccountVariableName + " is not a valid variable name that can be used for updating accounts table values automatically.");
                    return;
                }

                MessageLog.Print("ERROR: Need to finish implementing the TryPerformSetAllAccountVariableValue function in the CommandInput class.");

                SetAllAccountVariableEvent = false;
            }
        }
        private void TryPerformSetAllCharacterVariableValue()
        {
            if(SetAllCharacterVariableEvent)
            {
                MessageLog.Print("Attempting to set the value of " + SetAllCharacterVariableName + " to " + SetAllCharacterVariableValue + " in all existing character tables.");
                if(!ValidInputCheckers.IsValidVariableType(SetAllCharacterVariableType))
                {
                    MessageLog.Print("ERROR: " + SetAllCharacterVariableType + " is not a valid variable type that can be used for updating database values automatically.");
                    return;
                }
                if(!ValidInputCheckers.IsCharacterVariableNameValid(SetAllCharacterVariableName))
                {
                    MessageLog.Print("ERROR: " + SetAllCharacterVariableName + " is not a valid variable name that can be used for updating characters table values automatically.");
                    return;
                }

                if(SetAllCharacterVariableType != "integer")
                {
                    MessageLog.Print("ERROR: SetAllCharacterVariable only supports Integer type for now.");
                    return;
                }

                //Assume they are trying to update some integer value, as thats all we need for now, the rest can be implemented later when it actually becomes useful
                int IntegerValue = int.Parse(SetAllCharacterVariableValue);
                CharactersDatabase.SetAllIntegerValue(SetAllCharacterVariableName, IntegerValue);

                //Log that the command has been executed and disable the event flag
                MessageLog.Print("Finished updating character database values");
                SetAllCharacterVariableEvent = false;
            }
        }

        //Tries using entered information to perform a player location check
        private void TryPerformWherePlayer()
        {
            if(WhereEvent)
            {
                //Log what is happening here
                MessageLog.Print("Finding " + WhereTarget + "'s location...");

                //Get the client controlling the character we are looking for
                ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(WhereTarget);

                //If the client was unable to be found, the character being searched for isnt in the game right now
                if (Client == null)
                    MessageLog.Print(WhereTarget + " could not be found.");
                //Otherwise, we use that client to find their characters current location then display that
                else
                    MessageLog.Print(WhereTarget + " is located at " + Client.Character.Position.ToString());

                //Disable the event flag
                WhereEvent = false;
            }
        }

        //Tries using entered information to perform a character/useraccount info check
        private void TryPerformShowCharacterInfo()
        {
            //Perform the event whenever it has been flagged
            if (CharacterInfoEvent)
            {
                //Log what is happening here
                MessageLog.Print("Finding " + CharacterInfoName + "s character information...");

                //Make sure the character exists before we try looking up its information
                bool CharacterExists = CharactersDatabase.DoesCharacterExist(CharacterInfoName);
                if(!CharacterExists)
                {
                    MessageLog.Print("ERROR: There is no character who is called " + CharacterInfoName + ", no information to display.");
                    return;
                }

                //Get the characters information from the database
                CharacterData CharactersInformation = CharactersDatabase.GetCharacterData(CharacterInfoName);

                string CharacterInfo = "CHARACTER INFO: " + CharacterInfoName + " is a level " + CharactersInformation.Level + (CharactersInformation.IsMale ? " male" : " female") +
                    " with " + CharactersInformation.CurrentHealth + "/" + CharactersInformation.MaxHealth + " Health Points and is currently located at " +
                    "(" + CharactersInformation.Position.X + "," + CharactersInformation.Position.Y + "," + CharactersInformation.Position.Z + ").";
                MessageLog.Print(CharacterInfo);

                //Disable the event flag now that the event has been performed
                CharacterInfoEvent = false;
            }
        }
        private void TryPerformShowAccountInfo()
        {
            if(AccountInfoEvent)
            {
                MessageLog.Print("Finding " + AccountInfoName + "s account information...");
                bool AccountExists = AccountsDatabase.DoesAccountExist(AccountInfoName);
                if(!AccountExists)
                {
                    MessageLog.Print("ERROR: There is no account with the username " + AccountInfoName + ", no information to display.");
                    return;
                }
                AccountData AccountsInformation = AccountsDatabase.GetAccountData(AccountInfoName);
                string AccountInfo = "ACCOUNT INFO: " + AccountInfoName + " has " +
                    AccountsInformation.CharacterCount + (AccountsInformation.CharacterCount == 1 ? " character" : " characters");
                switch(AccountsInformation.CharacterCount)
                {
                    case (0):
                        AccountInfo += ".";
                        break;
                    case (1):
                        AccountInfo += ", named " + AccountsInformation.FirstCharacterName;
                        break;
                    case (2):
                        AccountInfo += ", named " + AccountsInformation.FirstCharacterName + " and " + AccountsInformation.SecondCharacterName;
                        break;
                    case (3):
                        AccountInfo += ", named " + AccountsInformation.FirstCharacterName + ", " + AccountsInformation.SecondCharacterName + " and " + AccountsInformation.ThirdCharacterName;
                        break;
                }
                AccountInfo += ".";
                MessageLog.Print(AccountInfo);
                AccountInfoEvent = false;
            }
        }

        //Tries using entered information to perform a server shutdown
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
                    CharactersDatabase.SaveCharacterData(ActiveClient.Character);

                //Close and save the current log file properly
                MessageLog.Close();

                //Now everything is backed up, we can shutdown the application
                Program.ApplicationWindow.Close();
            }
        }

        //Tries using entered information to kick a player from the server
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
                    PlayerManagementPacketSender.SendRemoveRemotePlayer(OtherClient.NetworkID, TargetClient.Character.Name);

                //Disable the event flag
                KickPlayerEvent = false;
            }
        }
    }
}