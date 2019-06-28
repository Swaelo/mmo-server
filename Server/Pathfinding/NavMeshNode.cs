// ================================================================================================================================
// File:        NavMeshNode.cs
// Description: Defines one of the triangle meshes which make up part of the navigation mesh, lists its neighbours and the vertices
//              which make up the 3 points of the triangle
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;

namespace Server.Pathfinding
{
    public class NavMeshNode
    {
        public List<NavMeshNode> Neighbours = new List<NavMeshNode>();  //The list of nodes which are neighbours to this mesh node
        public List<NavMeshVertex> NodeVertices = new List<NavMeshVertex>();    //The 3 vertex locations for each corner of this node
        public List<Vector3> VertexLocations = new List<Vector3>();

        //A* pathfinding values
        public NavMeshNode Parent = null;
        public float GScore = float.MaxValue;
        public float FScore = float.MaxValue;
        public void ResetPathfindingValues()
        {
            Parent = null;
            GScore = float.MaxValue;
            FScore = float.MaxValue;
        }

        //returns the average location of the 3 vertex locations of this mesh node
        public Vector3 AverageVertexLocations()
        {
            float X = (NodeVertices[0].VertexLocation.X + NodeVertices[1].VertexLocation.X + NodeVertices[2].VertexLocation.X) / 3f;
            float Y = (NodeVertices[0].VertexLocation.Y + NodeVertices[1].VertexLocation.Y + NodeVertices[2].VertexLocation.Y) / 3f;
            float Z = (NodeVertices[0].VertexLocation.Z + NodeVertices[1].VertexLocation.Z + NodeVertices[2].VertexLocation.Z) / 3f;
            return new Vector3(X, Y, Z);
        }

        public NavMeshNode(Vector3[] VertexPositions)
        {
            for (int i = 0; i < 3; i++)
                VertexLocations.Add(VertexPositions[i]);
        }

        //returns the NodeVertex which is closest to the given location
        public NavMeshVertex GetVertexClosestTo(Vector3 Location)
        {
            NavMeshVertex ClosestVertex = NodeVertices[0];
            float ClosestVertexDistance = Vector3.Distance(Location, ClosestVertex.VertexLocation);
            for (int i = 1; i < NodeVertices.Count; i++)
            {
                float CompareVertexDistance = Vector3.Distance(Location, NodeVertices[i].VertexLocation);
                if (CompareVertexDistance < ClosestVertexDistance)
                {
                    ClosestVertex = NodeVertices[i];
                    ClosestVertexDistance = CompareVertexDistance;
                }
            }
            return ClosestVertex;
        }

        //Computes the heuristic cost value between this node and the given node
        //This is simply the vector2 distance between the two points if the navmesh were flatten onto a 2d plane
        public float HeuristicCost(NavMeshNode Goal)
        {
            Vector3 NodeLocation = AverageVertexLocations();
            Vector3 GoalLocation = Goal.AverageVertexLocations();

            Vector2 CurrentHeuristic = new Vector2(NodeLocation.X, NodeLocation.Z);
            Vector2 GoalHeuristic = new Vector2(GoalLocation.X, GoalLocation.Z);
            return Vector2.Distance(CurrentHeuristic, GoalHeuristic);
        }
    }
}
