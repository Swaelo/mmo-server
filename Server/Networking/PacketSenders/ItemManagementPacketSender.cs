// ================================================================================================================================
// File:        ItemManagementPacketSender.cs
// Description: Formats and delivers network packets to game clients to keep them updated on the state of any item pickups in the game world
// ================================================================================================================================

using System.Collections.Generic;
using Server.Maths;
using Server.Interface;
using Server.GameItems;

namespace Server.Networking.PacketSenders
{
    public static class ItemManagementPacketSender
    {
        /// <summary>
        /// Instructs all active game clients to add a new item pickup to their game worlds
        /// </summary>
        /// <param name="ItemPickup">The new game item thats going to be spawned</param>
        public static void SendAllSpawnItemPickup(GameItem ItemPickup)
        {
            //Log a message to the network packets window
            Log.PrintOutgoingPacketMessage("ItemManagement.SendAllSpawnItemPickup");

            //Go through the list of all the active game clients
            List<ClientConnection> ActiveClients = ConnectionManager.GetActiveClients();
            foreach(ClientConnection ActiveClient in ActiveClients)
            {
                //Send each of them a new network packet that instructs them to spawn this item pickup into their gameworld
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(ActiveClient.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.SpawnItem);
                //The clients must be told the items number, network ID and the location where its going to be spawned
                QueueWriter.WriteInt(ItemPickup.ItemNumber);
                QueueWriter.WriteInt(ItemPickup.ItemID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(ItemPickup.ItemPosition));
            }
        }

        /// <summary>
        /// Instructs all active game clients to remove an item pickup from their game worlds
        /// </summary>
        /// <param name="ItemPickup"></param>
        public static void SendAllRemoveItemPickup(GameItem ItemPickup)
        {
            //Log a message to the network packets window
            Log.PrintOutgoingPacketMessage("ItemManagement.SendAllRemoveItemPickup");

            //Go through the list of all the active game clients
            List<ClientConnection> ActiveClients = ConnectionManager.GetActiveClients();
            foreach(ClientConnection ActiveClient in ActiveClients)
            {
                //Send each of them a new network packet that instructs them to remove this item pickup from their gameworld
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(ActiveClient.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.RemoveItem);
                //The only need to know the items network ID to be able to remove it
                QueueWriter.WriteInt(ItemPickup.ItemID);
            }
        }
    }
}
