// ================================================================================================================================
// File:        ClientSubsetFinder.cs
// Description: Contains many helper functions for returning different subsets of the ConnectionManagers ActiveConnections Dictionary
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;

namespace Server.Networking
{
    public static class ClientSubsetFinder
    {
        //Returns a list of all client connections which have been flagged as having a new position value that needs to be applied
        public static List<ClientConnection> GetUpdatedClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> UpdatedClients = new List<ClientConnection>();

            //Loop through the entire dictionary of client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
            {
                //Add them to the list if they have been flagged as having a new position value
                if (Connection.Value.NewPositionReceived)
                    UpdatedClients.Add(Connection.Value);
            }

            //Return the final list of all the dead clients
            return UpdatedClients;
        }

        //Returns a list of all client connections which have been flagged as being dead
        public static List<ClientConnection> GetDeadClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> DeadClients = new List<ClientConnection>();

            //Loop through the entire dictionary of client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
            {
                //Add them to the list if they have been flagged as dead
                if (Connection.Value.ClientDead)
                    DeadClients.Add(Connection.Value);
            }

            //Return the final list of all the dead clients
            return DeadClients;
        }
        
        //Returns a list of all client connections which have not been flagged as being dead
        public static List<ClientConnection> GetLivingClients()
        {
            //Create a new list to store all the living client connections
            List<ClientConnection> LivingClients = new List<ClientConnection>();

            //Loop through the entire dictionary of active client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
            {
                //Add them to the list if they have not been flagged as dead
                if (!Connection.Value.ClientDead)
                    LivingClients.Add(Connection.Value);
            }

            //Return the final list of all the living clients
            return LivingClients;
        }
        
        //Returns all of the active client connections in a List format
        public static List<ClientConnection> GetAllClients()
        {
            //Create a new list to place all the clients into after taking them from the dictionary
            List<ClientConnection> Clients = new List<ClientConnection>();

            //Loop through the entire dictionary, placing each client object into the new list
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
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
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
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
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
            {
                //Add any clients who are ingame to the list
                if (Connection.Value.InGame)
                    InGameClients.Add(Connection.Value);
            }

            //Return the final list of InGame Clients
            return InGameClients;
        }

        //Returns a list of all the clients who havnt yet logged into the game world (still in the menus somewhere)
        public static List<ClientConnection> GetInMenuClients()
        {
            //Create a list to store all the clients who are not ingame
            List<ClientConnection> NotInGameClients = new List<ClientConnection>();

            //Loop through the entire list of active client connections
            foreach (KeyValuePair<int, ClientConnection> Connection in ConnectionManager.ActiveConnections)
            {
                //Add any clients not ingame to the list
                if (!Connection.Value.InGame)
                    NotInGameClients.Add(Connection.Value);
            }

            //Return the final list of non ingame clients
            return NotInGameClients;
        }

        //Returns a list of all the other clients who are currently in the game world and playing with one of their characters
        public static List<ClientConnection> GetInGameClientsExceptFor(int ClientID)
        {
            //Start by getting the complete list of ingame clients
            List<ClientConnection> InGameClients = GetInGameClients();

            //Get the ClientConnection that needs to be removed from the list before we return it
            ClientConnection ExceptFor = ConnectionManager.ActiveConnections[ClientID];

            //Remove the excepted client from the list if its in there
            if (InGameClients.Contains(ExceptFor))
                InGameClients.Remove(ExceptFor);

            //Return the final list of all the other ingame clients
            return InGameClients;
        }

        //Returns a list of all clients who have their WaitingToEnter flag set
        public static List<ClientConnection> GetClientsReadyToEnter()
        {
            //Create a new list to store the client who are ready
            List<ClientConnection> ReadyToEnterClients = new List<ClientConnection>();

            //Loop through the entire list of clients, finding which ones are ready and adding those ones to the list
            foreach(KeyValuePair<int, ClientConnection> Client in ConnectionManager.ActiveConnections)
            {
                if (Client.Value.WaitingToEnter)
                    ReadyToEnterClients.Add(Client.Value);
            }

            //Return the final list of all the clients ready to enter the game
            return ReadyToEnterClients;
        }
    }
}
