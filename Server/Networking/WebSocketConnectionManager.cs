// ================================================================================================================================
// File:        WebSocketConnectionManager.cs
// Description: Implementation of the ConnectionManager class using WebSockets, allowing WebGL clients to connect
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Server.Interface;

namespace Server.Networking
{
    public static class WebSocketConnectionManager
    {
        public static TcpListener NewClientListener;
        public static Dictionary<int, WebSocketClientConnection> ActiveConnections = new Dictionary<int, WebSocketClientConnection>();

        public static List<WebSocketClientConnection> GetAllClients()
        {
            List<WebSocketClientConnection> Clients = new List<WebSocketClientConnection>();
            foreach (KeyValuePair<int, WebSocketClientConnection> Connection in ActiveConnections)
                Clients.Add(Connection.Value);
            return Clients;
        }

        public static List<WebSocketClientConnection> GetAllOtherClients(int ClientID)
        {
            List<WebSocketClientConnection> OtherClients = new List<WebSocketClientConnection>();
            foreach (KeyValuePair<int, WebSocketClientConnection> Connection in ActiveConnections)
                if (Connection.Key != ClientID)
                    OtherClients.Add(Connection.Value);
            return OtherClients;
        }

        public static void InitializeManager(string ServerIP)
        {
            WebSocketPacketHandler.RegisterPacketHandlers();

            NewClientListener = new TcpListener(IPAddress.Parse(ServerIP), 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);
        }

        private static void NewClientConnected(IAsyncResult Result)
        {
            Log.PrintDebugMessage("Networking.WebSocketConnectionManager new client connected");

            TcpClient NewConnection = NewClientListener.EndAcceptTcpClient(Result);
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientConnected), null);

            WebSocketClientConnection NewClient = new WebSocketClientConnection(NewConnection);
            ActiveConnections.Add(NewClient.NetworkID, NewClient);
        }

        public static void CloseConnection(WebSocketClientConnection Connection)
        {
            ActiveConnections.Remove(Connection.NetworkID);
        }

        //Sends a packet of information to all active clients
        public static void MessageAllClients(string ClientMessage)
        {
            foreach (KeyValuePair<int, WebSocketClientConnection> Client in ActiveConnections)
                Client.Value.SendPacket(ClientMessage);
        }

        //Sends a packet of information to 1 specific client
        public static void MessageClient(int ClientID, string ClientMessage)
        {
            ActiveConnections[ClientID].SendPacket(ClientMessage);
        }

        //Returns a list of all the active game clients
        public static List<WebSocketClientConnection> GetActiveClients()
        {
            List<WebSocketClientConnection> ActiveClients = new List<WebSocketClientConnection>();

            foreach(KeyValuePair<int, WebSocketClientConnection> Client in ActiveConnections)
            {
                if (Client.Value.InGame)
                    ActiveClients.Add(Client.Value);
            }

            return ActiveClients;
        }

        //Returns a list of all the active game clients, except for one
        public static List<WebSocketClientConnection> GetActiveClientsExceptFor(int ClientID)
        {
            List<WebSocketClientConnection> ActiveClients = GetActiveClients();
            ActiveClients.Remove(ActiveConnections[ClientID]);
            return ActiveClients;
        }

        //Checks everyone who is logged on to know if an account is already in use or not
        public static bool IsAccountLoggedIn(string AccountName)
        {
            foreach(KeyValuePair<int, WebSocketClientConnection> Client in ActiveConnections)
            {
                if (Client.Value.AccountName == AccountName)
                    return true;
            }
            return false;
        }
    }
}