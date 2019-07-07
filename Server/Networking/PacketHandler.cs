// ================================================================================================================================
// File:        PacketHandler.cs
// Description: Automatically handles any packets of data received from game clients and passes it on to its registered handler function
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Networking.PacketHandlers;

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
            //Create a new NetworkPacket object and palce all of this packet data inside it
            NetworkPacket NewPacket = new NetworkPacket(PacketMessage);

            //Loop through all of the packet data, passing each section of instructions on to their registered packet handler function
            while(!NewPacket.FinishedReading())
            {
                //Read the next packet type value
                ClientPacketType PacketType = NewPacket.ReadType();

                //Invoke the packet handler function that is regsitered to this specific client packet type
                if (PacketHandlers.TryGetValue(PacketType, out Packet Packet))
                    Packet.Invoke(ClientID, ref NewPacket);
            }
        }

        //Map all the packet handler functions into the dictionary
        public static void RegisterPacketHandlers()
        {
            //Map all the account management packet handlers into the dictionary
            PacketHandlers.Add(ClientPacketType.AccountLoginRequest, AccountManagementPacketHandler.HandleAccountLoginRequest);
            PacketHandlers.Add(ClientPacketType.AccountLogoutAlert, AccountManagementPacketHandler.HandleAccountLogoutAlert);
            PacketHandlers.Add(ClientPacketType.AccountRegistrationRequest, AccountManagementPacketHandler.HandleAccountRegisterRequest);
            PacketHandlers.Add(ClientPacketType.CharacterDataRequest, AccountManagementPacketHandler.HandleCharacterDataRequest);
            PacketHandlers.Add(ClientPacketType.CharacterCreationRequest, AccountManagementPacketHandler.HandleCreateCharacterRequest);

            //Map all the game world state packet handlers into the dictionary
            PacketHandlers.Add(ClientPacketType.EnterWorldRequest, GameWorldStatePacketHandler.HandleEnterWorldRequest);
            PacketHandlers.Add(ClientPacketType.NewPlayerReady, GameWorldStatePacketHandler.HandleNewPlayerReady);

            //Register player management handlers into the dictionary
            PacketHandlers.Add(ClientPacketType.PlayerUpdate, PlayerManagementPacketHandler.HandlePlayerUpdate);

            //Register miscellaneous packet handers into the dictionary
            PacketHandlers.Add(ClientPacketType.StillAlive, MiscellaneousPacketHandler.HandleStillAliveAlert);

            //Map player communication handlers into the dictionary
            PacketHandlers.Add(ClientPacketType.PlayerChatMessage, PlayerCommunicationPacketHandler.HandleClientChatMessage);
        }
    }
}