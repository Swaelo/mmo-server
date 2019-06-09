// ================================================================================================================================
// File:        ConnectionManager.cs
// Description: Keeps track of all the game clients which are currently connected to the server, and allows you to managed those
//              connections, and send packets to the clients as needed
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using BepuUtilities;
using Server.Entities;
using Server.Scenes;
using Server.Database;
using Server.Maths;
using Server.Interface;
using Server.Networking.PacketSenders;

namespace Server.Networking
{
    public static class ConnectionManager
    {
        //Listener to receive new incoming connection, and a dictionary of all the active connections currently ongoing
        public static TcpListener NewClientListener;
        public static Dictionary<int, ClientConnection> ActiveConnections = new Dictionary<int, ClientConnection>();

        //Sets up the connection manager and starts listening for new incoming game client connections
        public static void InitializeManager()
        {
            //Register all the packet reader handler functions
            PacketReceiver.RegisterPacketHandlers();

            //Start listening to new incoming client connections
            NewClientListener = new TcpListener(IPAddress.Any, 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);
        }

        //Callback event triggered when a new client has connected to the server
        private static void NewClientConnected(IAsyncResult Result)
        {
            Log.PrintDebugMessage("Networking.ConnectionManager new client connected");

            //Store this new client locally so we can handle it in a bit, but first reset the listener 
            //so it starts listening for other new clients again straight away, we dont want to miss any
            //if they are coming in rapidly
            TcpClient NewConnection = NewClientListener.EndAcceptTcpClient(Result);
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);

            //Set up a new ClientConnection object to handle communication with the new client that connected
            //and add it into the list with all the others
            ClientConnection NewClient = new ClientConnection(NewConnection);
            ActiveConnections.Add(NewClient.NetworkID, NewClient);
        }

        //Severs the network connection to one of the active game clients and removes it from the list of active connections
        public static void CloseConnection(ClientConnection Connection)
        {
            //Remove their entity from the game world
            EntityManager.HandleClientDisconnect(Connection);

            //Remove them from the list of active client connections
            ActiveConnections.Remove(Connection.NetworkID);

            //TODO: Tell all the other clients this player has left the game world
        }

        //Returns a subset of the ClientConnections list, containing only this clients who are currently logged in and playing one of their characters
        public static List<ClientConnection> GetActiveClients()
        {
            //Create a new list to store the active clients
            List<ClientConnection> ActiveClients = new List<ClientConnection>();

            //Loop through all the client connections, searching for the ones which are currently playing the game
            foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
            {
                //Add them to the list if they are currently playing
                if (Client.Value.InGame)
                    ActiveClients.Add(Client.Value);
            }

            //return the final list of clients who are currently logged in and playing the game
            return ActiveClients;
        }

        //Returns a subset of the ClientConnections list, containing the clients active in the game world, removing the target client from the list if they are in it
        public static List<ClientConnection> GetActiveClientsExceptFor(int ClientID)
        {
            List<ClientConnection> ActiveClients = GetActiveClients();
            ActiveClients.Remove(ActiveConnections[ClientID]);
            return ActiveClients;
        }

        //Sends a network packet through to one of the active game clients
        public static void SendPacketTo(int ClientID, byte[] PacketData)
        {
            ActiveConnections[ClientID].DataStream.BeginWrite(PacketData, 0, PacketData.Length, null, null);
        }

        //Checks if any of the active clients are currently logged into a given account
        public static bool IsAccountLoggedIn(string AccountName)
        {
            foreach (KeyValuePair<int, ClientConnection> Client in ActiveConnections)
            {
                if (Client.Value.AccountName == AccountName)
                    return true;
            }

            return false;
        }

        //Notifies all active client connections that a player has disconnected from the game world
        public static void HandleClientDisconnect(ClientConnection Client)
        {
            //Remove them from the client list
            ActiveConnections.Remove(Client.NetworkID);
            //Tell all the other clients this player has left the game world
            List<ClientConnection> ActiveClients = GetActiveClients();
            PlayerManagementPacketSender.SendListRemoveOtherCharacter(ActiveClients, Client.CharacterName);
        }
    }
}
