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
        //Listener for accepting new client connections, list of active connections and UI component for displaying all their information to the UI
        public static TcpListener NewClientListener;
        private static Dictionary<int, ClientConnection> ActiveConnections = new Dictionary<int, ClientConnection>();
        private static TextBuilder ClientsInfo = new TextBuilder(2048);

        //Sets up packet handlers and starts listening for new client connections
        public static void Initialize(string ServerIP)
        {
            PacketHandler.RegisterPacketHandlers();
            NewClientListener = new TcpListener(IPAddress.Parse(ServerIP), 5500);
            NewClientListener.Start();
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientEvent), null);
        }

        //Event triggered when new client has connected to the server
        private static void NewClientEvent(IAsyncResult Result)
        {
            //Place this connection into a new TcpClient, then reset the client listener so we can keep getting new connections
            TcpClient NewConnection = NewClientListener.EndAcceptTcpClient(Result);
            NewClientListener.BeginAcceptTcpClient(new AsyncCallback(NewClientEvent), null);
            //Store the new connection with the other clients
            ClientConnection NewClient = new ClientConnection(NewConnection);
            ActiveConnections.Add(NewClient.ClientID, NewClient);
        }

        //Returns all the ClientConnections in a List
        public static List<ClientConnection> GetClients()
        {
            List<ClientConnection> Clients = new List<ClientConnection>();
            foreach (KeyValuePair<int, ClientConnection> Client in ActiveConnections)
                Clients.Add(Client.Value);
            return Clients;
        }

        public static ClientConnection GetClient(int ClientID)
        {
            return ActiveConnections.ContainsKey(ClientID) ? ActiveConnections[ClientID] : null;
        }

        //Checks if anyone logged in with the given username
        public static bool AccountLoggedIn(string AccountName)
        {
            foreach (KeyValuePair<int, ClientConnection> Client in ActiveConnections)
                if (Client.Value.Character.Account == AccountName)
                    return true;
            return false;
        }

        //Cleans up any dead connections, removing their character from the world, and telling other clients to do the same on their end
        public static void CleanDeadClients(Simulation World)
        {
            foreach(ClientConnection DeadClient in ClientSubsetFinder.GetDeadClients())
            {
                //Backup / Remove from World and alert other clients about any ingame dead clients
                if(DeadClient.Character.InGame)
                {
                    CharactersDatabase.SaveCharacterData(DeadClient.Character);
                    World.Bodies.Remove(DeadClient.Character.BodyHandle);
                    World.Shapes.Remove(DeadClient.Character.BodyIndex);
                    foreach (ClientConnection LivingClient in ClientSubsetFinder.GetInGameLivingClientsExceptFor(DeadClient.ClientID))
                        PlayerManagementPacketSender.SendRemoveRemotePlayer(LivingClient.ClientID, DeadClient.Character.Name, DeadClient.Character.IsAlive);
                }
            }
        }

        //Adds any clients into the game world who are waiting for it
        public static void AddNewClients(Simulation World)
        {
            foreach(ClientConnection NewClient in ClientSubsetFinder.GetClientsReadyToEnter())
            {
                NewClient.Character.InitializeBody(World, NewClient.Character.Position);
                NewClient.Character.WaitingToEnter = false;
                NewClient.Character.InGame = true;
                PlayerManagementPacketSender.SendPlayerBegin(NewClient.ClientID);
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(NewClient.ClientID))
                    PlayerManagementPacketSender.SendAddRemotePlayer(OtherClient.ClientID, NewClient.Character);
            }
        }

        //Respawns any clients characters who were dead and clicked the respawn button
        public static void RespawnDeadPlayers(Simulation World)
        {
            foreach(ClientConnection RespawningClient in ClientSubsetFinder.GetClientsAwaitingRespawn())
            {
                RespawningClient.Character.SetDefaultValues();
                RespawningClient.Character.InitializeBody(World, RespawningClient.Character.Position);
                RespawningClient.Character.IsAlive = true;
                CombatPacketSenders.SendLocalPlayerRespawn(RespawningClient.ClientID, RespawningClient.Character);
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(RespawningClient.ClientID))
                    CombatPacketSenders.SendRemotePlayerRespawn(OtherClient.ClientID, RespawningClient.Character);
                RespawningClient.Character.WaitingToRespawn = false;
            }
        }

        public static void UpdateClientPositions(Simulation World)
        {
            foreach(ClientConnection UpdatedClient in ClientSubsetFinder.GetUpdatedClients())
            {
                if (UpdatedClient.ConnectionDead || !UpdatedClient.Character.InGame)
                    continue;

                UpdatedClient.Character.UpdateBody(World, UpdatedClient.Character.Position);
            }
        }

        public static void PerformPlayerAttacks(Simulation World)
        {
            foreach(ClientConnection AttackingClient in ClientSubsetFinder.GetClientsAttacking())
            {
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameLivingClientsExceptFor(AttackingClient.ClientID))
                {
                    float AttackDistance = Vector3.Distance(OtherClient.Character.Position, AttackingClient.Character.AttackPosition);
                    if(AttackDistance < 1.5f)
                    {
                        OtherClient.Character.CurrentHealth -= 1;
                        if(OtherClient.Character.CurrentHealth > 0)
                        {
                            CombatPacketSenders.SendLocalPlayerTakeHit(OtherClient.ClientID, OtherClient.Character.CurrentHealth);
                            foreach (ClientConnection OtherOtherCLient in ClientSubsetFinder.GetInGameClientsExceptFor(OtherClient.ClientID))
                                CombatPacketSenders.SendRemotePlayerTakeHit(OtherOtherCLient.ClientID, OtherClient.Character.Name, OtherClient.Character.CurrentHealth);
                        }
                        OtherClient.Character.IsAlive = false;
                        OtherClient.Character.RemoveBody(World);
                        CombatPacketSenders.SendLocalPlayerDead(OtherClient.ClientID);
                        foreach (ClientConnection OtherOtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(OtherClient.ClientID))
                            CombatPacketSenders.SendRemotePlayerDead(OtherOtherClient.ClientID, OtherClient.Character.Name);
                    }
                }
                AttackingClient.Character.AttackPerformed = false;
            }
        }
    }
}