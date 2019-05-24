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
        //Tells a list of clients to spawn a new item pickup into their game world
        public static void SendListSpawnItemPickup(List<ClientConnection> Clients, GameItem ItemPickup)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("ItemManagement.SendListSpawnItemPickup");

            //Loop through each client in the list who needs to have this information delivered to them
            foreach(ClientConnection Client in Clients)
            {
                //Fetch each clients PacketWriter and write in the packet type
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.SpawnItem);

                //Write the new item pickups information into the packet data
                QueueWriter.WriteInt(ItemPickup.ItemNumber);
                QueueWriter.WriteInt(ItemPickup.ItemID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(ItemPickup.ItemPosition));
            }
        }

        //Tells a list of clients to remove one of the active item pickups from their game worlds
        public static void SendListRemoveItemPickup(List<ClientConnection> Clients, int ItemID)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("ItemManagement.SendListRemoveItemPickup");

            //Loop through each client in the list
            foreach(ClientConnection Client in Clients)
            {
                //Fetch each clients PacketWriter and write in the packet type
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.RemoveItem);

                //Write the items info
                QueueWriter.WriteInt(ItemID);
            }
        }
    }
}
