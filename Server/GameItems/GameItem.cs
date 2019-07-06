// ================================================================================================================================
// File:        GameItem.cs
// Description: Stores information for an ingame item
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.GameItems
{
    public class GameItem
    {
        public string ItemName; //The ingame name of this item
        public string ItemType; //The type of item category it belongs to
        public int ItemNumber;  //Identifier number used to look up its data in the item list
        public int ItemID;      //Unique network ID used to track items location across the network
        public Vector3 ItemPosition;    //The items current position in the game world

        public TypedIndex ItemShapeIndex;
        public Box ItemColliderShape;    //Physics collider variables
        public CollidableDescription ItemColliderDescription;
        public int ItemColliderHandle;

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="ItemNumber">The game items unique number identifer used to fetch all of its data from the ItemList</param>
        /// <param name="GameWorld">The current game world physics simulation that the item collider will be added into</param>
        /// <param name="ItemSpawnLocation">Location in the game world where the item will be instantiated</param>
        public GameItem(int ItemNumber, Simulation GameWorld, Vector3 ItemSpawnLocation)
        {
            //Store the new items number inside the class
            this.ItemNumber = ItemNumber;
            ItemPosition = ItemSpawnLocation;

            //Fetch all the items information from the information database and store anything important in the class here
            ItemData ItemData = ItemInfoDatabase.GetItemInfo(ItemNumber);

            //Instantiate the new item pickup into the game world
            ItemColliderShape = new Box(.25f, .25f, .25f);
            ItemShapeIndex = GameWorld.Shapes.Add(ItemColliderShape);
            ItemColliderDescription = new CollidableDescription(ItemShapeIndex, 0.25f);
            ItemColliderShape.ComputeInertia(1, out var Inertia);
            ItemColliderHandle = GameWorld.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(ItemSpawnLocation, Quaternion.Identity), Inertia, ItemColliderDescription, new BodyActivityDescription(0.01f)));
        }
    }
}
