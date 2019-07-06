// ================================================================================================================================
// File:        AStarSearch.cs
// Description: Constructs a list of world locations which can be travelled across one at a time to traverse from the starting location
//              to the ending location. Based on the A* Pseudocode found in this wikipedia article https://en.wikipedia.org/wiki/A*_search_algorithm
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using System.Collections.Generic;
using Server.Interface;

namespace Server.Pathfinding
{
    public static class AStarSearch
    {
        //Constructs a pathway searching only through the nodes in the nav mesh
        public static List<Vector3> ConstructNodePathway(NavMesh NavMesh, Vector3 PathStart, Vector3 PathEnd)
        {
            //Project the pathways starting and ending locations onto the nav mesh plane to find which nodes they are contained within
            NavMeshNode StartNode = NavMesh.FindNodeContainingPoint(PathStart);
            NavMeshNode EndNode = NavMesh.FindNodeContainingPoint(PathEnd);

            //The set of nodes already evaluated
            List<NavMeshNode> ClosedSet = new List<NavMeshNode>();

            //The set of currently discovered nodes that are not evaluated yet.
            //Initially, only the start node is known.
            List<NavMeshNode> OpenSet = new List<NavMeshNode>();
            OpenSet.Add(StartNode);

            //Reset the pathfinding values of all nodes in the nav mesh
            foreach (NavMeshNode Node in NavMesh.MeshNodes)
                Node.ResetPathfindingValues();

            //Precalculate the values for the starting node, as the cost to travel to itself is zero
            StartNode.GScore = 0;
            StartNode.FScore = StartNode.HeuristicCost(EndNode);

            //Iterator over the open set until a pathway is found or all nodes have been evaluated resulting in no pathway
            while (OpenSet.Count > 0)
            {
                //Finding the new current node, member of OpenSet with the lowest FScore value
                NavMeshNode CurrentNode = OpenSet[0];
                for (int i = 1; i < OpenSet.Count; i++)
                    if (OpenSet[i].FScore < CurrentNode.FScore)
                        CurrentNode = OpenSet[i];

                //If the new current node is the end node, then the pathway is complete
                if (CurrentNode == EndNode)
                {
                    Log.Chat("Pathfinding.AStarSearch pathway found");
                    //Start from the end node and follow its parents back all the way to the start
                    List<Vector3> Pathway = new List<Vector3>();
                    NavMeshNode CurrentStep = EndNode;
                    while (CurrentStep != null)
                    {
                        Pathway.Add(CurrentStep.AverageVertexLocations());
                        CurrentStep = CurrentStep.Parent;
                    }
                    Pathway.Reverse();
                    return Pathway;
                }

                //Move the new current node over the closed set as we are now going to compute all possible pathways over it
                OpenSet.Remove(CurrentNode);
                ClosedSet.Add(CurrentNode);

                //Iterate over each neighbour of the current node to check if thats a cheaper way to travel to the target location
                foreach (NavMeshNode Neighbour in CurrentNode.Neighbours)
                {
                    //Ignore any neighbours in the closed set which have been completely evaulated
                    if (ClosedSet.Contains(Neighbour))
                        continue;

                    //Calculate the distance to travel here from the starting node
                    float GScore = CurrentNode.GScore + Vector3.Distance(CurrentNode.AverageVertexLocations(), Neighbour.AverageVertexLocations());

                    //Add newly discovered nodes into the open list so they can be evaluated later
                    if (!OpenSet.Contains(Neighbour))
                        OpenSet.Add(Neighbour);
                    //If not, ignore if its not a cheaper way to travel
                    else if (GScore >= Neighbour.GScore)
                        continue;

                    //A cheaper GScore means this neighbour is the cheapest way to travel, update things accordingly
                    Neighbour.Parent = CurrentNode;
                    Neighbour.GScore = GScore;
                    Neighbour.FScore = Neighbour.GScore + Neighbour.HeuristicCost(EndNode);
                }
            }

            Log.Chat("Pathfinding.AStarSearch no path found");
            return null;
        }

