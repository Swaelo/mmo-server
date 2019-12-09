// ================================================================================================================================
// File:        AccountManagementPacketHandler.cs
// Description: Handles any client packets which are recieved regarding any account management actions that need to be performed
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Data;
using Server.Database;
using Server.Logging;
using Server.Misc;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class AccountManagementPacketHandler
    {
        //Retrives values for an account login request
        public static NetworkPacket GetValuesAccountLoginRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.AccountLoginRequest);
            Packet.WriteString(ReadFrom.ReadString());
            Packet.WriteString(ReadFrom.ReadString());
            return Packet;
        }

        //Handles a users account login request
        public static void HandleAccountLoginRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + "Account Login Request.");

            //Get the username and password the user provided for trying to login with
            string AccountName = Packet.ReadString();
            string AccountPass = Packet.ReadString();

            //Make sure we are still connected to this client
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                //Ignore the request if we cant find this clients network connection
                MessageLog.Print("ERROR: Connection to this client could not be found, no way to reply to their Account Login Request so it has been aborted.");
                return;
            }

            //Make sure there is account that exists with the name that was provided by the user
            if (!AccountsDatabase.DoesAccountExist(AccountName))
            {
                //Reject the request if that account doesnt exist
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "That account doesnt exist.");
                return;
            }

            //Make sure someone else isnt already logged into that account
            if(ConnectionManager.IsAccountLoggedIn(AccountName))
            {
                //Reject the request if the account is already being used
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "That account is already logged in.");
                return;
            }

            //Check if they provided the correct password
            if(!AccountsDatabase.IsPasswordCorrect(AccountName, AccountPass))
            {
                //Reject the request if the password was wrong
                AccountManagementPacketSenders.SendAccountLoginReply(ClientID, false, "The password was incorrect.");
                return;
            }

            //Fetch all of the accounts information from the database and store it with this client
            AccountData Account = AccountsDatabase.GetAccountData(AccountName);
            Client.Account = Account;

            //Grant this users account login request
            MessageLog.Print(ClientID + " logged into the account " + AccountName);
            AccountManagementPacketSenders.SendAccountLoginReply(ClientID, true, "Login Request Granted.");
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesAccountLogoutAlert(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.AccountLogoutAlert);
            return Packet;
        }

        //Handles a users account logout alert
        public static void HandleAccountLogoutAlert(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Account Logout Alert.");

            //Get the client who is logged out
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);

            //Clear them as being logged in to any account
            Client.Account = new AccountData();
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesAccountRegisterRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.AccountRegistrationRequest);
            Packet.WriteString(ReadFrom.ReadString());
            Packet.WriteString(ReadFrom.ReadString());
            return Packet;
        }

        //Handles a users new user account registration request
        public static void HandleAccountRegisterRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Account Registration Request.");

            //Fetch the username and password the client has provided
            string AccountName = Packet.ReadString();
            string AccountPass = Packet.ReadString();

            //Make sure we are still connected to this client
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                //Ignore the request if we cant find this clients network connection
                MessageLog.Print("ERROR: Connection to this client could not be found, no way to reply to their Account Registration Request.");
                return;
            }

            //Make sure this username isnt already taken by someone else
            if(AccountsDatabase.DoesAccountExist(AccountName))
            {
                //Reject the request is the username is already taken
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "That username is already taken.");
                return;
            }

            //Make sure they have provided us with a valid username and password
            if(!ValidInputCheckers.IsValidUsername(AccountName))
            {
                //Reject the request if the username contained any banned characters
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, "The username you provided contained banned characters.");
                return;
            }
            if(!ValidInputCheckers.IsValidUsername(AccountPass))
            {
                //Reject the request if the password contained any banned characters
                AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, false, ("The password you provided contained banned characters."));
                return;
            }

            //Register the new account into the database and tell the client their request has been granted
            AccountsDatabase.RegisterNewAccount(AccountName, AccountPass);
            AccountManagementPacketSenders.SendAccountRegistrationReply(ClientID, true, "Account Registered Successfully.");
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesCharacterDataRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.CharacterDataRequest);
            return Packet;
        }

        //Handles a users character data request
        public static void HandleCharacterDataRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Character Data Request.");

            //Make sure we are still connected to this client
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                //Ignore the request if we arent connected to this client anymore
                MessageLog.Print("ERROR:  Cant find this clients network connection, no way to fullfil their Character Data Request.");
                return;
            }

            //Fulfil the users request
            AccountManagementPacketSenders.SendCharacterDataReply(ClientID, Client.Account.Username);
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesCreateCharacterRequest(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.CharacterCreationRequest);
            Packet.WriteString(ReadFrom.ReadString());
            return Packet;
        }

        //Handles a users character creation request
        public static void HandleCreateCharacterRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Character Creation Request.");

            //Fetch the name that has been provided for the new character
            string CharacterName = Packet.ReadString();

            //Make sure we are still connected to this client
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                //Ignore the request if the connection could not be found
                MessageLog.Print("ERROR: " + ClientID + " network connection could not be found, ignoring their character creation request.");
                return;
            }

            //Make sure they provided a valid character name
            if(!ValidInputCheckers.IsValidCharacterName(CharacterName))
            {
                //Reject the request if the provided character name contained any banned character
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, "Character name provided contained banned characters.");
                return;
            }

            //Make sure the character name isnt already taken
            if(!CharactersDatabase.IsCharacterNameAvailable(CharacterName))
            {
                //Reject the request if the name is already taken
                AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, false, "That character name is already taken.");
                return;
            }

            //Register the new character into the database and then reload this clients account information from the database
            CharactersDatabase.SaveNewCharacter(Client.Account.Username, CharacterName);
            Client.Account = AccountsDatabase.GetAccountData(Client.Account.Username);

            //Tell the client their character creation request has been a success
            AccountManagementPacketSenders.SendCreateCharacterReply(ClientID, true, "Character Created.");
        }
    }
}