// ================================================================================================================================
// File:        ItemInfoDatabase.cs
// Description: Used to fetch the rest of an items information with just its ItemNumber
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.IO;
using System.Collections.Generic;
using Server.Interface;

namespace Server.GameItems
{
    public static class ItemInfoDatabase
    {
        private static Dictionary<int, ItemData> ItemInfoList = new Dictionary<int, ItemData>();

        /// <summary>
        /// Loads information for every item available in the game from the MasterItemList file that is exported from the unity editor
        /// </summary>
        /// <param name="MasterItemListFile">Filename / pathway to the MasterItemList file to be imported, this will automatically have the project directory prepended onto it.</param>
        public static void LoadItemList(string MasterItemListFile)
        {
            //Append the filename onto the current working directory to get the exact location of the file that we need to load
            string WorkingDirectory = Environment.CurrentDirectory;
            string FilePath = WorkingDirectory + "/" + MasterItemListFile;

            //Display what filepath is being used then load that file into memory
            Console.WriteLine("Loading item list from: " + FilePath);
            string[] FileLines = System.IO.File.ReadAllLines(FilePath);

            //Loop through all the lines, processing one at a time
            foreach (string Line in FileLines)
            {
                //Split each line with its : seperators
                string[] LineSplit = Line.Split(':');

                //Extract all the items data into a new ItemData object
                ItemData NewItem = new ItemData();
                NewItem.ItemName = LineSplit[0];
                NewItem.ItemDisplayName = LineSplit[1];
                NewItem.ItemType = GetItemType(LineSplit[2]);
                NewItem.ItemEquipmentSlot = GetItemSlot(LineSplit[3]);
                NewItem.ItemNumber = Int32.Parse(LineSplit[4]);

                //Store the new item definition into the dictionary with all the others
                ItemInfoList.Add(NewItem.ItemNumber, NewItem);
            }

            //Print a message to verify all items information was loaded from the file
            Log.Chat(ItemInfoList.Count + " items information was loaded after processing " + FileLines.Length + " lines of the ItemList file.");
        }

        /// <summary>
        /// Returns an ItemData object containing all of an items information
        /// </summary>
        /// <param name="ItemNumber">The game items identifier you want to fetch the information for</param>
        /// <returns></returns>
        public static ItemData GetItemInfo(int ItemNumber)
        {
            if (!ItemInfoList.ContainsKey(ItemNumber))
            {
                Log.Chat("ItemInfoDatabase.GetItemInfo No item exists in the database with number " + ItemNumber + ". Returning null");
                return null;
            }

            return ItemInfoList[ItemNumber];
        }

        /// <summary>
        /// Returns the items type with just the string value read out from the text file
        /// </summary>
        /// <param name="ItemTypeValue">string value read from text file</param>
        /// <returns></returns>
        private static ItemType GetItemType(string ItemTypeValue)
        {
            if (ItemTypeValue == "Consumable")
                return ItemType.Consumable;
            else if (ItemTypeValue == "Equipment")
                return ItemType.Equipment;
            else if (ItemTypeValue == "AbilityGem")
                return ItemType.AbilityGem;

            return ItemType.NULL;
        }

        /// <summary>
        /// Returns the items equipment slot with just the string value read out from the text file
        /// </summary>
        /// <param name="ItemSlotValue">string value read from text file</param>
        /// <returns></returns>
        private static EquipmentSlot GetItemSlot(string ItemSlotValue)
        {
            if (ItemSlotValue == "Head")
                return EquipmentSlot.Head;
            else if (ItemSlotValue == "Back")
                return EquipmentSlot.Back;
            else if (ItemSlotValue == "Neck")
                return EquipmentSlot.Neck;
            else if (ItemSlotValue == "LeftShoulder")
                return EquipmentSlot.LeftShoulder;
            else if (ItemSlotValue == "RightShoulder")
                return EquipmentSlot.RightShoulder;
            else if (ItemSlotValue == "Chest")
                return EquipmentSlot.Chest;
            else if (ItemSlotValue == "LeftGlove")
                return EquipmentSlot.LeftGlove;
            else if (ItemSlotValue == "RightGlove")
                return EquipmentSlot.RightGlove;
            else if (ItemSlotValue == "Legs")
                return EquipmentSlot.Legs;
            else if (ItemSlotValue == "LeftHand")
                return EquipmentSlot.LeftHand;
            else if (ItemSlotValue == "RightHand")
                return EquipmentSlot.RightHand;
            else if (ItemSlotValue == "LeftFoot")
                return EquipmentSlot.LeftFoot;
            else if (ItemSlotValue == "RightFoot")
                return EquipmentSlot.RightFoot;

            return EquipmentSlot.NULL;
        }

        /// <summary>
        /// Returns the EquipmentSlot that a given game item may be equipped to
        /// </summary>
        /// <param name="ItemNumber">Identifier to look up the items information from the database</param>
        /// <returns></returns>
        public static EquipmentSlot GetItemSlot(int ItemNumber)
        {
            //1-2 are potions
            if (ItemNumber == 1 || ItemNumber == 2)
                return EquipmentSlot.NULL;
            //3-4 are main hand weapons
            else if (ItemNumber == 3 || ItemNumber == 4)
                return EquipmentSlot.RightHand;
            //5 is a shield for off hand
            else if (ItemNumber == 5)
                return EquipmentSlot.LeftHand;
            //6 is helmets
            else if (ItemNumber == 6)
                return EquipmentSlot.Head;
            //7-8 is left/right shoulders
            else if (ItemNumber == 7)
                return EquipmentSlot.LeftShoulder;
            else if (ItemNumber == 8)
                return EquipmentSlot.RightShoulder;
            //9-10 is left/right gloves
            else if (ItemNumber == 9)
                return EquipmentSlot.LeftGlove;
            else if (ItemNumber == 10)
                return EquipmentSlot.RightGlove;
            //11 is amulets
            else if (ItemNumber == 11)
                return EquipmentSlot.Neck;
            //12 is cloaks
            else if (ItemNumber == 12)
                return EquipmentSlot.Back;
            //13-14 is chest pieces
            else if (ItemNumber == 13 || ItemNumber == 14)
                return EquipmentSlot.Chest;
            //15-16 is leggings
            else if (ItemNumber == 15 || ItemNumber == 16)
                return EquipmentSlot.Legs;
            //17-18 is left feet
            else if (ItemNumber == 17 || ItemNumber == 18)
                return EquipmentSlot.LeftFoot;
            //19-20 is right feet
            else if (ItemNumber == 19 || ItemNumber == 20)
                return EquipmentSlot.RightFoot;
            //The rest are ability gems
            else
                return EquipmentSlot.NULL;
        }
    }
}
