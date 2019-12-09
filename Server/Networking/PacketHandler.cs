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
        public static void ReadClientPacket(int ClientID, string PacketData)
        {
            //Store the total set of packet data into a new PacketData object for easier reading
            NetworkPacket TotalPacket = new NetworkPacket(PacketData);

            //Fetch the client connection who sent this to us, making sure they're still active
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);
            if(Client == null)
            {
                MessageLog.Print("ERROR: Client #" + ClientID + " not found, unable to read network packet from them.");
                Client.ClientDead = true;
                return;
            }

            //Iterate over all the packet data until we finished reading and handling all of it
            while(!TotalPacket.FinishedReading())
            {
                //Read the packets order number and packet type enum values
                int OrderNumber = TotalPacket.ReadInt();
                ClientPacketType PacketType = TotalPacket.ReadType();


                //Get the rest of the values for this set based on the packet type, then put the orer number back in the front of it
                NetworkPacket SectionPacket = ReadPacketValues(PacketType, TotalPacket);

                //Compared this packets order number to see if its arrived in the order we were expecting
                int ExpectedOrderNumber = Client.LastPacketNumberRecieved + 1;
                bool InOrder = OrderNumber == ExpectedOrderNumber;

                //If the packet arrived in order then it gets processed normally
                if (InOrder)
                {
                    //Reset the packets data before we pass it to the handler
                    SectionPacket.ResetRemainingData();

                    //Read away the packet type value as its not needed when processing packets immediately
                    SectionPacket.ReadType();

                    //Pass the section packet onto its registered handler function
                    if (PacketHandlers.TryGetValue(PacketType, out Packet Packet))
                        Packet.Invoke(ClientID, ref SectionPacket);

                    //Store this as the last packet that we have processed for this client
                    Client.LastPacketNumberRecieved = OrderNumber;
                }
                //If packets arrive out of order we tell the client what number we were expecting to receive next so everything since then gets resent
                else
                    SystemPacketSender.SendMissingPacketsRequest(ClientID, ExpectedOrderNumber);
            }
        }

        //Map all the packet handler functions into the dictionary
        public static void RegisterPacketHandlers()
        {
            //Account Management Packet Handlers
            PacketHandlers.Add(ClientPacketType.AccountLoginRequest, AccountManagementPacketHandler.HandleAccountLoginRequest);
            PacketHandlers.Add(ClientPacketType.AccountLogoutAlert, AccountManagementPacketHandler.HandleAccountLogoutAlert);
            PacketHandlers.Add(ClientPacketType.AccountRegistrationRequest, AccountManagementPacketHandler.HandleAccountRegisterRequest);
            PacketHandlers.Add(ClientPacketType.CharacterDataRequest, AccountManagementPacketHandler.HandleCharacterDataRequest);
            PacketHandlers.Add(ClientPacketType.CharacterCreationRequest, AccountManagementPacketHandler.HandleCreateCharacterRequest);

            //Game World State Packet Handlers
            PacketHandlers.Add(ClientPacketType.EnterWorldRequest, GameWorldStatePacketHandler.HandleEnterWorldRequest);
            PacketHandlers.Add(ClientPacketType.PlayerReadyAlert, GameWorldStatePacketHandler.HandleNewPlayerReady);

            //Player Communication Packet Handlers
            PacketHandlers.Add(ClientPacketType.PlayerChatMessage, PlayerCommunicationPacketHandler.HandleClientChatMessage);

            //Player Management Packet Handlers
            PacketHandlers.Add(ClientPacketType.LocalPlayerCharacterUpdate, PlayerManagementPacketHandler.HandlePlayerCharacterUpdate);
            PacketHandlers.Add(ClientPacketType.LocalPlayerCameraUpdate, PlayerManagementPacketHandler.HandlePlayerCameraUpdate);
            PacketHandlers.Add(ClientPacketType.LocalPlayerPlayAnimationAlert, PlayerManagementPacketHandler.HandlePlayAnimationAlert);

            //System Packet Handlers
            PacketHandlers.Add(ClientPacketType.MissedPacketsRequest, SystemPacketHandler.HandleMissedPacketsRequest);
            PacketHandlers.Add(ClientPacketType.StillConnectedReply, SystemPacketHandler.HandleStillConnectedReply);

            //Combat Packet Handlers
            PacketHandlers.Add(ClientPacketType.PlayerAttackAlert, CombatPacketHandler.HandlePlayerAttackAlert);
            PacketHandlers.Add(ClientPacketType.PlayerRespawnRequest, CombatPacketHandler.HandlePlayerRespawnRequest);
        }

        private static NetworkPacket ReadPacketValues(ClientPacketType PacketType, NetworkPacket ReadFrom)
        {
            switch(PacketType)
            {
                //Account Management
                case (ClientPacketType.AccountLoginRequest):
                    return AccountManagementPacketHandler.GetValuesAccountLoginRequest(ReadFrom);
                case (ClientPacketType.AccountLogoutAlert):
                    return AccountManagementPacketHandler.GetValuesAccountLogoutAlert(ReadFrom);
                case (ClientPacketType.AccountRegistrationRequest):
                    return AccountManagementPacketHandler.GetValuesAccountRegisterRequest(ReadFrom);
                case (ClientPacketType.CharacterDataRequest):
                    return AccountManagementPacketHandler.GetValuesCharacterDataRequest(ReadFrom);
                case (ClientPacketType.CharacterCreationRequest):
                    return AccountManagementPacketHandler.GetValuesCreateCharacterRequest(ReadFrom);

                //Game World State
                case (ClientPacketType.EnterWorldRequest):
                    return GameWorldStatePacketHandler.GetValuesEnterWorldRequest(ReadFrom);
                case (ClientPacketType.PlayerReadyAlert):
                    return GameWorldStatePacketHandler.GetValuesNewPlayerReady(ReadFrom);

                //Player Communication
                case (ClientPacketType.PlayerChatMessage):
                    return PlayerCommunicationPacketHandler.GetValuesClientChatMessage(ReadFrom);

                //Player Management
                case (ClientPacketType.LocalPlayerCharacterUpdate):
                    return PlayerManagementPacketHandler.GetValuesPlayerCharacterUpdate(ReadFrom);
                case (ClientPacketType.LocalPlayerCameraUpdate):
                    return PlayerManagementPacketHandler.GetValuesPlayerCameraUpdate(ReadFrom);
                case (ClientPacketType.LocalPlayerPlayAnimationAlert):
                    return PlayerManagementPacketHandler.GetValuesPlayAnimationAlert(ReadFrom);

                //System
                case (ClientPacketType.MissedPacketsRequest):
                    return SystemPacketHandler.GetValuesMissedPacketsRequest(ReadFrom);
                case (ClientPacketType.StillConnectedReply):
                    return SystemPacketHandler.GetValuesStillConnectedReply(ReadFrom);

                //Combat
                case (ClientPacketType.PlayerAttackAlert):
                    return CombatPacketHandler.GetValuesPlayerAttackAlert(ReadFrom);
                case (ClientPacketType.PlayerRespawnRequest):
                    return CombatPacketHandler.GetValuesPlayerRespawnRequest(ReadFrom);
            }
            return new NetworkPacket();
        }
    }
}