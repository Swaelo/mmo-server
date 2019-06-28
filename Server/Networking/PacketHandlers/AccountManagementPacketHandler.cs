// ================================================================================================================================
// File:        AccountManagementPacketHandlers.cs
// Description: Defines all the packet handlers related to user account management
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using Server.Data;
using Server.Database;
using Server.Interface;
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

        //Allows users to register new accounts into the database
        public static void HandleAccountRegistrationRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": AccountManagement.AccountRegistrationRequest");

            //Extract the required information from the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string AccountName = Reader.ReadString();
            string AccountPass = Reader.ReadString();

            //Reject this request if the username or password contain any banned characters, or if the username is already taken
            if(!IsValidUsername(AccountName))
            {
                AccountManagementPacketSender.SendAccountRegistrationReply(ClientID, false, "username is banned");
                return;
            }
            if(!IsValidUsername(AccountPass))
            {
                AccountManagementPacketSender.SendAccountRegistrationReply(ClientID, false, "password is banned");
                return;
            }
            if(!AccountsDatabase.IsAccountNameAvailable(AccountName))
            {
                AccountManagementPacketSender.SendAccountRegistrationReply(ClientID, false, "username is already taken");
                return;
            }

            //Register the account into the database and tell the client it was a success
            AccountsDatabase.RegisterNewAccount(AccountName, AccountPass);
            AccountManagementPacketSender.SendAccountRegistrationReply(ClientID, true, "account registration success");
        }

        //Allows a user to login to their account
        public static void HandleAccountLoginRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": AccountManagement.AccountLoginRequest");

            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string AccountName = Reader.ReadString();
            string AccountPass = Reader.ReadString();

            //Make sure the user isnt trying to log into an account which doesnt exist
            if (AccountsDatabase.IsAccountNameAvailable(AccountName))
            {
                AccountManagementPacketSender.SendAccountLoginReply(ClientID, false, "account does not exist");
                return;
            }
            //Make sure someone else isnt already logged into this account
            if (ConnectionManager.IsAccountLoggedIn(AccountName))
            {
                AccountManagementPacketSender.SendAccountLoginReply(ClientID, false, "this account is already logged in");
                return;
            }
            //Check that the user has provided the correct password for the account
            if (!AccountsDatabase.IsPasswordCorrect(AccountName, AccountPass))
            {
                AccountManagementPacketSender.SendAccountLoginReply(ClientID, false, "password was incorrect");
                return;
            }

            //After all checks have passed we will finally allow this user to log into their account
            ConnectionManager.ActiveConnections[ClientID].AccountName = AccountName;
            AccountManagementPacketSender.SendAccountLoginReply(ClientID, true, "login success");
        }

        //Allows users to create new characters once logged into their accounts
        public static void HandleCharacterCreationRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": AccountManagement.CharacterCreationRequest");

            //Extract the data from the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Extract all the new character information into a CharacterData object
            CharacterData NewCharacterData = new CharacterData();
            NewCharacterData.Account = Reader.ReadString();
            NewCharacterData.Name = Reader.ReadString();
            NewCharacterData.IsMale = Reader.ReadInt() == 1;

            //Make sure the character name hasnt already been taken by someone else
            if(!CharactersDatabase.IsCharacterNameAvailable(NewCharacterData.Name))
            {
                AccountManagementPacketSender.SendCharacterCreationReply(ClientID, false, "character name already taken");
                return;
            }

            //save the new character data into the database
            CharactersDatabase.SaveNewCharacter(NewCharacterData);
            AccountManagementPacketSender.SendCharacterCreationReply(ClientID, true, "character creation success");
        }

        //Sends a user data about all the created characters after they have logged into their account
        public static void HandleCharacterDataRequest(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": AccountManagement.CharacterDataRequest");

            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string AccountName = Reader.ReadString();
            AccountManagementPacketSender.SendCharacterData(ClientID, AccountName);
        }
    }
}
