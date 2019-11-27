// ================================================================================================================================
// File:        ClientSubsetFinder.cs
// Description: Contains many helper functions for returning different subsets of the ConnectionManagers ActiveConnections Dictionary
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Logging;

namespace Server.Networking
{
    public static class ClientSubsetFinder
    {
        //Returns the client who is currently active in the game world with the given character name
        public static ClientConnection GetClientUsingCharacter(string CharacterName)
        {
            //Start with a list of all ingame clients
            List<ClientConnection> InGameClients = GetInGameClients();

            //Loop through these ingame cleints
            foreach(ClientConnection InGameClient in InGameClients)
            {
                //Return the client if they are using the character
                if (InGameClient.CharacterName == CharacterName)
                    return InGameClient;
            }

            //Return null if the client was not found
            return null;
        }

        //Returns a list of all client connections which have been flagged as having a new position value that needs to be applied
        public static List<ClientConnection> GetUpdatedClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> UpdatedClients = new List<ClientConnection>();

            //Fetch the entire list of current client connections and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach(ClientConnection ClientConnection in ClientConnections)
            {
                if (ClientConnection.NewPosition)
                    UpdatedClients.Add(ClientConnection);
            }

            //Return the final list of all the dead clients
            return UpdatedClients;
        }

        //Returns a list of all client connections which have been flagged as being dead
        public static List<ClientConnection> GetDeadClients()
        {
            //Create a new list to store all the dead clients in
            List<ClientConnection> DeadClients = new List<ClientConnection>();

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach(ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they have been flagged as dead
                if (ClientConnection.ClientDead)
                    DeadClients.Add(ClientConnection);
            }

            //Return the final list of all the dead clients
            return DeadClients;
        }
        
        //Returns a list of all client connections which have not been flagged as being dead
        public static List<ClientConnection> GetLivingClients()
        {
            //Create a new list to store all the living client connections
            List<ClientConnection> LivingClients = new List<ClientConnection>();

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach (ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they have not been flagged as dead
                if (!ClientConnection.ClientDead)
                    LivingClients.Add(ClientConnection);
            }

            //Return the final list of all the living clients
            return LivingClients;
        }

        //Returns all of the active client connections except for 1 with the matching ClientID that is provided
        public static List<ClientConnection> GetAllOtherClients(int ClientID)
        {
            //Create a new list to place all the clients into after taking them from the dictionary
            List<ClientConnection> OtherClients = new List<ClientConnection>();

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach (ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they arent the one we dont want
                if (ClientConnection.NetworkID != ClientID)
                    OtherClients.Add(ClientConnection);
            }

            //Return the final list of clients
            return OtherClients;
        }

        //Returns a list of all the clients currently in the game world playing with one of their characters
        public static List<ClientConnection> GetInGameClients()
        {
            //Create a new list to palce all the ingame clients into
            List<ClientConnection> InGameClients = new List<ClientConnection>();

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach (ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they are ingame
                if (ClientConnection.InGame)
                    InGameClients.Add(ClientConnection);
            }

            //Return the final list of InGame Clients
            return InGameClients;
        }

        //Returns a list of all the clients who havnt yet logged into the game world (still in the menus somewhere)
        public static List<ClientConnection> GetInMenuClients()
        {
            //Create a list to store all the clients who are not ingame
            List<ClientConnection> NotInGameClients = new List<ClientConnection>();

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach (ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they are in the menu
                if (!ClientConnection.InGame)
                    NotInGameClients.Add(ClientConnection);
            }

            //Return the final list of non ingame clients
            return NotInGameClients;
        }

        //Returns a list of all the other clients who are currently in the game world and playing with one of their characters
        public static List<ClientConnection> GetInGameClientsExceptFor(int ClientID)
        {
            //Start by getting the complete list of ingame clients
            List<ClientConnection> InGameClients = GetInGameClients();

            //Get the ClientConnection that we dont want in this list
            ClientConnection ExceptFor = ConnectionManager.GetClientConnection(ClientID);
            if (ExceptFor == null)
            {
                MessageLog.Print("ERROR: Client " + ClientID + " not found, unable to handle ingame clients list request.");
                return null;
            }

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

            //Fetch the entire list of current clients and loop through them all
            List<ClientConnection> ClientConnections = ConnectionManager.GetClientConnections();
            foreach (ClientConnection ClientConnection in ClientConnections)
            {
                //Add them to the list if they are ready to enter the game
                if (ClientConnection.WaitingToEnter)
                    ReadyToEnterClients.Add(ClientConnection);
            }

            //Return the final list of all the clients ready to enter the game
            return ReadyToEnterClients;
        }
    }
}
