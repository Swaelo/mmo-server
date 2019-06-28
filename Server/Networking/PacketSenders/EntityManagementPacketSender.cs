// ================================================================================================================================
// File:        EntityManagementPacketSender.cs
// Description: Formats and delivers network packets to game clients to keep them updated on the current state of entities in the game world
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Entities;
using Server.Interface;
using Server.Maths;

namespace Server.Networking.PacketSenders
{
    public static class EntityManagementPacketSender
    {
        //Sends instructions to a list of game clients, updated on all the active entities
        public static void SendListEntityUpdates(List<ClientConnection> Clients, List<BaseEntity> Entities)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("GameWorldState.SendListEntityUpdates");

            //Loop through each client in the list
            foreach (ClientConnection Client in Clients)
            {
                //Fetch each clients PacketWriter and write the packet type and number of entity updates into the data
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.EntityUpdates);
                QueueWriter.WriteInt(Entities.Count);

                //Loop through the list of entities and each ones information into the packet data
                foreach (BaseEntity Entity in Entities)
                {
                    //Write in each entities NetworkID, Location, Orientation and current health point value
                    QueueWriter.WriteString(Entity.ID);
                    QueueWriter.WriteVector3(VectorTranslate.ConvertVector(Entity.Location));
                    QueueWriter.WriteQuaternion(Entity.Rotation);
                    QueueWriter.WriteInt(Entity.HealthPoints);
                }
            }
        }

        //Sends instructions to a list a game clients to have a list of active entities removed from their game worlds
        public static void SendListRemoveEntities(List<ClientConnection> Clients, List<BaseEntity> Entities)
        {
            //Log a message to the display window
            Log.PrintOutgoingPacketMessage("GameWorldState.SendListRemoveEntities");

            //Loop through each client in the clist
            foreach (ClientConnection Client in Clients)
            {
                //Fetch each clients PacketWriter and write the packet type and number of entities to be removed into the packet data
                PacketWriter QueueWriter = PacketSender.GetQueueWriter(Client.NetworkID);
                QueueWriter.WriteInt((int)ServerPacketType.RemoveEntities);
                QueueWriter.WriteInt(Entities.Count);

                //Loop through the list of entities and write each ones information into the packet data
                foreach (BaseEntity Entity in Entities)
                    QueueWriter.WriteString(Entity.ID);
            }
        }
    }
}
