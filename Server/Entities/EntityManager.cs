// ================================================================================================================================
// File:        EntityManager.cs
// Description: Keeps track of all entities currently active in the servers world simulation, used to keep them all up to date and
//              for sending all their updated information to any connected game clients to keep them updated on the entities states
// ================================================================================================================================

using System.Collections.Generic;
using Server.Networking;
using Server.Scenes;
using Server.Database;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Entities
{
    public static class EntityManager
    {
        //All entities currently active in the game world physics simulation
        public static List<BaseEntity> ActiveEntities = new List<BaseEntity>();

        //Adds a new entity into the list with all the others
        public static void AddEntity(BaseEntity NewEntity)
        {
            ActiveEntities.Add(NewEntity);
            NewEntity.ID = EntityIDGenerator.GetNextID();
        }

        //Removes an already existing entity from the game world simulation
        public static void RemoveEntity(BaseEntity OldEntity)
        {
            //remove them from the list of entities
            ActiveEntities.Remove(OldEntity);
            //remove them from the physics simulation too
            Program.World.WorldSimulation.Bodies.Remove(OldEntity.BodyID);
        }

        //Removes a whole list of entities from the game world
        public static void RemoveEntities(List<BaseEntity> OldEntities)
        {
            foreach (BaseEntity OldEntity in OldEntities)
                RemoveEntity(OldEntity);
        }

        //Returns from the list of active entities, which ones clients should be told about
        public static List<BaseEntity> GetInteractiveEntities()
        {
            //Create a new list to store all the interactive entities
            List<BaseEntity> InteractiveEntities = new List<BaseEntity>();

            //Add to the list all the entities which are not of the STATIC entity type
            foreach(BaseEntity Entity in ActiveEntities)
            {
                if (Entity.Type != "Static")
                    InteractiveEntities.Add(Entity);
            }

            //Return the final list of interactive entities
            return InteractiveEntities;
        }

        //Runs the update function on all the currently active entities
        public static void UpdateEntities(float DeltaTime)
        {
            //Pass DeltaTime value into the update function of all the currently active entities
            foreach (BaseEntity Entity in ActiveEntities)
                Entity.Update(DeltaTime);
        }

        //Instructs the inner AI of any enemies which are currently targetting the given clients game character to drop their current target and return to their default start position and AI behaviour state
        public static void DropTarget(ClientConnection PlayerTarget)
        {
            //Loop through all the active entities trying to find any that currently have this player targetted
            foreach(var Entity in ActiveEntities)
            {
                //Cast it to the enemy entity type
                EnemyEntity Enemy = (EnemyEntity)Entity;

                //Tell them to drop target if they have this player targetted
                if (Enemy.PlayerTarget == PlayerTarget)
                    Enemy.DropTarget();
            }
        }

        //Handles having a player disconnect from the game, has all enemies drop them as their target, and backs up the characters data to the database
        public static void HandleClientDisconnect(ClientConnection Client)
        {
            if (Client.BodyHandle != -1)
            {
                //Remove them from the physics scene
                Program.World.WorldSimulation.Bodies.Remove(Client.BodyHandle);
                Client.BodyHandle = -1;

                //Tell any enemies targetting this character to stop targetting them
                EntityManager.DropTarget(Client);

                //Backup the characters data
                CharactersDatabase.SaveCharacterLocation(Client.CharacterName, Maths.VectorTranslate.ConvertVector(Client.CharacterPosition));
            }
        }

        //Checks if the attack hit any enemies, damages them accordingly
        public static void HandlePlayerAttack(Vector3 AttackPosition, Vector3 AttackScale, Quaternion AttackRotation)
        {
            ////Create a box shape and transform to apply to it for the players attack collider
            //BoxShape AttackCollider = new BoxShape(AttackScale.X, AttackScale.Y, AttackScale.Z);
            //RigidTransform AttackTransform = new RigidTransform(AttackPosition);

            ////While applying the attack to all the enemies in the scene, keep a list of enemies that were killed by the attack so they can
            ////be removed from the game once the collection of enemies has been iterated through completely
            //List<BaseEntity> DeadEntities = new List<BaseEntity>();

            ////Test this attack against every enemy in the scene
            //foreach (BaseEntity Enemy in ActiveEntities)
            //{
            //    //Create a collider for each enemy
            //    BoxShape EnemyCollider = new BoxShape(1, 1, 1);
            //    RigidTransform EnemyTransform = new RigidTransform(Enemy.Entity.Position);

            //    //Check if the attack hit this enemy
            //    bool AttackHit = BoxBoxCollider.AreBoxesColliding(AttackCollider, EnemyCollider, ref AttackTransform, ref EnemyTransform);
            //    if (AttackHit)
            //    {
            //        //Apply damage to the enemy and update all players on its new status
            //        Enemy.HealthPoints -= 1;

            //        //If the enemy has run out of health they need to be removed from the game, and all clients need to be told to remove them
            //        if (Enemy.HealthPoints <= 0)
            //            //Add the enemy to the list of enemies which were killed by this attack so they can be dealt with once the attack has been applied to all enemies
            //            DeadEntities.Add(Enemy);
            //    }
            //}

            ////If any entities were killed by this players attack they need to be removed from the game world
            //if (DeadEntities.Count > 0)
            //{
            //    //Get a list of all the active players who will need to be told about the enemies that were just killed
            //    List<ClientConnection> ActivePlayers = ConnectionManager.GetActiveClients();
            //    //Tell all of these active players which entities where killed by the players attack
            //    PacketManager.SendListRemoveEntities(ActivePlayers, DeadEntities);
            //    //Now remove all these entities from the servers world simulation
            //    RemoveEntities(DeadEntities);
            //}
        }
    }
}
