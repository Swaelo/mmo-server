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
using Server.Data;
using Server.Database;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Interface
{
    public class CommandInput
    {
        private TextBuilder TextBuilder; //Used by the Renderer the draw the text to the UI
        private string PreInput = "Command Input: ";   //Text shown to show where the user can type commands into
        private string MessageInput = "";    //Current contents of the command input field
        private InputControls Controls; //Input Controls definitions

        public bool InputEnabled = false;   //Input is only read for this window if the field is enabled

        private bool BackSpaceHeld = false; //Tracks when the BackSpace is being held for continuous backspacing
        private float BackSpaceCoolDown = 0.025f; //How often to perform a backspace when its being held down
        private float NextBackSpace = 0.025f;   //How long until the next backspace is to be performed

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

                //Check for comma's, periods and - symbols
                if (Controls.Comma.WasTriggered(Input))
                    MessageInput += Shift ? "<" : ",";
                if (Controls.Period.WasTriggered(Input))
                    MessageInput += Shift ? ">" : ".";
                if (Controls.Minus.WasTriggered(Input))
                    MessageInput += Shift ? "_" : "-";

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
        public void Render(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font FontType)
        {
            Renderer.TextBatcher.Write(TextBuilder.Clear().Append(PreInput + MessageInput), Position, FontSize, FontColor, FontType);
        }

        //Try using the current contents of the command input field to execute a new command
        private void TryExecute()
        {
            //Do nothing if there is no content in the input field
            if (MessageInput == "")
                return;

            //Split the string up from its spaces, seperating the command key from its arguments
            string[] InputSplit = MessageInput.Split(" ");

            //Check if input can be used for one command after another until one can be performed
            if (CanShowCommands(InputSplit))    //Show Available Commands
                Reset();
            else if (CanServerShutdown(InputSplit)) //Perform Server Shutdown
                TryServerShutdown(InputSplit);
            else if (CanKickPlayer(InputSplit)) //Kick Player From Server
                TryKickPlayer(InputSplit);
            else if (CanCharacterInfoSearch(InputSplit))    //Display Characters Information
                TryCharacterInfoSearch(InputSplit);
            else if (CanAccountInfoSearch(InputSplit))  //Display Accounts Information
                TryAccountInfoSearch(InputSplit);
            else if (CanSetAllCharactersIntegerValue(InputSplit))   //Set some Integer value for all Characters in the database
                TrySetAllCharactersIntegerValue(InputSplit);
            else if (CanSetAllCharactersPositions(InputSplit))  //Set position value for all characters in the database
                TrySetAllCharactersPositions(InputSplit);
            else if (CanSetAllCharactersRotations(InputSplit))  //Set the rotation value for all characters in the database
                TrySetAllCharactersRotations(InputSplit);
            else if (CanSetAllCharactersCameras(InputSplit))    //Set the camera values for all characters in the database
                TrySetAllCharactersCameras(InputSplit);
            else if (CanPurgeDatabase(InputSplit))  //Purge all entries from the database
                TryPurgeDatabase(InputSplit);
            else if (CanKillPlayer(InputSplit)) //Kill one of the ingame player characters
                TryKillPlayer(InputSplit);
            else if (CanRevivePlayer(InputSplit))   //Revive one of the ingame player characters
                TryRevivePlayer(InputSplit);

            Reset();
        }

        //Checks input can be used for showing all the available commands
        //Example: commands
        private bool CanShowCommands(string[] Input)
        {
            if(Input[0] == "commands")
            {
                MessageLog.Print("commands1: Shows the first page of available server commands.");
                MessageLog.Print("commands2: Shows the second page of available server commands.");
                return true;
            }
            else if(Input[0] == "commands1")
            {
                string Pre1 = "---Commands 1---";
                string Shutdown = "Server Shutdown: shutdown";
                string KickPlayer = "Kick Player: kick charactername";
                string CharacterInfo = "Show Character Info: characterinfo charactername";
                string AccountInfo = "Show Account Info: accountinfo accountname";
                string SetAllInteger = "Set All Characters Integer Value: setallcharactersinteger integername integervalue";
                string SetAllPositions = "Set All Characters Positions: setallcharacterspositions xposition yposition zposition";

                MessageLog.Print(Pre1);
                MessageLog.Print(Shutdown);
                MessageLog.Print(KickPlayer);
                MessageLog.Print(CharacterInfo);
                MessageLog.Print(AccountInfo);
                MessageLog.Print(SetAllInteger);
                MessageLog.Print(SetAllPositions);

                return true;
            }
            else if(Input[0] == "commands2")
            {
                string Pre2 = "---Commands 2---";
                string SetAllRotations = "Set All Characters Rotations: setallcharactersrotations xrotation yrotation zrotation wrotation";
                string SetAllCameras = "Set All Characters Cameras: setallcharacterscameras zoom xrotation yrotation";
                string PurgeDatabase = "Remove every account and character information from the database: purgedatabase";
                string KillPlayer = "Kill Player: kill charactername";
                string RevivePlayer = "Revive Player: revive charactername";

                MessageLog.Print(Pre2);
                MessageLog.Print(SetAllRotations);
                MessageLog.Print(SetAllCameras);
                MessageLog.Print(PurgeDatabase);
                MessageLog.Print(KillPlayer);
                MessageLog.Print(RevivePlayer);

                return true;
            }

            return false;
        }

        //Checks input can be used for performing kill player command
        private bool CanKillPlayer(string[] Input)
        {
            //Check argument count
            if (Input.Length != 2)
                return false;
            //Check command key
            if (Input[0] != "kill")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing revive player command
        private bool CanRevivePlayer(string[] Input)
        {
            if (Input.Length != 2)
                return false;
            if (Input[0] != "revive")
                return false;
            return true;
        }

        //Checks input can be used for performing server shutdown
        //Example: shutdown
        private bool CanServerShutdown(string[] Input)
        {
            //Check argument count
            if (Input.Length != 1)
                return false;
            //Check command key
            if (Input[0] != "shutdown")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing player kick
        //Example: kick charactername
        private bool CanKickPlayer(string[] Input)
        {
            //Check argument count
            if (Input.Length != 2)
                return false;
            //Check command key
            if (Input[0] != "kick")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing character info lookup
        //Example: characterinfo charactername
        private bool CanCharacterInfoSearch(string[] Input)
        {
            //Check argument count
            if (Input.Length != 2)
                return false;
            //Check command key
            if (Input[0] != "characterinfo")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing account info lookup
        //Example: accountinfo accountname
        private bool CanAccountInfoSearch(string[] Input)
        {
            //Check argument count
            if (Input.Length != 2)
                return false;
            //Check command key
            if (Input[0] != "accountinfo")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing setallcharactersintegervalue
        //Example: setallcharactersinteger variablename variablevalue
        private bool CanSetAllCharactersIntegerValue(string[] Input)
        {
            //Check argument count
            if (Input.Length != 3)
                return false;
            //Check command key
            if (Input[0] != "setallcharactersinteger")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing setallcharacterspositions
        //Example: setallcharacterspositions xposition yposition zposition
        private bool CanSetAllCharactersPositions(string[] Input)
        {
            //Check argument count
            if (Input.Length != 4)
                return false;
            //Check command key
            if (Input[0] != "setallcharacterspositions")
                return false;
            //Input valid
            return true;
        }

        //Checks input can be used for performing setallcharactersrotations
        //Example: setallcharactersrotations xrotation yrotation zrotation wrotation
        private bool CanSetAllCharactersRotations(string[] Input)
        {
            //Check argument count
            if (Input.Length != 5)
                return false;
            //Check command key
            if (Input[0] != "setallcharactersrotations")
                return false;
            //input valid
            return true;
        }

        //Checks input can be used for performing setallcharacterscamera
        //Example: setallcharacterscamera zoom xrotation yrotation
        private bool CanSetAllCharactersCameras(string[] Input)
        {
            //Check argument count
            if (Input.Length != 4)
                return false;
            //Check command key
            if (Input[0] != "setallcharacterscameras")
                return false;
            //input is valid
            return true;
        }

        //Checks input can be used for performing a database purge
        //Example: purgedatabase
        private bool CanPurgeDatabase(string[] Input)
        {
            //Check argument length
            if (Input.Length != 1)
                return false;
            //Check command key
            if (Input[0] != "purgedatabase")
                return false;
            //input is valid
            return true;
        }

        //Tries using the command arguments for performing a server shutdown
        private void TryServerShutdown(string[] Input)
        {
            //Log what is happening here
            MessageLog.Print("Server shutting down...");

            //Get a list of all ingame clients who are logged in and playing right now
            List<ClientConnection> ActiveClients = ClientSubsetFinder.GetInGameClients();

            //Loop through all the active players and backup their data
            foreach (ClientConnection ActiveClient in ActiveClients)
                CharactersDatabase.SaveCharacterData(ActiveClient.Character);

            //Close and save the current log file
            MessageLog.Close();

            //Close the application
            Program.ApplicationWindow.Close();
        }

        //Tries using the command arguments for killing one of the player characters
        private void TryKillPlayer(string[] Input)
        {
            string CharacterName = Input[1];
            if(!CharactersDatabase.DoesCharacterExist(CharacterName))
            {
                MessageLog.Print("That character doesnt exist, cant kill them.");
                return;
            }
            ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(CharacterName);
            if(Client == null)
            {
                MessageLog.Print("That character is not ingame right now, cant kill them.");
                return;
            }
            //Make sure the character is still alive
            if(!Client.Character.IsAlive)
            {
                MessageLog.Print("That character is already dead, cant kill them.");
                return;
            }
            MessageLog.Print("Killing " + CharacterName + "...");
            Client.Character.IsAlive = false;
            Client.RemovePhysicsBody(Program.World.WorldSimulation);
            CombatPacketSenders.SendLocalPlayerDead(Client.NetworkID);
            foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(Client.NetworkID))
                CombatPacketSenders.SendRemotePlayerDead(OtherClient.NetworkID, Client.Character.Name);
        }
        private void TryRevivePlayer(string[] Input)
        {
            string CharacterName = Input[1];
            if(!CharactersDatabase.DoesCharacterExist(CharacterName))
            {
                MessageLog.Print("That character doesnt exist, cant revive them.");
                return;
            }
            ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(CharacterName);
            if(Client == null)
            {
                MessageLog.Print("That character is not ingame right now, cant revive them.");
                return;
            }
            if(Client.Character.IsAlive)
            {
                MessageLog.Print("That character is not dead, cant revive them.");
                return;
            }
            MessageLog.Print("Reviving " + CharacterName + "...");
            Client.Character.IsAlive = true;
            Client.Character.SetDefaultValues();
            CombatPacketSenders.SendLocalPlayerRespawn(Client.NetworkID, Client.Character);
            foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(Client.NetworkID))
                CombatPacketSenders.SendRemotePlayerRespawn(OtherClient.NetworkID, Client.Character);
        }

        //Tries using the command arguments for performing a player kick
        private void TryKickPlayer(string[] Input)
        {
            //Get the characters name
            string CharacterName = Input[1];

            //Make sure the character exists
            if(!CharactersDatabase.DoesCharacterExist(CharacterName))
            {
                MessageLog.Print("That character doesnt exist, cant kick them.");
                return;
            }

            //Get the client who this character belongs to
            ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(CharacterName);

            //If the client couldnt be found then the character isnt logged in currently
            if(Client == null)
            {
                MessageLog.Print("That character is not in the game right now, cant kick them.");
                return;
            }

            //Show that the player is being kicked
            MessageLog.Print("Kicking " + CharacterName + " from the game...");

            //Tell the client that they have been kicked from the game and mark them to be cleaned up from the game
            SystemPacketSender.SendKickedFromServer(Client.NetworkID);
            Client.ClientDead = true;

            //Tell everyone else to remove the client from their games
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(Client.NetworkID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendRemoveRemotePlayer(OtherClient.NetworkID, Client.Character.Name);
        }

        //Tries using the command arguments for performing a character info search
        private void TryCharacterInfoSearch(string[] Input)
        {
            //Get the characters name
            string CharacterName = Input[1];

            //Make sure the character exists
            if(!CharactersDatabase.DoesCharacterExist(CharacterName))
            {
                //Say the character doesnt exist and exit the function
                MessageLog.Print("No character named " + CharacterName + " exists, couldnt look up their info.");
                return;
            }

            //Characters Data will be stored here once we acquire it
            CharacterData Data;

            //Find the client currently controlling this character
            ClientConnection Client = ClientSubsetFinder.GetClientUsingCharacter(CharacterName);

            //If no client was found then we want to get the characters info from the database
            if (Client == null)
                Data = CharactersDatabase.GetCharacterData(CharacterName);
            //Otherwise we get the currently live data from the client who is currently using the character
            else
                Data = Client.Character;

            //Define some nicely formatted strings containing all the characters data
            string CharacterInfo = CharacterName + " level " + Data.Level + (Data.IsMale ? " male." : "female.") + " with " + Data.CurrentHealth + "/" + Data.MaxHealth + " HP.";
            string CharacterPosition = "Position: " + "(" + Data.Position.X + "," + Data.Position.Y + "," + Data.Position.Z + ").";
            string CharacterRotation = "Rotation: (" + Data.Rotation.X + "," + Data.Rotation.Y + "," + Data.Rotation.Z + "," + Data.Rotation.W + ").";
            string CharacterCamera = "Camera: Zoom:" + Data.CameraZoom + " XRot:" + Data.CameraXRotation + " YRot:" + Data.CameraYRotation + ".";

            //Display all the information to the message window
            MessageLog.Print(CharacterInfo);
            MessageLog.Print(CharacterPosition);
            MessageLog.Print(CharacterRotation);
            MessageLog.Print(CharacterCamera);
        }

        //Tries using the command arguments for performing an account info search
        private void TryAccountInfoSearch(string[] Input)
        {
            //Get the accounts name
            string AccountName = Input[1];

            //Make sure the account exists
            if(!AccountsDatabase.DoesAccountExist(AccountName))
            {
                MessageLog.Print("ERROR: There is no account called " + AccountName + ", no information to display.");
                return;
            }

            //Get the accounts information from the database
            AccountData Data = AccountsDatabase.GetAccountData(AccountName);

            //Define a string display all the accounts info, then display it all in the message window
            string AccountInfo = "ACCOUNT INFO: " + AccountName + " has " +
                Data.CharacterCount + (Data.CharacterCount == 1 ? " character" : " characters");
            switch(Data.CharacterCount)
            {
                case (0):
                    AccountInfo += ".";
                    break;
                case (1):
                    AccountInfo += ", named " + Data.FirstCharacterName;
                    break;
                case (2):
                    AccountInfo += ", named " + Data.FirstCharacterName + " and " + Data.SecondCharacterName;
                    break;
                case (3):
                    AccountInfo += ", named " + Data.FirstCharacterName + ", " + Data.SecondCharacterName + " and " + Data.ThirdCharacterName;
                    break;
            }
            AccountInfo += ".";
            MessageLog.Print(AccountInfo);
        }

        //Tries using the command arguments for performing a setallcharactersintegervalue
        private void TrySetAllCharactersIntegerValue(string[] Input)
        {
            //Seperate the command arguments
            string IntegerName = Input[1];
            int IntegerValue = int.Parse(Input[2]);

            //Log what is happening here
            MessageLog.Print("Setting the value of the " + IntegerName + " integer to " + IntegerValue.ToString() + " in all character tables in the characters database.");

            //Use the arguments to apply the new value to the character tables in the database
            CharactersDatabase.SetAllIntegerValue(IntegerName, IntegerValue);
        }

        //Tries using the command arguments for performing a setallcharacterspositions
        private void TrySetAllCharactersPositions(string[] Input)
        {
            //Seperate the command arguments
            Vector3 Position = new Vector3(float.Parse(Input[1]), float.Parse(Input[2]), float.Parse(Input[3]));

            //Log what is happening here
            MessageLog.Print("Setting the position of all characters in the database to " + Position.ToString());

            //Apply the new position to all characters in the database
            CharactersDatabase.SetAllPositions(Position);
        }

        //Tries using the command arguments for performing a setallcharactersrotations
        private void TrySetAllCharactersRotations(string[] Input)
        {
            //Seperate the command arguments
            Quaternion Rotation = new Quaternion(float.Parse(Input[1]), float.Parse(Input[2]), float.Parse(Input[3]), float.Parse(Input[4]));

            //Log what is happening here
            MessageLog.Print("Setting the rotation of all characters in the database to " + Rotation.ToString());

            //Apply the new rotation to all characters in the database
            CharactersDatabase.SetAllRotations(Rotation);
        }

        //Tries using the command arguments for performing a setallcharacterscameras
        private void TrySetAllCharactersCameras(string[] Input)
        {
            //Seperate the command arguments
            float Zoom = float.Parse(Input[1]);
            float XRot = float.Parse(Input[2]);
            float YRot = float.Parse(Input[3]);

            //Log what is happening here
            MessageLog.Print("Setting the camera of all characters in the database to Zoom:" + Zoom + ", XRot:" + XRot + ", YRot:" + YRot + ".");

            //Apply the new camera settings to all characters in the database
            CharactersDatabase.SetAllCameras(Zoom, XRot, YRot);
        }

        //Tries using the command arguments for performing a database purge
        private void TryPurgeDatabase(string[] Input)
        {
            //Log what is happening here
            MessageLog.Print("Purging all entries from all databases.");

            //Purge all the databases
            AccountsDatabase.PurgeAccounts();
            ActionBarsDatabase.PurgeActionBars();
            CharactersDatabase.PurgeCharacters();
            EquipmentsDatabase.PurgeEquipments();
            InventoriesDatabase.PurgeInventories();
        }
    }
}