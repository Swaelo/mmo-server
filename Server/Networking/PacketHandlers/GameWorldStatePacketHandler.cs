// ================================================================================================================================
// File:        GameWorldStatePacketHandler.cs
// Description: Handles client packets regarding the current state of the game world
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using System.Collections.Generic;
using Server.Networking.PacketSenders;
using BepuPhysics;
using BepuPhysics.Collidables;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking.PacketHandlers
{
    public static class GameWorldStatePacketHandler
    {
        //When a client wants to enter the game world, we need to send them a bunch of information to set up their game world before they can enter
        public static void HandleEnterWorldRequest(int ClientID, ref NetworkPacket Packet)
        {
            //Read the name of the character this user wants to enter into the world with
            string CharacterName = Packet.ReadString();

            //Store the name of the character the client is playing with in their ClientConnection object
            ConnectionManager.ActiveConnections[ClientID].CharacterName = CharacterName;

            //First need to send this client a list of all other players already in the game
            //a list of all entities active in the game, and a list of all active game items
            GameWorldStatePacketSender.SendActivePlayerList(ClientID);
            GameWorldStatePacketSender.SendActiveEntityList(ClientID);
            GameWorldStatePacketSender.SendActiveItemList(ClientID);

            //Next we need to send through the contents of their characters inventory, whatever
            //items they have currently equipped, and what abilities are on their action bar
            GameWorldStatePacketSender.SendInventoryContents(ClientID, CharacterName);
            GameWorldStatePacketSender.SendEquippedItems(ClientID, CharacterName);
            GameWorldStatePacketSender.SendSocketedAbilities(ClientID, CharacterName);
        }

        //When a client has finished receiving all the setup information they will let us know when they are entering into the game world finally
        public static void HandleNewPlayerReady(int ClientID, ref NetworkPacket Packet)
        {
            //Get this clients information and update them as being ingame
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.InGame = true;

            //Add a new collider into the physics scene to represent where this client is located
            Simulation World = Program.World.WorldSimulation;
            Client.PhysicsShape = new Capsule(0.5f, 1);
            Client.ShapeIndex = World.Shapes.Add(Client.PhysicsShape);
            Client.PhysicsDescription = new CollidableDescription(Client.ShapeIndex, 0.1f);
            Client.PhysicsShape.ComputeInertia(1, out var Inertia);
            Vector3 SpawnLocation = new Vector3(Client.CharacterPosition.X, Client.CharacterPosition.Y + 2, Client.CharacterPosition.Z);
            Client.ShapePose =  new RigidPose(SpawnLocation, Quaternion.Identity);
            Client.ActivityDescription = new BodyActivityDescription(0.01f);
            Client.PhysicsBody = BodyDescription.CreateDynamic(Client.ShapePose, Inertia, Client.PhysicsDescription, Client.ActivityDescription);
            Client.BodyHandle = World.Bodies.Add(Client.PhysicsBody);

            //Get the current list of all the other game clients who are already ingame
            List<ClientConnection> OtherClients = ConnectionManager.GetInGameClientsExceptFor(ClientID);
            //Tell all of these other clients to spawn this new character into their game worlds
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendAddOtherPlayer(OtherClient.NetworkID, Client.CharacterName, Client.CharacterPosition);
        }
    }
}