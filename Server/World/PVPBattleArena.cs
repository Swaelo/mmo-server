// ================================================================================================================================
// File:        PVPBattleArena.cs
// Description: Keeps track of which players are inside and outside the battle arena, and when they enter/leave from it
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using Server.Data;
using Server.Networking;
using Server.Networking.PacketSenders;

namespace Server.World
{
    public static class PVPBattleArena
    {
        //Define the 4 corner locations of the battle arena
        private static float XMinimum = 18.707f;
        private static float XMaximum = 40.931f;
        private static float ZMinimum = -10.18f;
        private static float ZMaximum = 12.046f;

        //All players split into two lists, who is inside and outside of the PVP Battle Arena
        public static List<CharacterData> CharactersInside = new List<CharacterData>();
        public static List<CharacterData> CharactersOutside = new List<CharacterData>();

        //List of players who either just entered into or exited out from the battle arena
        public static List<CharacterData> CharactersEntering = new List<CharacterData>();
        public static List<CharacterData> CharactersExiting = new List<CharacterData>();

        //Takes a list of every player currently in the game world and usues that to update all the lists that we keep track of here
        public static void UpdateArenaInhabitants(List<CharacterData> Characters)
        {
            //Figure out who is currently inside and outside of the battle arena
            List<CharacterData> NewCharactersInside = new List<CharacterData>();
            List<CharacterData> NewCharactersOutside = new List<CharacterData>();
            foreach (CharacterData Character in Characters)
            {
                //Check the characters position against the X/Z bounds of the battle arena
                bool XInside = Character.Position.X >= XMinimum && Character.Position.X <= XMaximum;
                bool ZInside = Character.Position.Z >= ZMinimum && Character.Position.Z <= ZMaximum;
                //If both checks pass the character is inside, otherwise they're otherside
                if (XInside && ZInside)
                    NewCharactersInside.Add(Character);
                else
                    NewCharactersOutside.Add(Character);
            }

            //Compare the new Inside/Outside lists against those from the last update to find out who just recently entered/exited from the battle arena
            CharactersEntering.Clear();
            CharactersExiting.Clear();
            foreach (CharacterData Character in NewCharactersInside)
            {
                //If from the previous update to this one they moved from the outside list to the inside then we know they only just entered the arena
                if (CharactersOutside.Contains(Character) && NewCharactersInside.Contains(Character))
                    CharactersEntering.Add(Character);
            }
            foreach (CharacterData Character in NewCharactersOutside)
            {
                //If from the previous update to this one they moved from the inside list to the outside then we know they only just exited the arena
                if (CharactersInside.Contains(Character) && NewCharactersOutside.Contains(Character))
                    CharactersExiting.Add(Character);
            }

            //Now store all the new inside/outside lists so we can check against them in the next update
            CharactersInside = NewCharactersInside;
            CharactersOutside = NewCharactersOutside;
        }

        //Sends a message to any player who just entered into or left from the battle arena that their PVP status has changed
        public static void AlertTravellers()
        {
            //First message all arrivals letting them know pvp is active in this area
            foreach (CharacterData Character in CharactersEntering)
            {
                //Get the client who controls this characters and send the message to them
                ClientConnection CharacterClient = ConnectionManager.GetClient(Character);
                SystemPacketSender.SendUIMessage(CharacterClient.ClientID, "Now entering PVP area, beware...");
            }

            //Also message any departures letting them know pvp is no longer active
            foreach (CharacterData Character in CharactersExiting)
            {
                ClientConnection CharacterClient = ConnectionManager.GetClient(Character);
                SystemPacketSender.SendUIMessage(CharacterClient.ClientID, "Leaving PVP area, you are safe again.");
            }
        }

        //Provides a list of the ClientConnections who's characters are currently inside the PVP Battle Arena
        public static List<ClientConnection> GetClientsInside()
        {
            List<ClientConnection> ClientsInside = new List<ClientConnection>();
            foreach (CharacterData CharacterInside in CharactersInside)
                ClientsInside.Add(ConnectionManager.GetClient(CharacterInside));
            return ClientsInside;
        }
    }
}
