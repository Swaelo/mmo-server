// ================================================================================================================================
// File:        ItemManager.cs
// Description: Keeps track of what items are in the game world, allows functions to easily add more and provide all item info to players
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using BepuPhysics;
using Server.Database;
using Server.Interface;
using Server.Networking;
using Server.Networking.PacketSenders;

namespace Server.GameItems
{
    public static class ItemManager
    {
        //Queue up a list of item pickups that need to be removed from the scene, the GameWorld will access this queue and remove them when its able to
        public static List<GameItem> ItemsToRemove = new List<GameItem>();

        //Checks if an item is currently in the queue to be removed from the game
        public static bool IsItemInRemoveQueue(int ItemID)
        {
            foreach(GameItem ItemPickup in ItemsToRemove)
            {
                if (ItemPickup.ItemID == ItemID)
                    return true;
            }
            return false;
        }

        //Removes all the item pickups from the game world simulation which have been queued up to be removed
        public static void ClearRemoveQueue(Simulation GameWorld)
        {
            //Loop through each item in the queue
            foreach(GameItem ItemPickup in ItemsToRemove)
            {
                //Remove them all from the game world simulation
                GameWorld.Bodies.Remove(ItemPickup.ItemColliderHandle);
                GameWorld.Shapes.Remove(ItemPickup.ItemShapeIndex);
                //Remove the item from the active item dictionary if its still in there for some reason
                if (ActiveItemDictionary.ContainsKey(ItemPickup.ItemID))
                    ActiveItemDictionary.Remove(ItemPickup.ItemID);
            }

            //Now clear the list since all the items in it are gone now
            ItemsToRemove.Clear();
        }

        //The universal item ID number to be assigned to the next item that is created
        private static int NextItemID = -1;

        //Keep a list of all the items that have been dropped into the game world and are still available to be taken by players
        public static Dictionary<int, GameItem> ActiveItemDictionary = new Dictionary<int, GameItem>();

        //Returns a list of every single active item pickup, all values extracted from the dictionary
        public static List<GameItem> GetActiveItemList()
        {
            //Create a new list to store all the active item pickups
            List<GameItem> ActiveItemList = new List<GameItem>();

            //Loop through the dictionary, adding each value into the list
            foreach (KeyValuePair<int, GameItem> Entry in ActiveItemDictionary)
                ActiveItemList.Add(Entry.Value);

            //Return the final list of all the active item pickups
            return ActiveItemList;
        }

        //Load in from the database what the next unique item ID to be assigned will be
        public static void InitializeItemManager()
        {
            NextItemID = GlobalsDatabase.GetNextItemID();
        }

        //Save into the database what the next unique item ID to be assigned will be
        public static void SaveNextID()
        {
            GlobalsDatabase.SaveNextItemID(NextItemID);
        }

        //Returns the next newly ItemID, updates database what the next one will be
        public static int GetNextID()
        {
            //Store the new item ID in a local variable
            int NewItemID = NextItemID;

            //Increment the next item ID value and back it up to the database
            NextItemID++;
            SaveNextID();

            //Return the new item ID value
            return NewItemID;
        }

        /// <summary>
        /// Spawns a new item pickup into the game world that players are able to pick up
        /// </summary>
        /// <param name="ItemNumber">The game items unique number identifer used to fetch all of its data from the ItemList</param>
        /// <param name="ItemSpawnLocation">Location in the game world where the item will be instantiated</param>
        public static void AddNewItemPickup(int ItemNumber, Simulation GameWorld, Vector3 ItemSpawnLocation)
        {
            //Create the new item pickup and store it in the dictionary with all the other active pickup items
            GameItem ItemPickup = new GameItem(ItemNumber, GameWorld, ItemSpawnLocation);
            ItemPickup.ItemID = GetNextID();
            ActiveItemDictionary.Add(ItemPickup.ItemID, ItemPickup);

            //Tell all the active game clients to spawn this item pickup into their game world
            ItemManagementPacketSender.SendAllSpawnItemPickup(ItemPickup);
        }

        //Queues up an item to be removed from the game world in the next GameWorld.Update call
        public static void QueueRemoveItemPickup(GameItem ItemPickup)
        {
            ItemsToRemove.Add(ItemPickup);
        }
    }
}
