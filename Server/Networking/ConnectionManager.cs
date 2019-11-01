// ================================================================================================================================
// File:        ConnectionManager.cs
// Description: Implementation of the ConnectionManager class using WebSockets, allowing WebGL clients to connect
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Server.Networking.PacketSenders;
using Server.Interface;
using BepuPhysics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking
{
    public static class ConnectionManager
    {
        public static TcpListener NewClientListener;    //Receives new incoming client connections
        public static Dictionary<int, ClientConnection> ActiveConnections = new Dictionary<int, ClientConnection>();    //Each active client connection mapped to their network ID

        public static float ConnectionCheckInterval = 5.0f; //How often to check for dead client connections
        public static float NextConnectionCheck = 5.0f; //How long until we need to check for dead client connections again
        private static int ClientConnectionTimeout = 15;    //How many seconds must pass without hearing from a client before we shut down their connection

        //Sets up the connection manager and starts listening for new incoming client connections
        public static void InitializeManager(string ServerIP)
        {
            PacketHandler.RegisterPacketHandlers();

            NewClientListener = new TcpListener(IPAddress.Parse(ServerIP), 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);
        }

        //Checks how much time has passed since we last heard from each of the active client connections, cleaning up any connections which have been inactive for too long
        public static void CheckConnections(float DeltaTime)
        {
            //Count down the timer until we need to perform a new connection check on all the clients
            NextConnectionCheck -= DeltaTime;

            //Check the status of all client connections and reset the timer whenever it reaches zero
            if(NextConnectionCheck <= 0.0f)
            {
                Log.Chat("Checking for inactive clients...");
                
                //Reset the timer for checking client connections again
                NextConnectionCheck = ConnectionCheckInterval;

                //Get all the current client connections in a List format
                List<ClientConnection> ClientConnections = GetAllClients();

                //Loop through all of the client connections and check how long since we have last heard from each of them
                int ClientsToRemove = 0;
                foreach(ClientConnection ClientConnection in ClientConnections)
                {
                    //Check how many seconds have passed since we last heard from this client
                    int SecondsPassed = ClientConnection.LastCommunication.AgeInSeconds();

                    //If enough time has passed since we last heard from this client then we need to clean up their connection, log out their account etc.
                    if (SecondsPassed >= ClientConnectionTimeout)
                    {
                        //Flag this client as being dead so the GameWorld simulation cleans it up when its ready to do so
                        ClientsToRemove++;
                        ClientConnection.ClientDead = true;
                    }
                }

                if (ClientsToRemove == 0)
                    Log.Chat("No inactive clients found.");
                else
                    Log.Chat(ClientsToRemove.ToString() + " inactive clients need to be cleaned up.");
            }
        }

        //Cleans up any dead client connections, removing their character from the physics scene, telling other clients they are now gone etc.
        public static void CleanDeadClients(Simulation WorldSimulation)
        {
            //Get a list of all the current client connections which have been flagged as being dead
            List<ClientConnection> DeadClients = GetDeadClients();

            //Loop through and clean all of them up
            foreach(ClientConnection DeadClient in DeadClients)
            {
                //If they were inside the game world their character needs to be removed from the world
                if(DeadClient.InGame)
                {
                    //Remove their character from the physics world simulation
                    WorldSimulation.Bodies.Remove(DeadClient.BodyHandle);
                    WorldSimulation.Shapes.Remove(DeadClient.ShapeIndex);

                    //Get the current list of other game clients who are currently ingame
                    List<ClientConnection> OtherClients = GetInGameClientsExceptFor(DeadClient.NetworkID);
                    //Tell all these other clients to remove this dead clients remote player from their game worlds
                    foreach (ClientConnection OtherClient in OtherClients)
                        PlayerManagementPacketSender.SendRemoveOtherPlayer(DeadClient.NetworkID, DeadClient.CharacterName);
                }

                //Remove this client from this list of active connections now that they have been cleaned up
                ActiveConnections.Remove(DeadClient.NetworkID);
            }
        }

        //Updates the physics body position of any clients who have sent us a new position update since the last update
        public static void UpdateClientPositions(Simulation World)
        {
            //Get the list of clients with updated positions
            List<ClientConnection> UpdatedClients = GetUpdatedClients();

            //Loop through them all so we can apply their new position values to their physics objects in the world simulation
            foreach(ClientConnection UpdatedClient in UpdatedClients)
            {
                //Store the clients new position value in their ClientConnection object
                UpdatedClient.CharacterPosition = UpdatedClient.NewPosition;
                //Use the NewPosition value to reassign a new ShapePose for the clients physics body
                UpdatedClient.ShapePose = new RigidPose(UpdatedClient.CharacterPosition, Quaternion.Identity);
                //Calculate a new Inertia value for the clients physics body
                UpdatedClient.PhysicsShape.ComputeInertia(1, out var Inertia);
                //Use the new ShapePose and Inertia values to assign a new BodyDescription to the client
                UpdatedClient.PhysicsBody = BodyDescription.CreateDynamic(UpdatedClient.ShapePose, Inertia, UpdatedClient.PhysicsDescription, UpdatedClient.ActivityDescription);
                //Apply this new body description to the clients physics body inside the gameworld physics simulation
                World.Bodies.ApplyDescription(UpdatedClient.BodyHandle, ref UpdatedClient.PhysicsBody);

                //Reset the clients updated position flag now that their physics body has been moved to the new position
                UpdatedClient.NewPositionReceived = false;
            }
        }

        //Returns a list of all client connections which have been flagged as having a new position value that needs to be applied
        private static List<ClientConnection> GetUpdatedClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> UpdatedClients = new List<ClientConnection>();

            //Loop through the entire dictionary of client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ActiveConnections)
            {
                //Add them to the list if they have been flagged as having a new position value
                if (Connection.Value.NewPositionReceived)
                    UpdatedClients.Add(Connection.Value);
            }

            //Return the final list of all the dead clients
            return UpdatedClients;
        }

        //Returns a list of all client connections which have been flagged as being dead
        private static List<ClientConnection> GetDeadClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> DeadClients = new List<ClientConnection>();

            //Loop through the entire dictionary of client connections
            foreach(KeyValuePair<int, ClientConnection> Connection in ActiveConnections)
            {
                //Add them to the list if they have been flagged as dead
                if (Connection.Value.ClientDead)
                    DeadClients.Add(Connection.Value);
            }

            //Return the final list of all the dead clients
            return DeadClients;
        }

        //Returns all of the active client connections in a List format
        public static List<ClientConnection> GetAllClients()
        {
            //Create a new list to place all the clients into after taking them from the dictionary
            List<ClientConnection> Clients = new List<ClientConnection>();

            //Loop through the entire dictionary, placing each client object into the new list
            foreach (KeyValuePair<int, ClientConnection> Connection in ActiveConnections)
                Clients.Add(Connection.Value);

            //Return the final list of clients
            return Clients;
        }

        //Returns all of the active client connections except for 1 with the matching ClientID that is provided
        public static List<ClientConnection> GetAllOtherClients(int ClientID)
        {
            //Create a new list to place all the clients into after taking them from the dictionary
            List<ClientConnection> OtherClients = new List<ClientConnection>();

            //Loop through the entire dictionary, placing each client that doesnt have the matching ID into the List
            foreach (KeyValuePair<int, ClientConnection> Connection in ActiveConnections)
                if (Connection.Key != ClientID)
                    OtherClients.Add(Connection.Value);

            //Return the final list of clients
            return OtherClients;
        }

        //Returns a list of all the clients currently in the game world playing with one of their characters
        public static List<ClientConnection> GetInGameClients()
        {
            //Create a new list to palce all the ingame clients into
            List<ClientConnection> InGameClients = new List<ClientConnection>();

            //Loop through the entire dictionary of client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ActiveConnections)
            {
                //Add any clients who are ingame to the list
                if (Connection.Value.InGame)
                    InGameClients.Add(Connection.Value);
            }

            //Return the final list of InGame Clients
            return InGameClients;
        }

        //Returns a list of all the other clients who are currently in the game world and playing with one of their characters
        public static List<ClientConnection> GetInGameClientsExceptFor(int ClientID)
        {
            //Start by getting the complete list of ingame clients
            List<ClientConnection> InGameClients = GetInGameClients();

            //Get the ClientConnection that needs to be removed from the list before we return it
            ClientConnection ExceptFor = ActiveConnections[ClientID];

            //Remove the excepted client from the list if its in there
            if (InGameClients.Contains(ExceptFor))
                InGameClients.Remove(ExceptFor);

            //Return the final list of all the other ingame clients
            return InGameClients;
        }

        //ASync event triggered when a new client has connected to the server, sets them up and stored them in the connections dictionary
        private static void NewClientConnected(IAsyncResult Result)
        {
            //Grab the new connection into a new TcpClient object, then re-register this function again to immediatly listen for new connections again
            TcpClient NewConnection = NewClientListener.EndAcceptTcpClient(Result);
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);

            //Place the new client connection into its own client object and store that with the rest
            ClientConnection NewClient = new ClientConnection(NewConnection);
            ActiveConnections.Add(NewClient.NetworkID, NewClient);
        }

        //Shuts down the connection with a specific client and removes them from the active connections
        public static void CloseConnection(ClientConnection Connection)
        {
            //Flag this client as dead so it gets cleaned up by the GameWorld in its next pass
            Connection.ClientDead = true;
        }

        //Checks everyone who is logged on to know if an account is already in use or not
        public static bool IsAccountLoggedIn(string AccountName)
        {
            foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
            {
                if (Client.Value.AccountName == AccountName)
                    return true;
            }
            return false;
        }
    }
}