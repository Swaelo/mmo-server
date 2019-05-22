// ================================================================================================================================
// File:        NavMeshVertex.cs
// Description: Defines a single vertex in a NavMesh, lists which other vertices in the NavMesh it is linked to, and stored values
//              used for pathfinding while computing a pathway with A* searches
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Numerics;
using BepuUtilities;
using BepuPhysics;

namespace Server.Pathfinding
{
    public class NavMeshVertex
    {
        public Vector3 VertexLocation = Vector3.Zero;   //This vertices world location
        public List<NavMeshVertex> NeighbouringVertices = new List<NavMeshVertex>();    //The rest of the vertices which included with this one make up a node in the navmesh

        //Default constructor
        public NavMeshVertex(Vector3 VertexLocation)
        {
            this.VertexLocation = VertexLocation;
        }

        //Pathfinding values used during A* pathway navigation
        public NavMeshVertex ParentVertex = null;   //This neighbouring vertex which should be travelled to next to reach the target location in the shortest path possible
        public float GScore = float.MaxValue;       //GScore and FScore values are valued more highly when they have a lower value, setting them to max value by default ensures proper pathfinding
        public float FScore = float.MaxValue;

        //Resets all the pathfinding values to default, should be called on every vertex before starting a new A* search
        public void ResetPathfindingValues()
        {
            ParentVertex = null;
            GScore = float.MaxValue;
            FScore = float.MaxValue;
        }

        //Assigns to vertices as neighbours to each other
        public void LinkVertices(NavMeshVertex OtherVertex)
        {
            if (!NeighbouringVertices.Contains(OtherVertex))
                NeighbouringVertices.Add(OtherVertex);
            if (!OtherVertex.NeighbouringVertices.Contains(this))
                OtherVertex.NeighbouringVertices.Add(this);
        }

        //Adds every vertex in the arry to our list of neighbours
        public void AddNeighbours(List<NavMeshVertex> NewNeighbours)
        {
            //Loop through each vertex which needs to be added to our list of neighbours
            foreach(NavMeshVertex NewNeighbour in NewNeighbours)
            {
                //Check that they arent already in the list before adding them
                if (!NeighbouringVertices.Contains(NewNeighbour))
                    NeighbouringVertices.Add(NewNeighbour);
            }
        }

        //Computes the heuristic cost to travel from this node to the given target vertex
        //(this is pretty much the distance between the 2 nodes when cast onto a 2d plane
        public float HeurtisticCost(NavMeshVertex TargetVertex)
        {
            Vector2 CurrentHeuristic = new Vector2(VertexLocation.X, VertexLocation.Z);
            Vector2 TargetHeuristic = new Vector2(TargetVertex.VertexLocation.X, TargetVertex.VertexLocation.Z);
            return Vector2.Distance(CurrentHeuristic, TargetHeuristic);
        }

        //Performs a line of sight check between the two vertices to see if there are any obstacles in between that may make a pathway invalid
        public bool LOSCheck(NavMeshVertex TargetVertex)
        {
            Vector3 RayDirection = VertexLocation - TargetVertex.VertexLocation;

            Console.WriteLine("Reimplement navmeshvertex line of sight raycasting");

            //Space.RayCast changed to Simulation.RayCast

            
            return false;
        }
    }
}
