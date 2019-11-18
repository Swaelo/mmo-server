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
using Server.Logging;
using Server.Database;
using BepuPhysics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking
{
    public static class ConnectionManager
    {
        public static TcpListener NewClientListener;    //Receives new incoming client connections
        private static Dictionary<int, ClientConnection> ActiveConnections = new Dictionary<int, ClientConnection>();    //Each active client connection mapped to their network ID

        public static float ConnectionCheckInterval = 2.5f; //How often to check for dead client connections
        public static float NextConnectionCheck = 2.5f; //How long until we need to check for dead client connections again
        private static int ClientConnectionTimeout = 5;    //How many seconds must pass without hearing from a client before we shut down their connection

        //Sets up the connection manager and starts listening for new incoming client connections
        public static void InitializeManager(string ServerIP)
        {
            PacketHandler.RegisterPacketHandlers();

            NewClientListener = new TcpListener(IPAddress.Parse(ServerIP), 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);
        }

        //Returns the entire list of ClientConnections
        public static List<ClientConnection> GetClientConnections()
        {
            List<ClientConnection> ClientConnections = new List<ClientConnection>();
            foreach (KeyValuePair<int, ClientConnection> ClientConnection in ActiveConnections)
                ClientConnections.Add(ClientConnection.Value);
            return ClientConnections;
        }

        //Returns a ClientConnection from its NetworkID
        public static ClientConnection GetClientConnection (int ClientID)
        {
            //Return null if theres no client with this ID number
            if (!ActiveConnections.ContainsKey(ClientID))
                return null;
            return ActiveConnections[ClientID];
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

            //Display a message showing that this client connection was successful
            MessageLog.Print("New client connected from " + NewConnection.Client.RemoteEndPoint.ToString());
        }

        //Shuts down the connection with a specific client and removes them from the active connections
        public static void CloseConnection(ClientConnection Connection)
        {
            //Flag this client as dead so it gets cleaned up by the GameWorld in its next pass
            Connection.ClientDead = true;
        }

        //Checks how much time has passed since we last heard from each of the active client connections, cleaning up any connections which have been inactive for too long
        public static void CheckConnections(float DeltaTime)
        {
            //Count down the timer until we need to perform a new connection check on all the clients
            NextConnectionCheck -= DeltaTime;

            //Check the status of all client connections and reset the timer whenever it reaches zero
            if(NextConnectionCheck <= 0.0f)
            {
                //Reset the timer for checking client connections again
                NextConnectionCheck = ConnectionCheckInterval;

                //Go through the entire list of active client connections and flag the ones which have had their connections timed out
                foreach (KeyValuePair<int, ClientConnection> Client in ActiveConnections)
                {
                    //Check how much time has passed since we last heard from them, flag their connection as dead if they have timed out
                    int LastHeard = Client.Value.LastCommunication.AgeInSeconds();
                    if (LastHeard >= ClientConnectionTimeout)
                        Client.Value.ClientDead = true;
                }
            }
        }

        //Cleans up any dead client connections, removing their character from the physics scene, telling other clients they are now gone etc.
        public static void CleanDeadClients(Simulation WorldSimulation)
        {
            //Split all the clients into two seperate lists, one containing all dead connections that need to be cleaned up, the other containing all the other connections who are still active
            List<ClientConnection> DeadClients = ClientSubsetFinder.GetDeadClients();
            List<ClientConnection> LivingClients = ClientSubsetFinder.GetLivingClients();

            //Loop through all of the dead clients who need to be cleaned up
            foreach(ClientConnection DeadClient in DeadClients)
            {
                MessageLog.Print(DeadClient.NetworkID + " client connection was cleaned up");

                //Check each DeadClient to see if they have one of their characters currently active in the game world
                if(DeadClient.InGame)
                {
                    //Save this characters values into the database
                    CharactersDatabase.SaveCharacterValues(DeadClient.CharacterName, DeadClient.CharacterPosition, DeadClient.CharacterRotation, DeadClient.CameraZoom, DeadClient.CameraXRotation, DeadClient.CameraYRotation);

                    //Remove the characters body from the servers world physics simulation
                    WorldSimulation.Bodies.Remove(DeadClient.BodyHandle);
                    WorldSimulation.Shapes.Remove(DeadClient.ShapeIndex);

                    //Tell all the living clients to remove this character from the game worlds on their end
                    foreach (ClientConnection LivingClient in LivingClients)
                        PlayerManagementPacketSender.SendRemoveOtherPlayer(LivingClient.NetworkID, DeadClient.CharacterName);

                    //Display a message showing that this character has been cleaned up from the game world
                    MessageLog.Print(DeadClient.CharacterName + " was removed from the game world after their connection timed out.");
                }
                else
                {
                    //DeadClients who werent ingame yet simply show a message that their connection has been closed properly
                    if (DeadClient.AccountName != "")
                        MessageLog.Print(DeadClient.AccountName + " has been logged out after their connection timed out.");
                    else
                        MessageLog.Print(DeadClient.NetworkID + " has been disconnected after their connection timed out.");
                }

                //Finally, after each DeadClient has been cleaned up, we remove them from the list of active network client connections
                ActiveConnections.Remove(DeadClient.NetworkID);
            }
        }

        //Updates the physics body position of any clients who have sent us a new position update since the last update
        public static void UpdateClientPositions(Simulation World)
        {
            //Get the list of clients with updated positions
            List<ClientConnection> UpdatedClients = ClientSubsetFinder.GetUpdatedClients();

            //Loop through them all so we can apply their new position values to their physics objects in the world simulation
            foreach(ClientConnection UpdatedClient in UpdatedClients)
            {
                //Use the NewPosition value to reassign a new ShapePose for the clients physics body
                UpdatedClient.ShapePose = new RigidPose(UpdatedClient.CharacterPosition, UpdatedClient.CharacterRotation);
                //Calculate a new Inertia value for the clients physics body
                UpdatedClient.PhysicsShape.ComputeInertia(1, out var Inertia);
                //Use the new ShapePose and Inertia values to assign a new BodyDescription to the client
                UpdatedClient.PhysicsBody = BodyDescription.CreateDynamic(UpdatedClient.ShapePose, Inertia, UpdatedClient.PhysicsDescription, UpdatedClient.ActivityDescription);
                //Apply this new body description to the clients physics body inside the gameworld physics simulation
                World.Bodies.ApplyDescription(UpdatedClient.BodyHandle, ref UpdatedClient.PhysicsBody);
                //Reset the clients updated position flag now that their physics body has been moved to the new position
                UpdatedClient.NewPosition = false;
            }
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