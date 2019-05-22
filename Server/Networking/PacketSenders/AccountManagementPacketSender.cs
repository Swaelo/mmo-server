using Server.Interface;
using Server.Database;
using Server.Data;

namespace Server.Networking.PacketSenders
{
    public static class AccountManagementPacketSender
    {
        //Replies to a users account registration request letting them know if it succeeded or not
        public static void SendAccountRegistrationReply(int ClientID, bool RegistrationSuccess, string ReplyMessage)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.AccountRegistrationReply);
            Log.OutgoingPacketsWindow.DisplayNewMessage(ClientID + ": AccountManagement.AccountRegistrationReply");

            //Write the rest of the packet data
            QueueWriter.WriteInt(RegistrationSuccess ? 1 : 0);
            QueueWriter.WriteString(ReplyMessage);
        }

        //Replies to a user accounts login request letting them know if it succeeded or not
        public static void SendAccountLoginReply(int ClientID, bool LoginSuccess, string ReplyMessage)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.AccountLoginReply);
            Log.OutgoingPacketsWindow.DisplayNewMessage(ClientID + ": AccountManagement.AccountLoginReply");

            //Write the rest of the packet data
            QueueWriter.WriteInt(LoginSuccess ? 1 : 0);
            QueueWriter.WriteString(ReplyMessage);

            //If the account login was a success write in all of the users character data into the packet at the same time
            if(LoginSuccess)
            {
                //Write the packet type first
                QueueWriter.WriteInt((int)ServerPacketType.CharacterDataReply);

                //Fetch the number of characters this user owns and write the amount into the packet data
                string AccountName = ConnectionManager.ActiveConnections[ClientID].AccountName;
                int CharacterCount = CharactersDatabase.GetCharacterCount(AccountName);
                QueueWriter.WriteInt(CharacterCount);

                //Loop through and write each characters information into the packet data
                for(int i = 0; i < CharacterCount; i++)
                {
                    //Grab each characters name and data set as we iterate through the list
                    string CharacterName = CharactersDatabase.GetCharacterName(AccountName, i + 1);
                    CharacterData CharacterData = CharactersDatabase.GetCharacterData(CharacterName);

                    //Write all of the characters information
                    QueueWriter.WriteString(CharacterData.Account);
                    QueueWriter.WriteString(CharacterData.Name);
                    QueueWriter.WriteVector3(CharacterData.Position);
                    QueueWriter.WriteInt(CharacterData.Level);
                    QueueWriter.WriteInt(CharacterData.IsMale ? 1 : 0);
                }
            }
        }

        //Replies to a users character creation request
        public static void SendCharacterCreationReply(int ClientID, bool CreationSuccess, string ReplyMessage)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.CharacterCreationReply);
            Log.OutgoingPacketsWindow.DisplayNewMessage(ClientID + ": AccountManagement.CharacterCreationReply");

            //Write the rest of the packet data
            QueueWriter.WriteInt(CreationSuccess ? 1 : 0);
            QueueWriter.WriteString(ReplyMessage);
        }

        //Sends a client all the data regarding every character existing under their account name
        public static void SendCharacterData(int ClientID, string AccountName)
        {
            //Fetch the packet writer, write the packet type, and log a window message
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.CharacterDataReply);
            Log.OutgoingPacketsWindow.DisplayNewMessage(ClientID + ": AccountManagement.CharacterDataRequest");

            //Get the number of characters in the users account, write that number into the packet
            int CharacterCount = CharactersDatabase.GetCharacterCount(AccountName);
            QueueWriter.WriteInt(CharacterCount);

            //Loop through and write into the packet the information about each character existing under this users account
            for(int i = 0; i < CharacterCount; i++)
            {
                //Grab the next characters name, then use the name to get the characters information
                string CharacterName = CharactersDatabase.GetCharacterName(AccountName, i + 1);
                CharacterData CharactersData = CharactersDatabase.GetCharacterData(CharacterName);

                //Write all the characters data into the packet
                QueueWriter.WriteString(CharactersData.Account);
                QueueWriter.WriteString(CharactersData.Name);
                QueueWriter.WriteVector3(CharactersData.Position);
                QueueWriter.WriteInt(CharactersData.Level);
                QueueWriter.WriteInt(CharactersData.IsMale ? 1 : 0);
            }
        }
    }
}
