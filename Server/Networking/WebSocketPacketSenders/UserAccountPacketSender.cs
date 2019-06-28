// ================================================================================================================================
// File:        UserAccountPacketSender.cs
// Description: Formats and delivers network packets to game clients while they are logging into or creating new user accounts
// Author:      Harley Laurie http://www.swaelo.com/
// ================================================================================================================================

using Server.Interface;
using Server.Database;
using Server.Data;

namespace Server.Networking.WebSocketPacketSenders
{
    public static class UserAccountPacketSender
    {
        //Replies to a users account login request letting them know if it succeeded or not
        public static void SendAccountLoginReply(int ClientID, bool LoginSuccess, string ReplyMessage)
        {
            string PacketData = ((int)ServerPacketType.AccountLoginReply) + " "
                + (LoginSuccess ? "1" : "0") + " "
                + ReplyMessage;
            WebSocketConnectionManager.ActiveConnections[ClientID].SendPacket(PacketData);
        }

        //Replies to a users account registation request, letting them know if it succeeded or not
        public static void SendAccountRegistationReply(int ClientID, bool RegistrationSuccess, string ReplyMessage)
        {
            string PacketData = ((int)ServerPacketType.AccountRegistrationReply) + " "
                + (RegistrationSuccess ? "1" : "0") + " "
                + ReplyMessage;
            WebSocketConnectionManager.ActiveConnections[ClientID].SendPacket(PacketData);
        }

        //Replies to a users character data request, providing them with all the information they are requesting
        public static void SendCharacterDataReply(int ClientID, string AccountName)
        {
            //Start the packet data to be sent back to the client
            string PacketData = ((int)ServerPacketType.CharacterDataReply) + " ";

            //Fetch the number of characters that exists in this users account and write that onto the packet data
            int CharacterCount = CharactersDatabase.GetCharacterCount(AccountName);
            PacketData += CharacterCount + " ";

            //Loop through each of the existing character, writing each ones information onto the packet data
            for(int i = 0; i < CharacterCount; i++)
            {
                //Grab the next characters name, then use that to fetch the rest of its information
                string CharacterName = CharactersDatabase.GetCharacterName(AccountName, i + 1);
                CharacterData Data = CharactersDatabase.GetCharacterData(CharacterName);

                //Write all of the characters information into the packetdata
                PacketData += Data.Account + " ";
                PacketData += Data.Name + " ";
                PacketData += Data.Position + " ";
                PacketData += Data.Level + " ";
                PacketData += (Data.IsMale ? "1" : "0") + " ";
            }

            //Send on the finalized packet data to the client who requested it
            WebSocketConnectionManager.ActiveConnections[ClientID].SendPacket(PacketData);
        }

        //Replies to a users character creation request
        public static void SendCreateCharacterReply(int ClientID, bool CreationSuccess, string ReplyMessage)
        {
            string PacketData = ((int)ServerPacketType.CharacterCreationReply) + " "
                    + (CreationSuccess ? "1" : "0") + " "
                    + ReplyMessage;
            WebSocketConnectionManager.ActiveConnections[ClientID].SendPacket(PacketData);
        }
    }
}