// ================================================================================================================================
// File:        AccountManagementPacketSenders.cs
// Description: Formats and delivers network packets to game clients while they are logging into or creating new user accounts
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Data;
using Server.Database;
using Server.Logging;

namespace Server.Networking.PacketSenders
{
    public static class AccountManagementPacketSenders
    {
        /// <summary>
        /// Replies to a clients request to login to a user account
        /// </summary>
        /// <param name="ClientID">The clients network ID</param>
        /// <param name="LoginSuccess">If the login request was a success</param>
        /// <param name="ReplyMessage">Details what happened if the request was denied</param>
        public static void SendAccountLoginReply(int ClientID, bool LoginSuccess, string ReplyMessage)
        {
            CommunicationLog.LogOut(ClientID + " account login reply");

            //Create a new NetworkPacket object to store the data for this account login reply
            NetworkPacket Packet = new NetworkPacket();

            //Write the relevant data values into the network packet
            Packet.WriteType(ServerPacketType.AccountLoginReply);
            Packet.WriteBool(LoginSuccess);
            Packet.WriteString(ReplyMessage);

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Replies to a clients request to register a brand new user account
        /// </summary>
        /// <param name="ClientID">The clients network ID</param>
        /// <param name="RegistrationSuccess">If the account registration request was a success</param>
        /// <param name="ReplyMessage">What went wrong if the request was denied</param>
        public static void SendAccountRegistrationReply(int ClientID, bool RegistrationSuccess, string ReplyMessage)
        {
            CommunicationLog.LogOut(ClientID + " account registration reply");

            //Create a new NetworkPacket object to store the data for this account login reply
            NetworkPacket Packet = new NetworkPacket();

            //Write the relevant data values into the network packet
            Packet.WriteType(ServerPacketType.AccountRegistrationReply);
            Packet.WriteBool(RegistrationSuccess);
            Packet.WriteString(ReplyMessage);

            //Add this packet to the target clients outgoign packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Sends a client a list of characters that they have created under their account
        /// </summary>
        /// <param name="ClientID">The clients network ID</param>
        /// <param name="AccountName">The account the client is logged into</param>
        public static void SendCharacterDataReply(int ClientID, string AccountName)
        {
            //Log what is happening here
            CommunicationLog.LogOut(ClientID + " Character Data Reply.");

            //Make sure we are still connected to this client
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                //Cancel the reply if the clients connection couldnt be found
                MessageLog.Print("ERROR: Clients network connection couldnt not be found, Character Data Reply cancelled.");
                return;
            }

            //Create a new NetworkPacket object to store all the character data we are going to send to the client
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ServerPacketType.CharacterDataReply);

            //Write the number of characters existing in the users account
            Packet.WriteInt(Client.Account.CharacterCount);

            //Loop through and fetch the data for each of the users characters
            for(int i = 0; i < Client.Account.CharacterCount; i++)
            {
                //Fetch the data of each character in their account
                CharacterData CharacterData = Client.Account.GetCharactersData(i + 1);

                //Write all of the characters information into the packet
                Packet.WriteString(CharacterData.Name);
                Packet.WriteVector3(CharacterData.Position);
                Packet.WriteQuaternion(CharacterData.Rotation);
                Packet.WriteFloat(CharacterData.CameraZoom);
                Packet.WriteFloat(CharacterData.CameraXRotation);
                Packet.WriteFloat(CharacterData.CameraYRotation);
                Packet.WriteInt(CharacterData.CurrentHealth);
                Packet.WriteInt(CharacterData.MaxHealth);
            }

            //Add this packet to the transmission queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// Replies to a clients new character creation request
        /// </summary>
        /// <param name="ClientID">The clients network ID</param>
        /// <param name="CreationSuccess">If the new character creation request was granted</param>
        /// <param name="ReplyMessage">What went wrong if the request was denied</param>
        public static void SendCreateCharacterReply(int ClientID, bool CreationSuccess, string ReplyMessage)
        {
            CommunicationLog.LogOut(ClientID + " character creation reply");

            //Create a new NetworkPacket object to store the data for this account login reply
            NetworkPacket Packet = new NetworkPacket();

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.CreateCharacterReply);
            Packet.WriteBool(CreationSuccess);
            Packet.WriteString(ReplyMessage);

            //Add this packet to the target clients outgoing packets queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}