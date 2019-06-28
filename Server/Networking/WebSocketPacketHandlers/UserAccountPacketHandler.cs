// ================================================================================================================================
// File:        UserAccountPacketHandler.cs
// Description: 
// Author:      Harley Laurie http://www.swaelo.com/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using Server.Interface;
using Server.Database;
using Server.Data;
using Server.Networking.WebSocketPacketSenders;

namespace Server.Networking.WebSocketPacketHandlers
{
    public static class UserAccountPacketHandler
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

        //Handles a users account login request
        public static void HandleAccountLoginRequest(int ClientID, string PacketMessage)
        {
            //Isolate the users account name and password from one another
            string Username = PacketMessage.Substring(0, PacketMessage.IndexOf(' '));
            string Password = PacketMessage.Substring(PacketMessage.IndexOf(' ')+1);

            //Display whats happening in the console
            Log.PrintDebugMessage("Handling account login request: " + Username + " " + Password);

            //First check if this user account exists
            if(AccountsDatabase.IsAccountNameAvailable(Username))
            {
                UserAccountPacketSender.SendAccountLoginReply(ClientID, false, "That account does not exist.");
                return;
            }

            //Make sure someone else isnt already logged into this account
            if(WebSocketConnectionManager.IsAccountLoggedIn(Username))
            {
                UserAccountPacketSender.SendAccountLoginReply(ClientID, false, "Someone else is already logged into this account.");
                return;
            }

            //Check they have provided the correct password for the account they want to login to
            if(!AccountsDatabase.IsPasswordCorrect(Username, Password))
            {
                UserAccountPacketSender.SendAccountLoginReply(ClientID, false, "The password you entered was incorrect.");
                return;
            }

            //Everything looks good, grant the user their account login request
            WebSocketConnectionManager.ActiveConnections[ClientID].AccountName = Username;
            UserAccountPacketSender.SendAccountLoginReply(ClientID, true, "Request granted.");
        }

        //Handles a users account logout notification
        public static void HandleAccountLogoutAlert(int ClientID, string PacketMessage)
        {
            Log.PrintDebugMessage("handling account logout alert");

            //Reset the clients account name value
            WebSocketConnectionManager.ActiveConnections[ClientID].AccountName = "";
        }

        //Handles a users new user account registration request
        public static void HandleAccountRegisterRequest(int ClientID, string PacketMessage)
        {
            //Isolate the users account name and password from one another
            string Username = PacketMessage.Substring(0, PacketMessage.IndexOf(' '));
            string Password = PacketMessage.Substring(PacketMessage.IndexOf(' ')+1);

            //Display whats happening in the console
            Log.PrintDebugMessage("Handle Account Registration: '" + Username + "', '" + Password + "'");

            //Reject this request if the username of password contain any banned characters, or if the username is already taken
            if(!IsValidUsername(Username))
            {
                UserAccountPacketSender.SendAccountRegistationReply(ClientID, false, "Username is invalid.");
                return;
            }
            if(!IsValidUsername(Password))
            {
                UserAccountPacketSender.SendAccountRegistationReply(ClientID, false, "Password is invalid.");
                return;
            }
            if(!AccountsDatabase.IsAccountNameAvailable(Username))
            {
                UserAccountPacketSender.SendAccountRegistationReply(ClientID, false, "Username is already taken.");
                return;
            }

            //If everything looks good then we should register the new account into the database and tell the client it was a success
            AccountsDatabase.RegisterNewAccount(Username, Password);
            UserAccountPacketSender.SendAccountRegistationReply(ClientID, true, "Registration Success.");
        }

        //Handles a users character data request
        public static void HandleCharacterDataRequest(int ClientID, string PacketMessage)
        {
            //Fetch the username this client is logged in to, then use that to send the information they are requesting
            string AccountName = WebSocketConnectionManager.ActiveConnections[ClientID].AccountName;
            UserAccountPacketSender.SendCharacterDataReply(ClientID, AccountName);
        }

        //Handles a users character creation request
        public static void HandleCreateCharacterRequest(int ClientID, string PacketMessage)
        {
            //Get the new character name from the packet data
            string CharacterName = PacketMessage.Substring(0, PacketMessage.IndexOf(' '));

            //Reject the request if the character name has already been taken by someone else
            if(!CharactersDatabase.IsCharacterNameAvailable(CharacterName))
            {
                UserAccountPacketSender.SendCreateCharacterReply(ClientID, false, "Character name is already taken.");
                return;
            }

            //Create a new character data object with all the characters information stored inside
            CharacterData NewCharacterData = new CharacterData();
            NewCharacterData.Account = WebSocketConnectionManager.ActiveConnections[ClientID].AccountName;
            NewCharacterData.Name = CharacterName;
            NewCharacterData.IsMale = true;

            //Register the new characters data into the database and tell the client it was a success
            CharactersDatabase.SaveNewCharacter(NewCharacterData);
            UserAccountPacketSender.SendCreateCharacterReply(ClientID, true, "Character Created!");
        }
    }
}