// ================================================================================================================================
// File:        NavMeshNode.cs
// Description: Defines one of the triangle meshes which make up part of the navigation mesh, lists its neighbours and the vertices
//              which make up the 3 points of the triangle
// ================================================================================================================================

using System.Collections.Generic;
using BepuUtilities;
using System.Numerics;

namespace Server.Pathfinding
{
    public class NavMeshNode
    {
        public List<NavMeshNode> NeighbouringNodes = new List<NavMeshNode>();   //Keep a list of the other NavMeshNodes which are adjacent to this one
        public List<NavMeshVertex> NodeVertices = new List<NavMeshVertex>();    //Each node is basically 1 tri in the nav mesh model, list each vertex which defines that triangle
        public List<Vector3> VertexLocations = new List<Vector3>();

        //Pathfinding values used during A* pathway navigation
        public NavMeshNode ParentNode = null;   //This neighbouring node which should be travelled to next to reach the target location in the cheapest way possible
        public float GScore = float.MaxValue;   //GScore and FScore values are valued more highly when they have a lower value, setting them to max value by default ensures proper pathfinding
        public float FScore = float.MaxValue;

        //Resets all the pathfinding values to default, should be called on every node in the navmesh before calculation of a brand new pathway begins
        public void ResetPathfindingValues()
        {
            ParentNode = null;
            GScore = float.MaxValue;
            FScore = float.MaxValue;
        }

        //Find the average location of the 3 corner vertices which make up this nodes polygon in the navmesh model
        public Vector3 GetAverageVertexLocation()
        {
            return Vector3.Zero;
        }
    }
}
