// ================================================================================================================================
// File:        GameWorldStatePacketSender.cs
// Description: Formats and delivers network packets to game clients to keep them updated on the current state of the game world
// ================================================================================================================================

using System.Collections.Generic;
using Server.Interface;
using Server.Entities;
using Server.Maths;
using Server.GameItems;

namespace Server.Networking.PacketSenders
{
    public static class GameWorldStatePacketSender
    {
        //Tells a client where all the other players are in the world to have them spawned in before they can enter into the world
        public static void SendActivePlayerList(int ClientID)
        {
            //Fetch the packet writer, write the packet type and log a message to the display window
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.ActivePlayerList);
            Log.PrintOutgoingPacketMessage(ClientID + ": GameWorldState.ActivePlayerList");

            //Fetch the list of all other active game clients and write into the packet how many there are
            List<ClientConnection> OtherClients = ConnectionManager.GetActiveClientsExceptFor(ClientID);
            QueueWriter.WriteInt(OtherClients.Count);

            //Loop through all the other clients and write each ones info into the packet
            foreach(ClientConnection OtherClient in OtherClients)
            {
                QueueWriter.WriteString(OtherClient.CharacterName);
                QueueWriter.WriteVector3(OtherClient.CharacterPosition);
            }
        }

        //Tells a client where all the active entities are in the world to have them spawned in before they are allowed to enter into the game world
        public static void SendActiveEntityList(int ClientID)
        {
            //Fetch the packet writer, write the packet type and log a message to the display window
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.ActiveEntityList);
            Log.PrintOutgoingPacketMessage(ClientID + ": GameWorldState.ActiveEntityList");

            //Fetch the list of all the active ingame entities, write the total number of entities into the packet
            List<BaseEntity> ActiveEntities = EntityManager.ActiveEntities;
            QueueWriter.WriteInt(ActiveEntities.Count);

            //Loop through the list, writing into the packet each entities information
            foreach(BaseEntity ActiveEntity in ActiveEntities)
            {
                QueueWriter.WriteString(ActiveEntity.Type);
                QueueWriter.WriteString(ActiveEntity.ID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(ActiveEntity.Location));
                QueueWriter.WriteInt(ActiveEntity.HealthPoints);
            }
        }

        //Tells a client where all the active items are in the world to have them spawned in before they can start playing
        public static void SendActiveItemList(int ClientID)
        {
            //Fetch the packet writer, write the packet type and log a message to the display window
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);
            QueueWriter.WriteInt((int)ServerPacketType.ActiveItemList);
            Log.PrintOutgoingPacketMessage(ClientID + ": GameWorldState.ActiveItemList");

            //Fetch the list of active game item picks, write the list length into the packet
            List<GameItem> ItemPickups = ItemManager.GetActiveItemList();
            QueueWriter.WriteInt(ItemPickups.Count);

            //Loop through the entire list, writing each items information into the packet data
            foreach(GameItem ItemPickup in ItemPickups)
            {
                QueueWriter.WriteInt(ItemPickup.ItemNumber);
                QueueWriter.WriteInt(ItemPickup.ItemID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(ItemPickup.ItemPosition));
            }
        }
    }
}
