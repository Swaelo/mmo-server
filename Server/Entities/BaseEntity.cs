// ================================================================================================================================
// File:        BaseEntity.cs
// Description: Base Entity type to implement from when defining more advanced entity types like EnemyEntity or a boss type entity
//              All entities once created, are stored in the EntityManager in this base class type to be kept in a single list
// ================================================================================================================================

using BepuUtilities;
using BepuPhysics;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Entities
{
    public abstract class BaseEntity
    {
        public string ID = "-1";
        public string Type = "NULL";
        public Vector3 Scale = Vector3.Zero;
        public Vector3 Location = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public int BodyID = -1;
        public abstract void Update(float DeltaTime);
        public int HealthPoints = 3;
    }
}

//body is added like this? Simulation.Bodies.Add returns a handle to uniquiely identify the body
//SceneHarness.CurrentScene.Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Location, Quaternion.Identity), Inertia, ItemColliderDescription, new BodyActivityDescription(0.01f)));
