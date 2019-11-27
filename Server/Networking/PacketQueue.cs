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
        private static float CommunicationInterval = 0.1f; //How often the outgoing packets list will be emptied and transmitted to the target game 
        private static float NextCommunication = 0.1f; //time remaining before the next communication interval occurs
        private static Dictionary<int, List<NetworkPacket>> OutgoingPackets = new Dictionary<int, List<NetworkPacket>>();   //List of outgoing packets for each of the current client connections, each mapped to their network ID
        private static Dictionary<int, List<NetworkPacket>> SecondaryPacketQueue = new Dictionary<int, List<NetworkPacket>>();  //Secondary list of outgoing packets, anything sent to the queue while the main list is currently
        //being used inside the TransmitPackets function is added to this SecondaryPacketQueue for safe keeping until TransmitPackets is complete, which will move anything from here over into the main list after its done
        private static bool MainQueueInUse = false;

        //Adds a network packet onto one of the clients outgoing packet queues
        public static void QueuePacket(int ClientID, NetworkPacket Packet)
        {
            if (MainQueueInUse)
                AddToSecondaryQueue(ClientID, Packet);
            else
                AddToMainQueue(ClientID, Packet);
        }

        //Adds a list of packets to the main queue
        private static void AddToMainQueue(int ClientID, List<NetworkPacket> Packets)
        {
            foreach (NetworkPacket Packet in Packets)
                AddToMainQueue(ClientID, Packet);
        }

        //Adds a packet to the main queue
        private static void AddToMainQueue(int ClientID, NetworkPacket Packet)
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

        //Adds a packet to the secondary queue
        private static void AddToSecondaryQueue(int ClientID, NetworkPacket Packet)
        {
            //Check if this client has an active packet queue list yet
            if (!SecondaryPacketQueue.ContainsKey(ClientID))
            {
                //Create a new packet queue for this client
                List<NetworkPacket> Packets = new List<NetworkPacket>();
                //Add the new packet to their queue
                Packets.Add(Packet);
                //Map this list into the dictioanry by the clients ID number
                SecondaryPacketQueue.Add(ClientID, Packets);
            }
            else
            {
                //Otherwise we just add the new Packet onto the clients already existing packet queue list
                SecondaryPacketQueue[ClientID].Add(Packet);
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
            //Reset the secondary queue, then enable the flag so new packets are added there until we finish transmitting whats in the main queue
            SecondaryPacketQueue.Clear();
            MainQueueInUse = true;

            //Reset the communication interval timer
            NextCommunication = CommunicationInterval;

            //Loop through every list stored in the main queue
            foreach (KeyValuePair<int, List<NetworkPacket>> OutgoingQueue in OutgoingPackets)
            {
                //Grab the packet list and target ClientID for each list in the dictionary, then pass each on to the transmission function to be sent to them
                List<NetworkPacket> CurrentList = OutgoingQueue.Value;
                int ClientID = OutgoingQueue.Key;

                //Pass these on to the transmission function so the data can be send to the target client
                SendPacketList(ClientID, CurrentList);
            }

            //All packets have been sent, we now want to rest the MainQueue, copy into it any contents in the SecondaryQueue
            //then reenable the MainQueue so it can be added to again
            OutgoingPackets.Clear();

            //Loop through everything stored in the secondary queue
            foreach(KeyValuePair<int, List<NetworkPacket>> OutgoingQueue in SecondaryPacketQueue)
            {
                //Grab the packet list for the current key as we iterate through the secondary queue
                List<NetworkPacket> SecondaryList = OutgoingQueue.Value;

                //Add each list of packets into the main packet queue
                AddToMainQueue(OutgoingQueue.Key, SecondaryList);
            }

            //Now finished everything, disable the flag so new packets are again added to the main queue
            MainQueueInUse = false;
        }

        //Takes a list of network packets, combines them all into a single packet and sends them to the target client
        private static void SendPacketList(int ClientID, List<NetworkPacket> PacketList)
        {
            //Fetch this clients ClientConnection and make sure their connection is still open
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);

            //Exit out of the function if the ClientConnection couldnt be found
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to send their packet queue to them.");
                return;
            }

            //Create a new string, and combine the data of every packet in the list into it
            string TotalData = "";
            foreach (NetworkPacket Packet in PacketList)
                TotalData += Packet.PacketData;

            //Now transmit the total data to the target client if it doesnt remain empty
            if(TotalData != "")
                Client.SendPacket(TotalData);
        }
    }
}