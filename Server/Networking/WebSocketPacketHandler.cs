// ================================================================================================================================
// File:        WebSocketPacketHandler.cs
// Description: Automatically handles any packets of data received from game clients and passes it on to its registered handler function
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using Server.Interface;
using Server.Networking.WebSocketPacketHandlers;

namespace Server.Networking
{
    public static class WebSocketPacketHandler
    {
        //Each handler function is mapped into the dictionary with their packet type identifier
        public delegate void WebSocketPacket(int index, string data);
        public static Dictionary<int, WebSocketPacket> PacketHandlers = new Dictionary<int, WebSocketPacket>();

        //Reads a packet of data sent from one of the clients and passes it onto its registered handler function
        public static void ReadClientPacket(int ClientID, string PacketMessage)
        {
            //Log the incoming network packet
            Log.PrintIncomingPacketMessage("Client: " + PacketMessage);

            //Read the packet type identifier placed before the message
            string PacketTypeSegment = PacketMessage.Substring(0, PacketMessage.IndexOf(' '));
            int PacketType = Int32.Parse(PacketTypeSegment);

            //Invoke the matching handler function for the given packet type
            if (PacketHandlers.TryGetValue(PacketType, out WebSocketPacket Packet))
                Packet.Invoke(ClientID, PacketMessage.Substring(PacketMessage.IndexOf(' ')+1));
        }

        //Map all of the handler functions to their packet type identifiers
        public static void RegisterPacketHandlers()
        {
            PacketHandlers.Add((int)ClientPacketType.PlayerChatMessage, NetworkingPacketHandler.HandleClientMessage);
            PacketHandlers.Add((int)ClientPacketType.AccountLoginRequest, UserAccountPacketHandler.HandleAccountLoginRequest);
            PacketHandlers.Add((int)ClientPacketType.AccountLogoutAlert, UserAccountPacketHandler.HandleAccountLogoutAlert);
            PacketHandlers.Add((int)ClientPacketType.AccountRegistrationRequest, UserAccountPacketHandler.HandleAccountRegisterRequest);
            PacketHandlers.Add((int)ClientPacketType.CharacterDataRequest, UserAccountPacketHandler.HandleCharacterDataRequest);
            PacketHandlers.Add((int)ClientPacketType.CharacterCreationRequest, UserAccountPacketHandler.HandleCreateCharacterRequest);
        }
    }
}