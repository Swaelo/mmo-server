// ================================================================================================================================
// File:        GameItem.cs
// Description: Stores information for an ingame item
// ================================================================================================================================

using BepuUtilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Server.Scenes;
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

        public Box ItemColliderShape;    //Physics collider variables
        public CollidableDescription ItemColliderDescription;


        public GameItem(string Name, string Type, int Number, int ID, Vector3 Location)
        {
            //Store all the details inside the class
            ItemName = Name;
            ItemType = Type;
            ItemNumber = Number;
            ItemID = ID;

            //Instantiate a new box collider into the server, start rendering it etc
            ItemColliderShape = new Box(.25f, .25f, .25f);
            ItemColliderDescription = new CollidableDescription(SceneHarness.CurrentScene.Simulation.Shapes.Add(ItemColliderShape), 0.25f);
            ItemColliderShape.ComputeInertia(1, out var Inertia);
            SceneHarness.CurrentScene.Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Location, Quaternion.Identity), Inertia, ItemColliderDescription, new BodyActivityDescription(0.01f)));
        }
    }
}
