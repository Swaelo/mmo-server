// ================================================================================================================================
// File:        PacketReceiver.cs
// Description: Defines all the functions used for recieving data through the network, and passes the data onto the correct
//              functions to be handled accordingly
// ================================================================================================================================

using System.Collections.Generic;
using Server.Networking.PacketHandlers;

namespace Server.Networking
{
    class PacketReceiver
    {
        //Delegate function set, used to invoke the required packet handler function for each different type of packet that can be sent through the network
        public delegate void Packet(int index, byte[] data);
        public static Dictionary<int, Packet> PacketHandlers = new Dictionary<int, Packet>();

        //Reads in a packet sent from a client and passes it onto the registered handler function for that specific packet type
        public static void ReadClientPacket(int ClientID, byte[] PacketBuffer)
        {
            //First open up the packet and find out what type it is
            PacketReader Reader = new PacketReader(PacketBuffer);
            int PacketType = Reader.ReadInt();

            //Invoke the handler function registered to this packet type
            if (PacketHandlers.TryGetValue(PacketType, out Packet Packet))
                Packet.Invoke(ClientID, PacketBuffer);
        }

        //Registered all the packet handler functions to each packet type
        public static void RegisterPacketHandlers()
        {
            PacketHandlers.Add((int)ClientPacketType.AccountRegistrationRequest, AccountManagementPacketHandler.HandleAccountRegistrationRequest);
            PacketHandlers.Add((int)ClientPacketType.AccountLoginRequest, AccountManagementPacketHandler.HandleAccountLoginRequest);
            PacketHandlers.Add((int)ClientPacketType.CharacterCreationRequest, AccountManagementPacketHandler.HandleCharacterCreationRequest);
            PacketHandlers.Add((int)ClientPacketType.CharacterDataRequest, AccountManagementPacketHandler.HandleCharacterDataRequest);

            PacketHandlers.Add((int)ClientPacketType.EnterWorldRequest, GameWorldStatePacketHandler.HandleEnterWorldRequest);
            PacketHandlers.Add((int)ClientPacketType.NewPlayerReady, GameWorldStatePacketHandler.HandleNewPlayerReady);

            PacketHandlers.Add((int)ClientPacketType.PlayerChatMessage, PlayerCommunicationPacketHandler.HandlePlayerChatMessage);

            PacketHandlers.Add((int)ClientPacketType.PlayerUpdate, PlayerManagementPacketHandler.HandlePlayerUpdate);
            PacketHandlers.Add((int)ClientPacketType.PlayerAttack, PlayerManagementPacketHandler.HandlePlayerAttack);

            PacketHandlers.Add((int)ClientPacketType.PlayerInventoryRequest, InventoryEquipmentManagementPacketHandler.HandlePlayerInventoryRequest);
            PacketHandlers.Add((int)ClientPacketType.PlayerEquipmentRequest, InventoryEquipmentManagementPacketHandler.HandlePlayerEquipmentRequest);
            PacketHandlers.Add((int)ClientPacketType.PlayerActionBarRequest, InventoryEquipmentManagementPacketHandler.HandlePlayerActionBarRequest);

            PacketHandlers.Add((int)ClientPacketType.PlayerTakeItemRequest, ItemManagementPacketHandler.HandlePlayerTakeItem);
            PacketHandlers.Add((int)ClientPacketType.RemoveInventoryItem, ItemManagementPacketHandler.HandlePlayerDropItem);
            PacketHandlers.Add((int)ClientPacketType.EquipInventoryItem, ItemManagementPacketHandler.HandleEquipInventoryItem);
            PacketHandlers.Add((int)ClientPacketType.UnequipItem, ItemManagementPacketHandler.HandleUnequipItem);
            PacketHandlers.Add((int)ClientPacketType.PlayerMoveInventoryItem, ItemManagementPacketHandler.HandleMoveInventoryItem);
            PacketHandlers.Add((int)ClientPacketType.PlayerSwapInventoryItems, ItemManagementPacketHandler.HandleSwapInventoryItems);
            PacketHandlers.Add((int)ClientPacketType.PlayerSwapEquipmentItem, ItemManagementPacketHandler.HandleSwapEquipmentItem);
            PacketHandlers.Add((int)ClientPacketType.PlayerDropItem, ItemManagementPacketHandler.HandlePlayerDropItem);
            PacketHandlers.Add((int)ClientPacketType.PlayerEquipAbility, ItemManagementPacketHandler.HandlePlayerEquipAbility);
            PacketHandlers.Add((int)ClientPacketType.PlayerSwapEquipAbility, ItemManagementPacketHandler.HandlePlayerSwapEquipAbility);
            PacketHandlers.Add((int)ClientPacketType.PlayerUnequipAbility, ItemManagementPacketHandler.HandlePlayerUnequipAbility);
            PacketHandlers.Add((int)ClientPacketType.PlayerSwapAbilities, ItemManagementPacketHandler.HandlePlayerSwapAbilities);
            PacketHandlers.Add((int)ClientPacketType.PlayerMoveAbility, ItemManagementPacketHandler.HandlePlayerMoveAbility);
        }
    }
}
