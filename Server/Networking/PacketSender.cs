// ================================================================================================================================
// File:        PacketSender.cs
// Description: Used to queue up instructions which need to be sent to a client in the next game tick, then sends all the instructions
//              to each client when a game tick passes
// ================================================================================================================================

using System.Collections.Generic;

namespace Server.Networking
{
    public static class PacketSender
    {
        //Keep a list of packet writers, one for each client connection used to append each set of instructions until the queue is sent through the network
        private static Dictionary<int, PacketWriter> QueueWriters = new Dictionary<int, PacketWriter>();

        //Sends all packets which have been queued up as ready to go
        public static void SendQueuedPackets()
        {
            //Loop through each active queue writer
            foreach(KeyValuePair<int, PacketWriter> QueueWriter in QueueWriters)
            {
                //Send each packet through to its target network client
                ConnectionManager.SendPacketTo(QueueWriter.Key, QueueWriter.Value.ToArray());
            }

            //Reset the queue writer list
            QueueWriters = new Dictionary<int, PacketWriter>();
        }

        //Returns the current PacketWriter for a clients packet queue, if it doesnt exist a new one is automatically created first then then returned
        public static PacketWriter GetQueueWriter(int ClientID)
        {
            //If there is no queue writer for this client, create one and add it to the dictionary
            if(!QueueWriters.ContainsKey(ClientID))
            {
                PacketWriter NewWriter = new PacketWriter();
                QueueWriters.Add(ClientID, NewWriter);
            }

            //Return the clients queue writer
            return QueueWriters[ClientID];
        }
    }
}
