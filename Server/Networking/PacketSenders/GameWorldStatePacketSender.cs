// ================================================================================================================================
// File:        GameWorldStatePacketSender.cs
// Description: Formats and delivers network packets to game clients to keep them updated on the current state of the game world
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Entities;
using Server.Misc;
using Server.GameItems;
using Server.Database;

namespace Server.Networking.PacketSenders
{
    public static class GameWorldStatePacketSender
    {
        //Tells a client where all the other players are in the world so they can be spawned in before they can enter the world
        public static void SendActivePlayerList(int ClientID)
        {
            //Create a new NetworkPacket object to store the data for this active player list
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the other active game clients
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.ActivePlayerList);
            Packet.WriteInt(OtherClients.Count);

            //Loop through the list of other clients and write each of their information into the packet data
            foreach(ClientConnection OtherClient in OtherClients)
            {
                Packet.WriteString(OtherClient.CharacterName);
                Packet.WriteVector3(OtherClient.CharacterPosition);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client where all the active entities are in the world to have them spawned in before they can enter the game world
        public static void SendActiveEntityList(int ClientID)
        {
            //Create a new NetworkPacket object to store the data for this active entity list
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the entities currently active in the game world
            List<BaseEntity> ActiveEntities = EntityManager.ActiveEntities;

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.ActiveEntityList);
            Packet.WriteInt(ActiveEntities.Count);

            //Loop through the list of active entities and write each of their information into the packet data
            foreach(BaseEntity ActiveEntity in ActiveEntities)
            {
                Packet.WriteString(ActiveEntity.Type);
                Packet.WriteString(ActiveEntity.ID);
                Packet.WriteVector3(VectorTranslate.ConvertVector(ActiveEntity.Location));
                Packet.WriteInt(ActiveEntity.HealthPoints);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client where all the active items are in the world to have them spawned in before they can start playing
        public static void SendActiveItemList(int ClientID)
        {
            //Create a new NetworkPacket object to store the data for this active item list
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the active item pickups currently in the game world
            List<GameItem> ItemPickups = ItemManager.GetActiveItemList();

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.ActiveItemList);
            Packet.WriteInt(ItemPickups.Count);

            //Loop through the list of item pickups and write each of their information into the packet data
            foreach(GameItem ItemPickup in ItemPickups)
            {
                Packet.WriteInt(ItemPickup.ItemNumber);
                Packet.WriteInt(ItemPickup.ItemID);
                Packet.WriteVector3(VectorTranslate.ConvertVector(ItemPickup.ItemPosition));
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a clients all the contents of their chosen characters inventory to be loaded in before they enter into the game world
        public static void SendInventoryContents(int ClientID, string CharacterName)
        {
            //Create a new NetworkPacket object to store the data for this inventory contents request
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently in the characters inventory
            List<ItemData> InventoryContents = InventoriesDatabase.GetAllInventorySlots(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.PlayerInventoryItems);
            Packet.WriteInt(InventoryContents.Count);

            //Loop through the list of items in the players inventory and write all of their information into the packet data
            foreach(ItemData Item in InventoryContents)
            {
                Packet.WriteInt(Item.ItemNumber);
                Packet.WriteInt(Item.ItemID);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client all the items currently equipped on their chosen character to be loaded in before they enter into the game world
        public static void SendEquippedItems(int ClientID, string CharacterName)
        {
            //Create a new NetworkPacket object to store the data for this equipped items request
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently equipped on the character
            List<ItemData> EquippedItems = EquipmentsDatabase.GetAllEquipmentSlots(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.PlayerEquipmentItems);
            Packet.WriteInt(EquippedItems.Count);

            //Loop through the list and write in each items information into the packet data
            foreach(ItemData Item in EquippedItems)
            {
                Packet.WriteInt((int)Item.ItemEquipmentSlot);
                Packet.WriteInt(Item.ItemNumber);
                Packet.WriteInt(Item.ItemID);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        //Tells a client all the items currently socketed into their ability bar to be loaded in before they can enter into the game world
        public static void SendSocketedAbilities(int ClientID, string CharacterName)
        {
            //Create a new NetworkPacket object to store the data for this socketed abilities request
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently socketed into the characters action bar
            List<ItemData> SocketedAbilities = ActionBarsDatabase.GetEveryActionBarItem(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.PlayerActionBarAbilities);
            Packet.WriteInt(SocketedAbilities.Count);

            //Loop through the list and write in each items information into the packet data
            foreach(ItemData Ability in SocketedAbilities)
            {
                Packet.WriteInt(Ability.ItemNumber);
                Packet.WriteInt(Ability.ItemID);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}