        //Constructs a pathway searching through the vertices in the nav mesh
        public static List<Vector3> ConstructVertexPathway(NavMesh NavMesh, Vector3 PathStart, Vector3 PathEnd)
        {
            //Project the pathways starting and ending locations onto the nav mesh plane to find which nodes they are contained within
            NavMeshNode StartNode = NavMesh.FindNodeContainingPoint(PathStart);
            NavMeshNode EndNode = NavMesh.FindNodeContainingPoint(PathEnd);
            //Define new NavMeshVertexs placed at the pathway start and end locations
            NavMeshVertex StartVertex = new NavMeshVertex(PathStart);
            NavMeshVertex EndVertex = new NavMeshVertex(PathEnd);
            //Link the starting vertex to the vertices in the start node
            StartVertex.AddNeighbours(StartNode.NodeVertices);

            //The set of vertices already evaluated
            List<NavMeshVertex> ClosedSet = new List<NavMeshVertex>();

            //The set of currently discovered vertices that are not evaluated yet.
            //Initially, only the start vertex is known.
            List<NavMeshVertex> OpenSet = new List<NavMeshVertex>();
            OpenSet.Add(StartVertex);

            //Reset the pathfinding values of all the vertices in the nav mesh
            foreach (NavMeshVertex Vertex in NavMesh.MeshVertices)
                Vertex.ResetPathfindingValues();

            //Precalculate the values for the starting node, as the cost to travel to itself is zero
            StartVertex.GScore = 0;
            StartVertex.FScore = StartVertex.HeuristicCost(EndVertex);

            //Iterate over the open set until a pathway is found or all nodes have been evaluated resulting in no pathway
            while (OpenSet.Count > 0)
            {
                //Finding the new current vertex, member of OpenSet with the lowest FScore value
                NavMeshVertex CurrentVertex = OpenSet[0];
                for (int i = 1; i < OpenSet.Count; i++)
                    if (OpenSet[i].FScore < CurrentVertex.FScore)
                        CurrentVertex = OpenSet[i];

                //If the new current vertex is one of the vertices making up the end node, then the pathway is complete
                if (EndNode.NodeVertices.Contains(CurrentVertex))
                {
                    Log.Chat("Pathfinding.AStarSearch pathway found");
                    //Start from the end vertex and follow its parents back all the way to the start
                    List<Vector3> Pathway = new List<Vector3>();
                    NavMeshVertex CurrentStep = CurrentVertex;
                    while (CurrentStep != StartVertex)
                    {
                        Pathway.Add(CurrentStep.VertexLocation);
                        CurrentStep = CurrentStep.Parent;
                    }
                    Pathway.Reverse();
                    return Pathway;
                }

                //Move the new current vertex over to the closed set as we are now going to compute all possible pathways over it
                OpenSet.Remove(CurrentVertex);
                ClosedSet.Add(CurrentVertex);

                //Iterate over each neighbour of the current vertex to check if thats a cheaper way to travel to the target location
                foreach (NavMeshVertex Neighbour in CurrentVertex.VertexNeighbours)
                {
                    //Ignore any neighbours in the closed set which have been completely evaulated
                    if (ClosedSet.Contains(Neighbour))
                        continue;

                    //Calculate the distance to travel here from the starting vertex
                    float GScore = CurrentVertex.GScore + Vector3.Distance(CurrentVertex.VertexLocation, Neighbour.VertexLocation);

                    //Add newly discovered vertices into the open list so they can be evaluated later
                    if (!OpenSet.Contains(Neighbour))
                        OpenSet.Add(Neighbour);
                    //If not, ignore if its not a cheaper way to travel
                    else if (GScore >= Neighbour.GScore)
                        continue;

                    //A cheaper GScore means this neighbour is the cheapest way to travel, update things accordingly
                    Neighbour.Parent = CurrentVertex;
                    Neighbour.GScore = GScore;
                    Neighbour.FScore = Neighbour.GScore + Neighbour.HeuristicCost(EndVertex);
                }
            }

            Log.Chat("Pathfinding.AStarSearch no pathway found");
            return null;
        }
    }
}
