// ================================================================================================================================
// File:        CharacterData.cs
// Description: Stores all the current information regarding a clients active player character currently active in the game world
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Logging;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Server.Data
{
    public class CharacterData
    {
        //Account / Character Details
        public string Account = "";  //Name of the account this character belongs to
        public string Name = ""; //Characters name
        public Vector3 Position = Vector3.Zero;    //Characters position in the world
        public bool NewPosition = false;
        public Quaternion Rotation = Quaternion.Identity; //Character current rotation
        public bool NewRotation = false;
        public int CurrentHealth = 1;   //Current number of Health Points
        public int MaxHealth = 1;   //Current maximum number of Health Points
        public int Experience = 0;  //Current EXP value
        public int ExperienceToLevel = 100;   //Amount of EXP needed to reach the next level
        public int Level = 1;   //Current level
        public bool IsMale = true; //Is the character male
        public bool IsAlive = true; //If the character currently alive
        public bool WaitingToRespawn = false;
        public bool InGame = false;

        //Physics Settings
        public bool WaitingToEnter = false; //Set when client ready to enter the game world
        public bool BodyActive = false; //Tracks if this character has a body in the physics simulation
        public Capsule BodyShape;
        public TypedIndex BodyIndex;
        public RigidPose BodyPose;
        public BodyActivityDescription ActivityDescription;
        public CollidableDescription CollidableDescription;
        public BodyDescription BodyDescription;
        public int BodyHandle;

        //Attack Details
        public bool AttackPerformed = false;
        public Vector3 AttackPosition = Vector3.Zero;

        //Camera Settings
        public float CameraZoom = 0f;    //How far this characters camera is zoomed out
        public float CameraXRotation = 0f;   //Character cameras current X Rotation value
        public float CameraYRotation = 0f;   //Character cameras current Y Rotation value

        //Moves character to spawn with default camera settings
        public void SetDefaultValues()
        {
            CurrentHealth = MaxHealth;
            Position = new Vector3(15.068f, 0.079f, 22.025f);
            Rotation = new Quaternion(0f, 0.125f, 0f, -0.992f);
            CameraZoom = 7f;
            CameraXRotation = -14.28f;
            CameraYRotation = 5.449f;
        }

        //Initialize the characters body in the physics simulation
        public void InitializeBody(Simulation World, Vector3 Location)
        {
            if (BodyActive)
                return;
            BodyActive = true;
            BodyShape = new Capsule(0.5f, 2);
            BodyIndex = World.Shapes.Add(BodyShape);
            CollidableDescription = new CollidableDescription(BodyIndex, 0.1f);
            BodyShape.ComputeInertia(1, out var Inertia);
            Vector3 SpawnLocation = new Vector3(Location.X, Location.Y + 1.5f, Location.Z);
            BodyPose = new RigidPose(SpawnLocation, Quaternion.Identity);
            ActivityDescription = new BodyActivityDescription(0.01f);
            BodyDescription = BodyDescription.CreateKinematic(BodyPose, CollidableDescription, ActivityDescription);
            BodyHandle = World.Bodies.Add(BodyDescription);
        }

        //Update the body with a new location
        public void UpdateBody(Simulation World)
        {
            Vector3 UpdatePosition = new Vector3(Position.X, Position.Y + 1.5f, Position.Z);
            BodyPose = new RigidPose(UpdatePosition, Rotation);
            BodyShape.ComputeInertia(1, out var Inertia);
            BodyDescription = BodyDescription.CreateKinematic(BodyPose, CollidableDescription, ActivityDescription);
            World.Bodies.ApplyDescription(BodyHandle, ref BodyDescription);
            NewPosition = false;
        }

        public void RemoveBody(Simulation World)
        {
            if (!BodyActive)
                return;
            BodyActive = false;
            World.Bodies.Remove(BodyHandle);
            World.Shapes.Remove(BodyIndex);
        }
    }
}