// ================================================================================================================================
// File:        WebSocketConnectionManager.cs
// Description: Implementation of the ConnectionManager class using WebSockets, allowing WebGL clients to connect
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
        public static TcpClient ConnectedClient;
        public static Dictionary<int, WebSocketClientConnection> ActiveConnections = new Dictionary<int, WebSocketClientConnection>();

        public static void InitializeManager(string ServerIP)
        {
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
    }
}