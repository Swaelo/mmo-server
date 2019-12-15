// ================================================================================================================================
// File:        EnemyEntity.cs
// Description: Defines a single enemy currently active in the servers world simulation, controls all of their AI/Behaviour during play
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using Server.Networking;
using BepuPhysics;
using BepuPhysics.Collidables;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Entities
{
    public enum EnemyState
    {
        Idle,
        Seek,
        Attack,
        Flee
    }

    public class EnemyEntity : BaseEntity
    {
        public EnemyState AIState = EnemyState.Idle;    //Current AI behaviour state of the enemy
        private float AgroRange = 5f;   //How close players need to be before the enemy will start attacking them
        private float AgroMaxRange = 10f;   //Once combat has started, how far the target must go before the enemy will stop targetting them
        public ClientConnection PlayerTarget = null;    //Enemys current player combat target
        private List<Vector3> NavigationPathway;    //Pathway formulated from AI search used to follow to reach the entities current target
        private Vector3 DefaultLocation;    //Spawn location of the enemy, where it returns to after ending combat
        private float SeekSpeed = 3;    //How fast the enemy moves while chasing its target in combat
        private float FleeSpeed = 5;    //How fast the enemy moves while retreating back to its default location after combat has ended
        //Physics simulation variables
        public Cylinder EntityColliderShape;
        public CollidableDescription EntityColliderDescription;

        //Default constructor
        public EnemyEntity(Vector3 SpawnLocation)
        {
            //Store the entities default location then add a new physics collider into the world physics simulation
            DefaultLocation = SpawnLocation;
            EntityColliderShape = new Cylinder(0.6f, 1.7f);
            EntityColliderDescription = new CollidableDescription(Program.World.World.Shapes.Add(EntityColliderShape), 0.25f);
            EntityColliderShape.ComputeInertia(1, out var Inertia);
            Program.World.World.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(SpawnLocation, Quaternion.Identity), Inertia, EntityColliderDescription, new BodyActivityDescription(0.01f)));

            //Pass it to the entity manager to be stored with all the others and set the enemies type value
            EntityManager.AddEntity(this);
            this.Type = "Skeleton Warrior";
        }

        //Custom entity update function
        public override void Update(float DeltaTime)
        {
            //Custom behaviour executed based on the entities current AI behaviour state
            switch (AIState)
            {
                case (EnemyState.Idle):
                    IdleState(DeltaTime);
                    break;

                case (EnemyState.Seek):
                    SeekState(DeltaTime);
                    break;

                case (EnemyState.Attack):
                    AttackState(DeltaTime);
                    break;

                case (EnemyState.Flee):
                    FleeState(DeltaTime);
                    break;

                default:
                    break;
            }
        }

        //Functions to process enemy AI logic during each seperate AI state available to the enemy
        private void IdleState(float DeltaTime)
        {
            //Look at all the active players in the game world, find the one closest to the enemy and start combat with them if they are within our agro range
            //List<ClientConnection> ActivePlayers = ConnectionManager.GetAc
        }
        private void SeekState(float DeltaTime)
        {

        }
        private void AttackState(float DeltaTime)
        {

        }
        private void FleeState(float DeltaTime)
        {

        }

        //Instructs the enemy to immediately end combat with its currently target, and return to its idle state once it returns to its default location
        public void DropTarget()
        {
            //AIState = EnemyState.Flee;
            //PlayerTarget = null;
        }
    }
}
