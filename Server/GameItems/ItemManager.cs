// ================================================================================================================================
// File:        ItemManager.cs
// Description: Keeps track of what items are in the game world, allows functions to easily add more and provide all item info to players
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using Server.Database;

namespace Server.GameItems
{
    public static class ItemManager
    {
        //The universal item ID number to be assigned to the next item that is createrd
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

        //Add a new item pickup into the game world
        public static void AddItemPickup(int ItemNumber, Vector3 ItemPosition)
        {
            //Gather information regarding the new item pickup about to be added into the game world
            string NewItemName = ItemList.GetItemName(ItemNumber);
            string NewItemType = ItemList.GetItemType(ItemNumber);

            //Instantiate this new item pickup into the servers game world, store it in the dictionary with the others
            GameItem NewItem = new GameItem(NewItemName, NewItemType, ItemNumber, GetNextID(), ItemPosition);
            ActiveItemDictionary.Add(NewItem.ItemID, NewItem);

            //Instruct all active clients to spawn this new item pickup into the game worlds
            //PacketManager.SendListSpawnItem(ConnectionManager.GetActiveClients(), NewItem);
        }

        //Adds an item pickup into the game world with a pre-existing ID value
        public static void AddItemPickup(int ItemNumber, int ItemID, Vector3 ItemLocation)
        {
            //Gather information regarding teh new item pickup about to be added into the game world
            string NewItemName = ItemList.GetItemName(ItemNumber);
            string NewItemType = ItemList.GetItemType(ItemNumber);

            //Instantiate this new item pickup object and store it in the dictionary with the others
            GameItem NewItem = new GameItem(NewItemName, NewItemType, ItemNumber, ItemID, ItemLocation);
            ActiveItemDictionary.Add(ItemID, NewItem);

            //Instruct all the active clients to spawn this new item pickup into their game worlds
            //PacketManager.SendListSpawnItem(ConnectionManager.GetActiveClients(), NewItem);
        }

        //Removes an active item pickup from the game world
        public static void RemoveItemPickup(int ItemID)
        {
            ////Get this items information out of the dictionary
            //GameItem ItemPickup = ActiveItemDictionary[ItemID];

            ////Remove the item from the dictionary and from the game world
            //Physics.WorldSimulator.Space.Remove(ItemPickup.Collider);
            //Rendering.Window.Instance.ModelDrawer.Remove(ItemPickup.Collider);
            //ActiveItemDictionary.Remove(ItemID);

            ////Instruct all active clients to remove this item pickup from their game worlds
            //PacketManager.SendListRemoveItem(ConnectionManager.GetActiveClients(), ItemID);
        }
    }
}
