// ================================================================================================================================
// File:        AccountManagementPacketHandler.cs
// Description: Handles any client packets which are recieved regarding any account management actions that need to be performed
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Database;
using Server.Logging;
using Server.Misc;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class AccountManagementPacketHandler
    {
        //Handles a users account login request
        public static void HandleAccountLoginRequest(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " account login request");

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
            if (!AccountsDatabase.IsPasswordCorrect(Username, Password))
            {
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "The password you entered was incorrect.");
                return;
            }

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if(ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to handle account login request.");
                return;
            }

            //Everything looks good, grant the users account login request and display message showing the account has been logged into
            ClientConnection.AccountName = Username;
            AccountManagementPacketSenders.SendAccountLoginReply(ClientID, true, "Login request granted.");
            MessageLog.Print(Username + " has logged in.");
        }

        //Handles a users new user account registration request
        public static void HandleAccountRegisterRequest(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " account registration request");

            //Fetch the relevant data values from the packet reader
            string Username = Packet.ReadString();
            string Password = Packet.ReadString();

            //Reject this request if the username is already taken, or if the username or password contain any banned characters
            if(!ValidInputCheckers.IsValidUsername(Username))
            {
                string InvalidWhy = ValidInputCheckers.InvalidUsernameReason(Username);
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "Username " + InvalidWhy);
                return;
            }
            if(!ValidInputCheckers.IsValidUsername(Password))
            {
                string InvalidWhy = ValidInputCheckers.InvalidUsernameReason(Password);
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
            CommunicationLog.LogIn(ClientID + " accounts character data request");

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to handle character data request.");
                return;
            }

            //Get the clients account name and use that to send the character data back to them
            string AccountName = ClientConnection.AccountName;
            AccountManagementPacketSenders.SendCharacterDataReply(ClientID, AccountName);
        }

        //Handles a users character creation request
        public static void HandleCreateCharacterRequest(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " characer creation request");

            //Fetch the desired name for the new character from the packet data
            string CharacterName = Packet.ReadString();

            //Reject the request if an illegal name was provided by the client
            if(!ValidInputCheckers.IsValidCharacterName(CharacterName))
            {
                string Reason = ValidInputCheckers.GetInvalidCharacterNameReason(CharacterName);
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, Reason);
                return;
            }

            //Reject the request if this character name has already been taken by someone else
            if(!CharactersDatabase.IsCharacterNameAvailable(CharacterName))
            {
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, "Character name is already taken");
                return;
            }

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to handle character creation request.");
                return;
            }

            //Register this new character into the database, then tell the client their request was granted
            CharactersDatabase.SaveNewCharacter(ClientConnection.AccountName, CharacterName);
            AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, true, "Character Created!");
        }
    }
}