// ================================================================================================================================
// File:        GameWorldStatePacketSender.cs
// Description: Formats and delivers network packets to game clients to keep them updated on the current state of the game world
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Database;
using Server.Entities;
using Server.GameItems;
using Server.Logging;
using Server.Misc;
using System.Collections.Generic;

namespace Server.Networking.PacketSenders
{
    public static class GameWorldStatePacketSender
    {
        /// <summary>
        /// //Tells a client where all the other players are in the world so they can be spawned in before they can enter the world
        /// </summary>
        /// <param name="ClientID">NetworkID for target client</param>
        public static void SendActivePlayerList(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " active player list");

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
                //Write each characters name, and current location and rotation values
                Packet.WriteString(OtherClient.Character.Name);
                Packet.WriteBool(OtherClient.Character.IsAlive);
                Packet.WriteVector3(OtherClient.Character.Position);
                Packet.WriteVector3(OtherClient.Character.Movement);
                Packet.WriteQuaternion(OtherClient.Character.Rotation);
                Packet.WriteInt(OtherClient.Character.CurrentHealth);
                Packet.WriteInt(OtherClient.Character.MaxHealth);
            }

            //Add this packet to the target clients outgoing packet queue
            PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client where all the active entities are in the world to have them spawned in before they can enter the game world
        /// </summary>
        /// <param name="ClientID">NetworkID for target client</param>
        public static void SendActiveEntityList(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " active entity list");

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

        /// <summary>
        /// //Tells a client where all the active items are in the world to have them spawned in before they can start playing
        /// </summary>
        /// <param name="ClientID">NetworkID for target client</param>
        public static void SendActiveItemList(int ClientID)
        {
            CommunicationLog.LogOut(ClientID + " active item list");

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

        /// <summary>
        /// //Tells a clients all the contents of their chosen characters inventory to be loaded in before they enter into the game world
        /// </summary>
        /// <param name="ClientID">NetworkID of target client</param>
        /// <param name="CharacterName">Name of character who's inventory contents are being sent</param>
        public static void SendInventoryContents(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " inventory contents");

            //Create a new NetworkPacket object to store the data for this inventory contents request
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently in the characters inventory
            List<ItemData> InventoryContents = InventoriesDatabase.GetAllInventorySlots(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.InventoryContents);
            
            Packet.WriteInt(0);
            PacketQueue.QueuePacket(ClientID, Packet);

            //Packet.WriteInt(InventoryContents.Count);

            ////Loop through the list of items in the players inventory and write all of their information into the packet data
            //foreach(ItemData Item in InventoryContents)
            //{
            //    Packet.WriteInt(Item.ItemNumber);
            //    Packet.WriteInt(Item.ItemID);
            //}

            ////Add this packet to the target clients outgoing packet queue
            //PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client all the items currently equipped on their chosen character to be loaded in before they enter into the game world
        /// </summary>
        /// <param name="ClientID">NetworkID of target client</param>
        /// <param name="CharacterName">Name of character who's equipped items are being sent</param>
        public static void SendEquippedItems(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " equipped items");

            //Create a new NetworkPacket object to store the data for this equipped items request
            NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently equipped on the character
            List<ItemData> EquippedItems = EquipmentsDatabase.GetAllEquipmentSlots(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.EquippedItems);

            Packet.WriteInt(0);
            PacketQueue.QueuePacket(ClientID, Packet);

            //Packet.WriteInt(EquippedItems.Count);

            ////Loop through the list and write in each items information into the packet data
            //foreach(ItemData Item in EquippedItems)
            //{
            //    Packet.WriteInt((int)Item.ItemEquipmentSlot);
            //    Packet.WriteInt(Item.ItemNumber);
            //    Packet.WriteInt(Item.ItemID);
            //}

            ////Add this packet to the target clients outgoing packet queue
            //PacketQueue.QueuePacket(ClientID, Packet);
        }

        /// <summary>
        /// //Tells a client all the items currently socketed into their ability bar to be loaded in before they can enter into the game world
        /// </summary>
        /// <param name="ClientID">NetworkID of target client</param>
        /// <param name="CharacterName">Name of character who's socketed abilities are being sent</param>
        public static void SendSocketedAbilities(int ClientID, string CharacterName)
        {
            CommunicationLog.LogOut(ClientID + " socketed abilities");
   
            //Create a new NetworkPacket object to store the data for this socketed abilities request
               NetworkPacket Packet = new NetworkPacket();

            //Grab the list of all the items currently socketed into the characters action bar
            List<ItemData> SocketedAbilities = ActionBarsDatabase.GetEveryActionBarItem(CharacterName);

            //Write the relevant data values into the packet data
            Packet.WriteType(ServerPacketType.SocketedAbilities);

            Packet.WriteInt(0);
            PacketQueue.QueuePacket(ClientID, Packet);

            //Packet.WriteInt(SocketedAbilities.Count);

            ////Loop through the list and write in each items information into the packet data
            //foreach(ItemData Ability in SocketedAbilities)
            //{
            //    Packet.WriteInt(Ability.ItemNumber);
            //    Packet.WriteInt(Ability.ItemID);
            //}

            ////Add this packet to the target clients outgoing packet queue
            //PacketQueue.QueuePacket(ClientID, Packet);
        }
    }
}