// ================================================================================================================================
// File:        PacketHandler.cs
// Description: Automatically handles any packets of data received from game clients and passes it on to its registered handler function
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Networking.PacketSenders;
using Server.Networking.PacketHandlers;
using Server.Logging;

namespace Server.Networking
{
    public static class PacketHandler
    {
        //Each handler function is mapped into the dictionary with their packet type identifier
        public delegate void Packet(int ClientID, ref NetworkPacket Packet);
        public static Dictionary<ClientPacketType, Packet> PacketHandlers = new Dictionary<ClientPacketType, Packet>();

        //Reads a packet of data sent from one of the clients and passes it onto its registered handler function
        public static void ReadClientPacket(int ClientID, string PacketMessage)
        {
            //Create a new NetworkPacket object to store the string value that was received from the game client
            NetworkPacket NewPacket = new NetworkPacket(PacketMessage);

            //Fetch the ClientConnection who sent this packet to us
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if (Client == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found.");
                return;
            }

            //Before reading the packets data, check the order number to make sure we didnt miss any packets in between
            int NewOrderNumber = NewPacket.ReadInt();
            int ExpectedOrderNumber = Client.LastPacketReceived + 1;

            //If we ever recieve a packet with an order number of -1, then that packet needs to be processed immediately
            if(NewOrderNumber == -1)
            {
                //Handle this packets data now
                HandlePacket(ClientID, NewPacket);
            }
            //If packets were missed then we need to let the client know, and then just wait until all the missing packets have been received
            else if (NewOrderNumber != ExpectedOrderNumber)
            {
                //Add this packet that we have just not received into the WaitingToProcess dictionary and mark it as being the NewestPacketWaitingToProcess while we wait for the rest
                Client.WaitingToProcess.Add(NewOrderNumber, NewPacket);
                Client.NewestPacketWaitingToProcess = NewOrderNumber;
                Client.WaitingForMissingPackets = true;

                //Send an alert to the client, letting them know which packets need to be resent to us so we can catch up to them
                SystemPacketSender.SendMissingPacketsRequest(ClientID, ExpectedOrderNumber);

                //Keep track of the initial order number that we are waiting to receive back from the client
                Client.FirstMissingPacketNumber = ExpectedOrderNumber;

                //Nothing else needs to be done right now, as we need to wait for the missing packets to be sent back from the game client
                return;
            }
            //If we missed nothing, then we process the packet data as normal
            else
            {
                //If we are waiting for missing packets to be resent, check if we have everything needed now to catch back up
                if(Client.WaitingForMissingPackets)
                {
                    //Make sure we arent trying to add packets into the WaitingToProcess dictionary which are already there
                    if(!Client.WaitingToProcess.ContainsKey(NewOrderNumber))
                    {
                        //Add this packet into the WaitingToProcess dictionary
                        Client.WaitingToProcess.Add(NewOrderNumber, NewPacket);

                        //Set this as the NewestPacketWaitingToProcess if it has a higher order number than the previous
                        if (NewOrderNumber > Client.NewestPacketWaitingToProcess)
                            Client.NewestPacketWaitingToProcess = NewOrderNumber;

                        //Check if we now have all the missing packets that we were waiting for
                        bool HaveMissingPackets = true;
                        for (int i = Client.FirstMissingPacketNumber; i < Client.NewestPacketWaitingToProcess; i++)
                        {
                            if (!Client.WaitingToProcess.ContainsKey(i))
                            {
                                HaveMissingPackets = false;
                                break;
                            }
                        }

                        //If we now have all the missing packets that we were waiting for, now they can all be processed catching up back up to this clients state
                        if (HaveMissingPackets)
                        {
                            //Go through all the packets waiting for be processed
                            for (int i = Client.FirstMissingPacketNumber; i < Client.NewestPacketWaitingToProcess; i++)
                            {
                                //Grab each packet from the dictionary
                                NetworkPacket PacketToProcess = Client.WaitingToProcess[i];

                                //Handle the packets data
                                HandlePacket(ClientID, PacketToProcess);
                            }

                            //All the missing packets have now been processed, reset the dictionary, disable the flag and set the new value for the next expected packet number
                            Client.WaitingToProcess = new Dictionary<int, NetworkPacket>();
                            Client.WaitingForMissingPackets = false;
                            Client.LastPacketReceived = Client.NewestPacketWaitingToProcess;
                        }
                    }
                }
                //If we arent waiting for any missing packets, we just process this data as normal
                else
                {
                    //Set this number as the last that was received from this client
                    Client.LastPacketReceived = NewOrderNumber;

                    //Handle the packets data
                    HandlePacket(ClientID, NewPacket);
                }
            }
        }

        //Read all the information from the given packet, passing each section on to its registered handler function
        private static void HandlePacket(int ClientID, NetworkPacket NewPacket)
        {
            while(!NewPacket.FinishedReading())
            {
                ClientPacketType PacketType = NewPacket.ReadType();
                if (PacketHandlers.TryGetValue(PacketType, out Packet Packet))
                    Packet.Invoke(ClientID, ref NewPacket);
            }
        }

        //Map all the packet handler functions into the dictionary
        public static void RegisterPacketHandlers()
        {
            //Account Management Packet Handlers
            PacketHandlers.Add(ClientPacketType.AccountLoginRequest, AccountManagementPacketHandler.HandleAccountLoginRequest);
            PacketHandlers.Add(ClientPacketType.AccountRegistrationRequest, AccountManagementPacketHandler.HandleAccountRegisterRequest);
            PacketHandlers.Add(ClientPacketType.CharacterDataRequest, AccountManagementPacketHandler.HandleCharacterDataRequest);
            PacketHandlers.Add(ClientPacketType.CharacterCreationRequest, AccountManagementPacketHandler.HandleCreateCharacterRequest);

            //Game World State Packet Handlers
            PacketHandlers.Add(ClientPacketType.EnterWorldRequest, GameWorldStatePacketHandler.HandleEnterWorldRequest);
            PacketHandlers.Add(ClientPacketType.PlayerReadyAlert, GameWorldStatePacketHandler.HandleNewPlayerReady);


            //Player Communication Packet Handlers
            PacketHandlers.Add(ClientPacketType.PlayerChatMessage, PlayerCommunicationPacketHandler.HandleClientChatMessage);

            //Player Management Packet Handlers
            PacketHandlers.Add(ClientPacketType.CharacterPositionUpdate, PlayerManagementPacketHandler.HandlePositionUpdate);
            PacketHandlers.Add(ClientPacketType.CharacterRotationUpdate, PlayerManagementPacketHandler.HandleRotationUpdate);
            PacketHandlers.Add(ClientPacketType.CharacterMovementUpdate, PlayerManagementPacketHandler.HandleMovementUpdate);
            PacketHandlers.Add(ClientPacketType.CharacterCameraUpdate, PlayerManagementPacketHandler.HandlePlayerCameraUpdate);

            //System Packet Handlers
            PacketHandlers.Add(ClientPacketType.MissedPacketsRequest, SystemPacketHandler.HandleMissedPacketsRequest);
            PacketHandlers.Add(ClientPacketType.StillConnectedReply, SystemPacketHandler.HandleStillConnectedReply);
            PacketHandlers.Add(ClientPacketType.OutOfSyncAlert, SystemPacketHandler.HandleOutOfSyncAlert);
        }
    }
}