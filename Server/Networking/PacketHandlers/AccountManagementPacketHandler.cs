// ================================================================================================================================
// File:        AccountManagementPacketHandler.cs
// Description: Handles any client packets which are recieved regarding any account management actions that need to be performed
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using Server.Database;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class AccountManagementPacketHandler
    {
        //Helper function to check if a given username or password contains any banned characters
        private static bool IsValidUsername(string Username)
        {
            for (int i = 0; i < Username.Length; i++)
            {
                //letters and numbers are allowed
                if (Char.IsLetter(Username[i]) || Char.IsNumber(Username[i]))
                    continue;
                //Dashes, Periods and Underscores are allowed
                if (Username[i] == '-' || Username[i] == '.' || Username[i] == '_')
                    continue;

                //Absolutely anything else is banned
                return false;
            }
            return true;
        }

        //Helper function telling you what banned character was found that made a username invalid
        private static string InvalidUsernameReason(string Username)
        {
            //Loop through all the characters in the username
            for(int i = 0; i < Username.Length; i++)
            {
                //letters and numbers are fine
                if (Char.IsLetter(Username[i]) || Char.IsNumber(Username[i]))
                    continue;
                //Dashes, Periods and Underscores are allowed
                if (Username[i] == '-' || Username[i] == '.' || Username[i] == '_')
                    continue;

                //Absolutely anything else is banned
                return ("contains '" + Username[i] + "'");
            }
            return "";
        }

        //Checks if a new character name is valid or not
        private static bool IsValidName(string CharacterName)
        {
            //Empty names are not allowed
            if (CharacterName == "")
                return false;

            //Spaces are not allowed in character names
            if (CharacterName.Contains(' '))
                return false;

            //Now just run the name through the username checker to detect any other illegal characters
            return IsValidUsername(CharacterName);
        }

        //Returns a message explaining why a character name was rejected
        private static string GetInvalidNameReason(string CharacterName)
        {
            //Check for empty name
            if (CharacterName == "")
                return "Empty names are not allowed";

            //Check for name with spaces
            if (CharacterName.Contains(' '))
                return "Spaces are not allowed in character names";

            //Check each character in the name individually
            for(int i = 0; i < CharacterName.Length; i++)
            {
                //letters and numbers are fine
                if (Char.IsLetter(CharacterName[i]) || Char.IsNumber(CharacterName[i]))
                    continue;
                //dashes, periods and underscores are allowed
                if (CharacterName[i] == '-' || CharacterName[i] == '.' || CharacterName[i] == '_')
                    continue;

                //Absolutely any other characters are banned from being used in character names
                return ("Cannot use '" + CharacterName[i] + "'s in character names");
            }

            return "";
        }

        //Handles a users account login request
        public static void HandleAccountLoginRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Read the data values from the packet reader
            string Username = Packet.ReadString();
            string Password = Packet.ReadString();

            //Check if this user account exists already
            if (AccountsDatabase.IsAccountNameAvailable(Username))
            {
                //Reject the request if the account doesnt even exist
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "That account does not exist.");
                return;
            }

            //Make sure someone else isnt already logged into this account
            if (ConnectionManager.IsAccountLoggedIn(Username))
            {
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "Someone else is already logged into this account.");
                return;
            }

            //Make sure they have provided a matching username and password
            if(!AccountsDatabase.IsPasswordCorrect(Username, Password))
            {
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "The password you entered was incorrect.");
                return;
            }

            //If everything all looks good, then we will grant the users account login request
            ConnectionManager.ActiveConnections[ClientID].AccountName = Username;
            AccountManagementPacketSenders.SendAccountLoginReply(ClientID, true, "Login request granted.");
        }

        //Handles a users account logout notification
        public static void HandleAccountLogoutAlert(int ClientID, ref NetworkPacket Packet)
        {
            //Just reset the value tracking what account this user is logged in to
            ConnectionManager.ActiveConnections[ClientID].AccountName = "";
        }

        //Handles a users new user account registration request
        public static void HandleAccountRegisterRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Fetch the relevant data values from the packet reader
            string Username = Packet.ReadString();
            string Password = Packet.ReadString();

            //Reject this request if the username is already taken, or if the username or password contain any banned characters
            if(!IsValidUsername(Username))
            {
                string InvalidWhy = InvalidUsernameReason(Username);
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "Username " + InvalidWhy);
                return;
            }
            if(!IsValidUsername(Password))
            {
                string InvalidWhy = InvalidUsernameReason(Password);
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "Password " + InvalidWhy);
                return;
            }
            if(!AccountsDatabase.IsAccountNameAvailable(Username))
            {
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "Username is already taken");
                return;
            }

            //All looks good, register the new account into the database and tell the client the registration was a success
            AccountsDatabase.RegisterNewAccount(Username, Password);
            AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, true, "Registration success");
        }

        //Handles a users character data request
        public static void HandleCharacterDataRequest(int ClientID, ref NetworkPacket Packet)
        {
            //We can find this clients logged in account name from their ClientConnection object
            string AccountName = ConnectionManager.ActiveConnections[ClientID].AccountName;

            //Send the requested character data back to the client
            AccountManagementPacketSenders.SendCharacterDataReply(ClientID, AccountName);
        }

        //Handles a users character creation request
        public static void HandleCreateCharacterRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Fetch the desired name for the new character from the packet data
            string CharacterName = Packet.ReadString();

            //Reject the request if an illegal name was provided by the client
            if(!IsValidName(CharacterName))
            {
                string Reason = GetInvalidNameReason(CharacterName);
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, Reason);
                return;
            }

            //Reject the request if this character name has already been taken by someone else
            if(!CharactersDatabase.IsCharacterNameAvailable(CharacterName))
            {
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, "Character name is already taken");
                return;
            }

            //Otherwise we need to register this new character into the database, then tell the client their request was granted
            CharactersDatabase.SaveNewCharacter(ConnectionManager.ActiveConnections[ClientID].AccountName, CharacterName);
            AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, true, "Character Created!");
        }
    }
}