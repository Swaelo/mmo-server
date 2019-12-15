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
using Server.World;
using Server.Data;
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

        //Check with clients every so often to see if they're still there
        private static float ClientCheckInterval = 1.0f;
        private static float NextClientCheck = 1.0f;
        private static float ClientConnectionTimeout = 15;

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

        //Gets a ClientConnection from its ClientID
        public static ClientConnection GetClient(int ClientID)
        {
            return ActiveConnections.ContainsKey(ClientID) ? ActiveConnections[ClientID] : null;
        }

        //Gets a ClientConnection who is controlling the given character
        public static ClientConnection GetClient(CharacterData Character)
        {
            //Loop through all the active clients
            foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
            {
                //Return the client who is controlling the provided character
                if (Client.Value.Character == Character)
                    return Client.Value;
            }
            return null;
        }

        public static void CheckClients(float DeltaTime)
        {
            NextClientCheck -= DeltaTime;
            if(NextClientCheck <= 0.0f)
            {
                NextClientCheck = ClientCheckInterval;
                foreach(KeyValuePair<int, ClientConnection> Client in ActiveConnections)
                {
                    SystemPacketSender.SendStillConnectedCheck(Client.Key);

                    int LastHeard = Client.Value.LastCommunication.AgeInSeconds();
                    if (LastHeard >= ClientConnectionTimeout)
                        Client.Value.ConnectionDead = true;
                }
            }
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
                    DeadClient.Character.RemoveBody(World);
                    foreach (ClientConnection LivingClient in ClientSubsetFinder.GetInGameLivingClientsExceptFor(DeadClient.ClientID))
                        PlayerManagementPacketSender.SendRemoveRemotePlayer(LivingClient.ClientID, DeadClient.Character.Name, DeadClient.Character.IsAlive);
                }
                ActiveConnections.Remove(DeadClient.ClientID);
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

                UpdatedClient.Character.UpdateBody(World);
            }
        }

        public static void PerformPlayerAttacks(Simulation World)
        {
            //Get a list of every client who's character is performing an attack this turn, and a list of every client who's character is currently inside the PVP battle arena
            List<ClientConnection> AttackingClients = ClientSubsetFinder.GetClientsAttacking();
            List<ClientConnection> ClientsInArena = PVPBattleArena.GetClientsInside();

            //Loop through all of the attacking clients so their attacks can be processed
            foreach(ClientConnection AttackingClient in AttackingClients)
            {
                //If the AttackingClient is inside the BattleArena, check their attack against the other players also in the arena
                if(ClientsInArena.Contains(AttackingClient))
                {
                    //Get a list of all the other clients in the arena to check the attack against
                    List<ClientConnection> OtherClients = PVPBattleArena.GetClientsInside();
                    OtherClients.Remove(AttackingClient);
                    foreach(ClientConnection OtherClient in OtherClients)
                    {
                        //Check the distance between the clients attack position and the other client to see if the attack hit
                        float AttackDistance = Vector3.Distance(AttackingClient.Character.AttackPosition, OtherClient.Character.Position);
                        if(AttackDistance <= 1.5f)
                        {
                            //Reduce the other characters health
                            OtherClient.Character.CurrentHealth -= 1;

                            //Send a damage alert to all clients if they survive the attack
                            if(OtherClient.Character.CurrentHealth > 0)
                            {
                                CombatPacketSenders.SendLocalPlayerTakeHit(OtherClient.ClientID, OtherClient.Character.CurrentHealth);
                                foreach (ClientConnection OtherOtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(OtherClient.ClientID))
                                    CombatPacketSenders.SendRemotePlayerTakeHit(OtherOtherClient.ClientID, OtherClient.Character.Name, OtherClient.Character.CurrentHealth);
                            }
                            
                            //Send a death alert to all clients if they are killed by the attack
                            if(OtherClient.Character.CurrentHealth <= 0)
                            {
                                OtherClient.Character.IsAlive = false;
                                OtherClient.Character.RemoveBody(World);
                                CombatPacketSenders.SendLocalPlayerDead(OtherClient.ClientID);
                                foreach (ClientConnection OtherOtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(OtherClient.ClientID))
                                    CombatPacketSenders.SendRemotePlayerDead(OtherOtherClient.ClientID, OtherClient.Character.Name);
                            }
                        }
                    }
                }
                //Disable the clients Attack flag now that the attack has been processed
                AttackingClient.Character.AttackPerformed = false;
            }
        }
    }
}