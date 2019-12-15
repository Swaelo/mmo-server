// ================================================================================================================================
// File:        MeshManager.cs
// Description: Handles loading meshes from the content archive, and adding and removing them from the physics scene
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using Server.Logging;
using System.Numerics;
using System.Collections.Generic;
using ContentLoader;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.World
{
    public static class MeshManager
    {
        //Content Archive where all mesh files will be loaded from, and the BufferPool used to execute loading actions
        private static ContentArchive MeshArchive = null;
        private static BufferPool MemoryPool = null;
        //All meshes that are currently loaded into memory, index by their mesh ID number
        private static Dictionary<int, Mesh> LoadedMeshes = new Dictionary<int, Mesh>();
        private static List<string> LoadedMeshNames = new List<string>();
        private static int NextMeshKey = 0;

        //Sets up the manager, takes in the ContentArchive where all future mesh objects will be loaded from
        public static void Initialize(ContentArchive Archive, BufferPool Pool)
        {
            MeshArchive = Archive;
            MemoryPool = Pool;
        }

        //Loads a mesh into memory, then returns a key you can use to access that mesh from the dictionary
        public static int LoadMesh(string ContentName, Vector3 Scale)
        {
            //Make sure the manager has been initialized first
            if(MeshArchive == null || MemoryPool == null)
            {
                MessageLog.Print("ERROR: MeshManager has not been initialized yet, initialize it first before trying to load meshes into memory.");
                return -1;
            }

            //Make sure there isnt already a mesh loaded with this name
            if(LoadedMeshNames.Contains(ContentName))
            {
                MessageLog.Print("Theres already a mesh loaded by the name of " + ContentName + ".");
                return -1;
            }

            //Load the meshes content from the archive and build a new Mesh object with its information
            var MeshContent = MeshArchive.Load<MeshContent>(ContentName);
            MemoryPool.Take<Triangle>(MeshContent.Triangles.Length, out var Triangles);
            for (int i = 0; i < MeshContent.Triangles.Length; i++)
                Triangles[i] = new Triangle(MeshContent.Triangles[i].A, MeshContent.Triangles[i].B, MeshContent.Triangles[i].C);
            Mesh MeshObject = new Mesh(Triangles, Scale, MemoryPool);

            //Store the new mesh object into the dictionary with the others
            int MeshKey = ++NextMeshKey;
            LoadedMeshes.Add(MeshKey, MeshObject);
            return MeshKey;
        }

        //Returns an already loaded meshes information from the dictionary using the key provided
        public static Mesh GetMesh(int MeshKey)
        {
            //Make sure a mesh with the given key has been loaded
            if(!LoadedMeshes.ContainsKey(MeshKey))
            {
                MessageLog.Print("ERROR: There is no mesh loaded into the dictionary with the key value " + MeshKey);
                return new Mesh();
            }

            //Return the mesh object from the dictionary
            return LoadedMeshes[MeshKey];
        }

        //Adds a mesh into the physics scene as a static object
        public static int AddStatic(Simulation World, int MeshHandle, Vector3 Position)
        {
            //Using the handle to get the mesh from the dictionary, add it into the simulation as a static object
            return World.Statics.Add(new StaticDescription(Position, new CollidableDescription(World.Shapes.Add(GetMesh(MeshHandle)), 0.1f)));
        }

        //Adds a mesh into the physics scene as a static object, and applied a specific rotation to it
        public static int AddStatic(Simulation World, int MeshHandle, Vector3 Position, Quaternion Rotation)
        {
            return World.Statics.Add(new StaticDescription(Position, Rotation, new CollidableDescription(World.Shapes.Add(GetMesh(MeshHandle)), 0.1f)));
        }
    }
}