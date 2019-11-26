// ================================================================================================================================
// File:        PacketQueue.cs
// Description: Stores a list of outgoing network packets to be sent to their target game clients in the next communication interval
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Logging;

namespace Server.Networking
{
    public static class PacketQueue
    {
        private static float CommunicationInterval = 0.25f; //How often the outgoing packets list will be emptied and transmitted to the target game 
        private static float NextCommunication = 0.25f; //time remaining before the next communication interval occurs
        private static Dictionary<int, List<NetworkPacket>> OutgoingPackets = new Dictionary<int, List<NetworkPacket>>();   //List of outgoing packets for each of the current client connections, each mapped to their network ID

        //Adds a network packet onto one of the clients outgoing packet queues
        public static void QueuePacket(int ClientID, NetworkPacket Packet)
        {
            //Check if this client has an active packet queue list yet
            if (!OutgoingPackets.ContainsKey(ClientID))
            {
                //Create a new packet queue for this client
                List<NetworkPacket> Packets = new List<NetworkPacket>();
                //Add the new packet to their queue
                Packets.Add(Packet);
                //Map this list into the dictioanry by the clients ID number
                OutgoingPackets.Add(ClientID, Packets);
            }
            else
            {
                //Otherwise we just add the new Packet onto the clients already existing packet queue list
                OutgoingPackets[ClientID].Add(Packet);
            }
        }

        //Keeps track of the interval timer, automatically resetting it and sending out all the queued packets each time the timer hits zero
        public static void UpdateQueue(float DeltaTime)
        {
            //Count down the timer until the next communciation event should occur
            NextCommunication -= DeltaTime;

            //Transmit all the outgoing packets to the target game clients and reset the timer whenever it reaches zero
            if (NextCommunication <= 0.0f)
                TransmitPackets();
        }

        //Transmits all the outgoing packets to their target game clients and resets the interval timer
        private static void TransmitPackets()
        {
            //Reset the communication interval timer
            NextCommunication = CommunicationInterval;

            //Loop through the entire dictionary of packet writer lists
            foreach (KeyValuePair<int, List<NetworkPacket>> OutgoingQueue in OutgoingPackets)
            {
                //Grab this clients packet queue
                List<NetworkPacket> PacketList = OutgoingQueue.Value;

                //The data of every packet in this clients queue will be combined together into a single string
                string TotalData = "";

                //Loop through all this clients outgoing packets, appending the data of each packet onto the end of our TotalData string
                foreach (NetworkPacket Packet in PacketList)
                    TotalData += Packet.PacketData;

                //If the final string actually contains some data then we can now transmit it to its target game client
                if (TotalData != "")
                {
                    //Fetch the ClientConnection and make sure we could find them
                    ClientConnection Client = ConnectionManager.GetClientConnection(OutgoingQueue.Key);
                    if (Client == null)
                    {
                        MessageLog.Print("ERROR: Client not found.");
                        continue;
                    }

                    //Send the data to the client
                    Client.SendPacket(TotalData);
                }
            }

            //Now all packets have been transmitted to their clients, we want to reset the entire dictionary
            OutgoingPackets = new Dictionary<int, List<NetworkPacket>>();
        }
    }
}