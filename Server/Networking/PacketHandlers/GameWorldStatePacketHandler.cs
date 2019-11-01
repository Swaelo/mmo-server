// ================================================================================================================================
// File:        GameWorldStatePacketHandler.cs
// Description: Handles client packets regarding the current state of the game world
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using Server.Logging;
using Server.Database;
using Server.Data;
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
            //Read the characters name the player is going to use, use it to fetch the rest of the characters data from the database
            string CharacterName = Packet.ReadString();
            CharacterData CharacterData = CharactersDatabase.GetCharacterData(CharacterName);

            //Fetch this clients ClientConnection object and update it with the info from CharacterData that we retrieved
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.CharacterName = CharacterName;
            Client.CharacterPosition = CharacterData.Position;

            //Send the clients lists of other players, AI entities, item pickups, inventory contents, equipped items and socketed actionbar abilities
            GameWorldStatePacketSender.SendActivePlayerList(ClientID);
            GameWorldStatePacketSender.SendActiveEntityList(ClientID);
            GameWorldStatePacketSender.SendActiveItemList(ClientID);
            GameWorldStatePacketSender.SendInventoryContents(ClientID, CharacterName);
            GameWorldStatePacketSender.SendEquippedItems(ClientID, CharacterName);
            GameWorldStatePacketSender.SendSocketedAbilities(ClientID, CharacterName);
        }

        //When a client has finished receiving all the setup information they will let us know when they are entering into the game world finally
        public static void HandleNewPlayerReady(int ClientID, ref NetworkPacket Packet)
        {
            //Get this clients information and flag them as being ingame
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Client.InGame = true;

            //Add a new collider into the physics scene to represent where this client is located in the game world
            Simulation World = Program.World.WorldSimulation;
            Client.PhysicsShape = new Capsule(0.5f, 1);
            Client.ShapeIndex = World.Shapes.Add(Client.PhysicsShape);
            Client.PhysicsDescription = new CollidableDescription(Client.ShapeIndex, 0.1f);
            Client.PhysicsShape.ComputeInertia(1, out var Inertia);
            Vector3 SpawnLocation = new Vector3(Client.CharacterPosition.X, Client.CharacterPosition.Y + 2, Client.CharacterPosition.Z);
            Client.ShapePose = new RigidPose(SpawnLocation, Quaternion.Identity);
            Client.ActivityDescription = new BodyActivityDescription(0.01f);
            Client.PhysicsBody = BodyDescription.CreateDynamic(Client.ShapePose, Inertia, Client.PhysicsDescription, Client.ActivityDescription);
            Client.BodyHandle = World.Bodies.Add(Client.PhysicsBody);

            //Tell all other ingame clients they need to spawn this new player into their game worlds
            foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(ClientID))
                PlayerManagementPacketSender.SendAddOtherPlayer(OtherClient.NetworkID, Client.CharacterName, Client.CharacterPosition);

            //Display a message showing that the users character has been spawned into the game world
            MessageLog.Print(Client.CharacterName + " has entered into the game world at location " + Client.CharacterPosition.ToString());
        }
    }
}