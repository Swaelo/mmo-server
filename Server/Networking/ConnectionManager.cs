// ================================================================================================================================
// File:        ConnectionManager.cs
// Description: Implementation of the ConnectionManager class using WebSockets, allowing WebGL clients to connect
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Server.Networking.PacketSenders;
using Server.Logging;
using Server.Database;
using BepuPhysics;
using ServerUtilities;
using ContentRenderer;
using ContentRenderer.UI;

namespace Server.Networking
{
    public static class ConnectionManager
    {
        public static TcpListener NewClientListener;    //Receives new incoming client connections
        private static Dictionary<int, ClientConnection> ActiveConnections = new Dictionary<int, ClientConnection>();    //Each active client connection mapped to their network ID

        public static float ConnectionCheckInterval = 2.5f; //How often to check for dead connections
        public static float NextConnectionCheck = 2.5f;   //How long until the next connection check will be performed
        private static int ClientConnectionTimeout = 15;    //How many seconds must pass without hearing from a client before we flag them as dead

        private static TextBuilder ConnectionsText = new TextBuilder(2048); //Used to render information about the active game cients to the window UI

        //Sets up the connection manager and starts listening for new incoming client connections
        public static void InitializeManager(string ServerIP)
        {
            PacketHandler.RegisterPacketHandlers();

            NewClientListener = new TcpListener(IPAddress.Parse(ServerIP), 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);
        }

        //Renders information about all the current clients connections to the window UI
        public static void RenderClientsInfo(Renderer Renderer, Vector2 Position, float FontSize, Vector3 FontColor, Font Font)
        {
            //Display an initial string at the start indicating what is being shown here
            Renderer.TextBatcher.Write(ConnectionsText.Clear().Append("---Active Clients Info---"), Position, FontSize, FontColor, Font);

            //Get the current list of active client connections
            List<ClientConnection> Clients = GetClientConnections();

            //Offset the Y value before we start rendering all the clients information to the log
            Position.Y += FontSize * 1.5f;

            //Loop through all of the active client connections
            foreach(ClientConnection Client in Clients)
            {
                //Check if each one is logged into an account, and if they are ingame with a character of theirs
                bool LoggedIn = Client.Account.Username != "";
                bool InGame = Client.Character.Name != "";

                //Create a string with all the clients info, start by putting their NetworkID
                string ClientInfo = "<" + Client.NetworkID + ">";

                //Add their current account name if they are logged into one
                if (LoggedIn)
                    ClientInfo += ", <" + Client.Account.Username + ">";

                //Add their characters information if they are ingame with one
                if (InGame)
                    ClientInfo +=
                        /*name*/    ", <" + Client.Character.Name + ">, " +
                        /*pos*/     "<" + Client.Character.Position.X + "," + Client.Character.Position.Y + "," + Client.Character.Position.Z + ">, " +
                        /*HP*/      "<" + Client.Character.CurrentHealth + "/" + Client.Character.MaxHealth + ">";

                //Draw the clients information to the UI and offset the position value for drawing the next clients information
                Renderer.TextBatcher.Write(ConnectionsText.Clear().Append(ClientInfo), Position, FontSize, FontColor, Font);
                Position.Y += FontSize * 1.2f;
            }
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

                //Send a message to all active client connections requesting they immediately let us know they are still connected
                foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
                {
                    //Ask this client if they are still connected to us
                    SystemPacketSender.SendStillConnectedCheck(Client.Key);

                    //Check how much time has passed since we last heard from them, flag their connection as dead if too much time has passed
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
                    CharactersDatabase.SaveCharacterData(DeadClient.Character);

                    //Remove the characters body from the servers world physics simulation
                    DeadClient.RemovePhysicsBody(WorldSimulation);

                    //Tell all the living clients to remove this character from the game worlds on their end
                    foreach (ClientConnection LivingClient in LivingClients)
                        PlayerManagementPacketSender.SendRemoveRemotePlayer(LivingClient.NetworkID, DeadClient.Character.Name, DeadClient.Character.IsAlive);

                    //Display a message showing that this character has been cleaned up from the game world
                    MessageLog.Print(DeadClient.Character.Name + " was removed from the game world after their connection timed out.");
                }
                else
                {
                    //DeadClients who werent ingame yet simply show a message that their connection has been closed properly
                    if (DeadClient.Character.Account != "")
                        MessageLog.Print(DeadClient.Character.Account + " has been logged out after their connection timed out.");
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
                //Ignore any clients who are dead or not ingame
                if (UpdatedClient.ClientDead || !UpdatedClient.InGame)
                    return;

                //Use the NewPosition value to reassign a new ShapePose for the clients physics body
                UpdatedClient.ShapePose = new RigidPose(UpdatedClient.Character.Position, UpdatedClient.Character.Rotation);
                //Calculate a new Inertia value for the clients physics body
                UpdatedClient.PhysicsShape.ComputeInertia(1, out var Inertia);
                //Use the new ShapePose and Inertia values to assign a new BodyDescription to the client
                UpdatedClient.PhysicsBody = BodyDescription.CreateDynamic(UpdatedClient.ShapePose, Inertia, UpdatedClient.PhysicsDescription, UpdatedClient.ActivityDescription);
                //Apply this new body description to the clients physics body inside the gameworld physics simulation
                World.Bodies.ApplyDescription(UpdatedClient.BodyHandle, ref UpdatedClient.PhysicsBody);
                //Reset the clients updated position flag now that their physics body has been moved to the new position
                UpdatedClient.Character.NewPosition = false;
            }
        }
        
        //Checks everyone who is logged on to know if an account is already in use or not
        public static bool IsAccountLoggedIn(string AccountName)
        {
            foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
            {
                if (Client.Value.Character.Account == AccountName)
                    return true;
            }
            return false;
        }
    }
}