// ================================================================================================================================
// File:        GameWorldStatePacketHandler.cs
// Description: Handles client packets regarding the current state of the game world
// ================================================================================================================================

using System.Numerics;
using Server.Interface;
using System.Collections.Generic;
using Server.Networking.PacketSenders;
using Server.Entities;
using Server.Maths;
using Server.GameItems;
using Server.Scenes;
using Server.Database;
using BepuPhysics;
using BepuPhysics.Collidables;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking.PacketHandlers
{
    public static class GameWorldStatePacketHandler
    {
        //Once a user wants to enter the game world, makes sure all the relevant information is sent to them before allowing them to
        public static void HandleEnterWorldRequest(int ClientID, byte[] PacketData)
        {
            //Log a message to the display window
            Log.PrintIncomingPacketMessage(ClientID + ": GameWorldState.EnterWorldRequest");

            //Open up the network packet and read the type in from it
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();

            //Get this clients ClientConnection object, and extract all the packet information into that object
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.AccountName = Reader.ReadString();
            Client.CharacterName = Reader.ReadString();
            Client.CharacterPosition = Maths.VectorTranslate.ConvertVector(Reader.ReadVector3());

            //Now we need to send a whole bunch of information to this client, grab their QueueWriter so we can start sending the data through
            PacketWriter QueueWriter = PacketSender.GetQueueWriter(ClientID);

            //First thing we need to send through is the list of other players who already active in the game world
            QueueWriter.WriteInt((int)ServerPacketType.ActivePlayerList);
            //Fetch the list of other players active in the game world, write into the packet how many there are
            List<ClientConnection> OtherClients = ConnectionManager.GetActiveClientsExceptFor(ClientID);
            QueueWriter.WriteInt(OtherClients.Count);
            //Loop through the list of other clients, writing each clients character information into the packet data
            foreach(ClientConnection OtherClient in OtherClients)
            {
                QueueWriter.WriteString(OtherClient.CharacterName);
                QueueWriter.WriteVector3(OtherClient.CharacterPosition);
            }

            //Second thing we need to send through is the list of all AI entities active in the game world
            QueueWriter.WriteInt((int)ServerPacketType.ActiveEntityList);
            //Fetch the list of all active ingame entities, write the total number into the packet data
            List<BaseEntity> ActiveEntities = EntityManager.ActiveEntities;
            QueueWriter.WriteInt(ActiveEntities.Count);
            //Loop through the list, writing each entities information into the packet data
            foreach(BaseEntity Entity in ActiveEntities)
            {
                QueueWriter.WriteString(Entity.Type);
                QueueWriter.WriteString(Entity.ID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(Entity.Location));
                QueueWriter.WriteInt(Entity.HealthPoints);
            }

            //Third thing we need to send through is the list of all active item pickups in the game world
            QueueWriter.WriteInt((int)ServerPacketType.ActiveItemList);
            //Fetch the list of active item pickups, write the total amount into the packet data
            List<GameItem> ItemPickups = ItemManager.GetActiveItemList();
            QueueWriter.WriteInt(ItemPickups.Count);
            //Loop through the list, writing each items information into the packet data
            foreach(GameItem Pickup in ItemPickups)
            {
                QueueWriter.WriteInt(Pickup.ItemNumber);
                QueueWriter.WriteInt(Pickup.ItemID);
                QueueWriter.WriteVector3(VectorTranslate.ConvertVector(Pickup.ItemPosition));
            }

            //Fourth thing we need to send through is the contents of the characters inventory
            QueueWriter.WriteInt((int)ServerPacketType.PlayerInventoryItems);
            //Fetch the list of items in the characters inventory
            List<ItemData> InventoryContents = InventoriesDatabase.GetAllInventorySlots(Client.CharacterName);
            //Loop through the list, writing each items details into the packet data
            foreach(ItemData Item in InventoryContents)
            {
                QueueWriter.WriteInt(Item.ItemNumber);
                QueueWriter.WriteInt(Item.ItemID);
            }

            //Fifth thing we need to send through is the items the character currently has equipped
            QueueWriter.WriteInt((int)ServerPacketType.PlayerEquipmentItems);
            //Fetch the list of items the character has equipped
            List<ItemData> EquippedItems = EquipmentsDatabase.GetAllEquipmentSlots(Client.CharacterName);
            //Loop through the list, writing each items details into the packet data
            foreach(ItemData Item in EquippedItems)
            {
                QueueWriter.WriteInt((int)Item.ItemEquipmentSlot);
                QueueWriter.WriteInt(Item.ItemNumber);
                QueueWriter.WriteInt(Item.ItemID);
            }

            //Final thing we need to send through is any ability gems the character has socketed on their action bar
            QueueWriter.WriteInt((int)ServerPacketType.PlayerActionBarAbilities);
            //Fetch the list of abilities socketed into the characters action bar
            List<ItemData> SocketedAbilities = ActionBarsDatabase.GetEveryActionBarItem(Client.CharacterName);
            //Loop through the list, writing each abilities information into the packet data
            foreach(ItemData Ability in SocketedAbilities)
            {
                QueueWriter.WriteInt(Ability.ItemNumber);
                QueueWriter.WriteInt(Ability.ItemID);
            }
        }

        //After recieving the active entity list, the new client will tell us they are ready to enter into the game world
        public static void HandleNewPlayerReady(int ClientID, byte[] PacketData)
        {
            Log.PrintIncomingPacketMessage(ClientID + ": GameWorldState.NewPlayerReady");

            //Store the new clients information
            ClientConnection NewClient = ConnectionManager.ActiveConnections[ClientID];
            NewClient.InGame = true;

            //Add a new collider into the physics scene to represent where this client is located
            Simulation World = Program.World.WorldSimulation;
            Capsule ClientShape = new Capsule(0.5f, 1);
            CollidableDescription ClientDescription = new CollidableDescription(World.Shapes.Add(ClientShape), 0.1f);
            ClientShape.ComputeInertia(1, out var Inertia);
            Vector3 SpawnLocation = new Vector3(NewClient.CharacterPosition.X, NewClient.CharacterPosition.Y + 2, NewClient.CharacterPosition.Z);
            RigidPose ClientPose = new RigidPose(SpawnLocation, Quaternion.Identity);
            NewClient.PhysicsBody = BodyDescription.CreateDynamic(ClientPose, Inertia, ClientDescription, new BodyActivityDescription(0.01f));
            NewClient.BodyHandle = World.Bodies.Add(NewClient.PhysicsBody);

            //Tell all the other already active players this new client has entered the game
            List<ClientConnection> OtherClients = ConnectionManager.GetActiveClientsExceptFor(NewClient.NetworkID);
            PlayerManagementPacketSender.SendListSpawnOtherCharacter(OtherClients, NewClient.CharacterName, NewClient.CharacterPosition);

            Log.PrintDebugMessage("Networking.PacketHandlers.GameWorldStatePacketHandler " + NewClient.CharacterName + " has entered the game at " + NewClient.CharacterPosition);
        }
    }
}
