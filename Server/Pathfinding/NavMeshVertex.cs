// ================================================================================================================================
// File:        NavMeshVertex.cs
// Description: Defines a single vertex in a NavMesh, lists which other vertices in the NavMesh it is linked to, and stored values
//              used for pathfinding while computing a pathway with A* searches
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Pathfinding
{
    public class NavMeshVertex
    {
        public Vector3 VertexLocation = Vector3.Zero;   //This vertices world location

        //Neighbouring Vertices
        public List<NavMeshVertex> VertexNeighbours = new List<NavMeshVertex>();    //The adjacent verices connected to
        public void LinkVertices(NavMeshVertex OtherVertex)
        {//Assign the two vertices as neighbours to one another
            if (!VertexNeighbours.Contains(OtherVertex))
                VertexNeighbours.Add(OtherVertex);
            if (!OtherVertex.VertexNeighbours.Contains(this))
                OtherVertex.VertexNeighbours.Add(this);
        }

        //Pathfinding values
        public NavMeshVertex Parent = null; //Which node to travel to next to traverse along the computed pathway
        public float GScore = float.MaxValue;    //Cost to travel from the starting vertex to this vertex
        public float FScore = float.MaxValue;    //Cost to travel from this vertex to the ending vertex
        public void ResetPathfindingValues()
        {
            Parent = null;
            GScore = float.MaxValue;
            FScore = float.MaxValue;
        }

        //Default Constructor
        public NavMeshVertex(Vector3 VertexLocation)
        {
            this.VertexLocation = VertexLocation;
        }

        //Adds all the vertices in the given array to our list of neighbour vertices
        public void AddNeighbours(List<NavMeshVertex> NewNeighbours)
        {
            //Loop through all of the new vertices which need to be added to our list of neighbours
            foreach (NavMeshVertex NewNeighbour in NewNeighbours)
            {
                //Only add the neighbours which arent already in our neighbours
                if (!VertexNeighbours.Contains(NewNeighbour))
                    VertexNeighbours.Add(NewNeighbour);
            }
        }

        //Computes the heuristic cost value between this node and the given node
        //This is simply the vector2 distance between the two points if the navmesh were flatten onto a 2d plane
        public float HeuristicCost(NavMeshVertex Goal)
        {
            Vector2 CurrentHeuristic = new Vector2(VertexLocation.X, VertexLocation.Z);
            Vector2 GoalHeuristic = new Vector2(Goal.VertexLocation.X, Goal.VertexLocation.Z);
            return Vector2.Distance(CurrentHeuristic, GoalHeuristic);
        }

        //Performs a line of sight check between two nodes, the function will return true if there is a straight line between these two
        //vertices with no obstacles in the way and having no point of the line step outside of the nav mesh
        public bool LineofSight(NavMeshVertex Target)
        {
            //First check if theres anything between the two nodes that would block walking in a straight line
            Vector3 RayDirection = VertexLocation - Target.VertexLocation;
            //Ray Ray = new Ray(VertexLocation, RayDirection);
            //RayCastResult Result;
            //Physics.WorldSimulator.Space.RayCast(Ray, out Result);

            return false;
        }
    }
}
