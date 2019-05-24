// ================================================================================================================================
// File:        NavMesh.cs
// Description: Stores information for the entire nav mesh to be loaded into the server simulation, once the nav mesh is set up you
//              can use the various pathfinding functions to computes a pathway between two locations in the mesh
// ================================================================================================================================

using System.Numerics;
using System.Collections.Generic;
using Server.Interface;

namespace Server.Pathfinding
{
    public class NavMesh
    {
        //Send this to the game windows model drawer to render the navmesh
        //public StaticMesh MeshData;

        //Every nav mesh node which has been created from the games current navigation mesh object
        public List<NavMeshNode> MeshNodes = new List<NavMeshNode>();

        //Every node vertex in the navigation mesh
        public List<NavMeshVertex> MeshVertices = new List<NavMeshVertex>();
        public NavMeshVertex VertexForPosition(Vector3 VertexPosition)
        {//Returns an already existing NavMeshVertex for the given position if it can be found
            foreach (NavMeshVertex Vertex in MeshVertices)
            {
                if (Vertex.VertexLocation == VertexPosition)
                    return Vertex;
            }
            //Create a new one in the list and return that if one didnt already exist for this location
            NavMeshVertex NewVertex = new NavMeshVertex(VertexPosition);
            MeshVertices.Add(NewVertex);
            return NewVertex;
        }

        //Default Constructor
        public NavMesh(string FileName)
        {
            //Variables to temporarily store all the data loaded from file while the nav mesh is constructed
            Vector3[] Vertices; //The entire list of vertices which make up the entirety of the navigation mesh
            int[] Indices;  //The indices of the vertices of the navigation mesh
            //Model ModelData;    //Model object constructed from the navmesh resource file, vertices and indices are extracted from this

            ////Load the nav mesh from file
            //ModelData = Rendering.Window.Instance.Content.Load<Model>(FileName);

            ////Extract the meshes vertices and indices
            //Data.ModelDataExtractor.GetVerticesAndIndicesFromModel(ModelData, out Vertices, out Indices);
            ////Use these to create a new static mesh object
            //MeshData = new StaticMesh(Vertices, Indices, new AffineTransform(Vector3.Zero));

            ////Loop through each face which makes up the navigation mesh and create a new mesh node object for each of them
            //for (int i = 0; i < Indices.Length / 3; i++)
            //{
            //    //Grab the locations of the 3 vertices which make up this face of the navigation mesh
            //    Vector3[] MeshVertices = GetNextLocations(i * 3, Vertices, Indices);
            //    //Create a new nav mesh node object, store in it the vertices, and the center location of the nodes 3 vertices
            //    NavMeshNode NewNode = new NavMeshNode(MeshVertices);
            //    //As each mesh node is created, store it in the static NavMeshNodes class so they can easily be accessed by all classes
            //    MeshNodes.Add(NewNode);
            //}

            ////Now that all of the mesh node objects have been define, we need to figure out and assign which nodes are neighbours to each other
            //for (int i = 0; i < MeshNodes.Count; i++)
            //{//Loop through each of the mesh nodes we have created
            //    NavMeshNode CurrentNode = MeshNodes[i];
            //    for (int j = i + 1; j < MeshNodes.Count; j++)
            //    {//Check each node against all nodes that are after it
            //        NavMeshNode CompareNode = MeshNodes[j];
            //        //If the current node shares any of its vertices with the compare node, then they are neighbours to one another
            //        if (CurrentNode.VertexLocations.Contains(CompareNode.VertexLocations[0]) ||
            //            CurrentNode.VertexLocations.Contains(CompareNode.VertexLocations[1]) ||
            //            CurrentNode.VertexLocations.Contains(CompareNode.VertexLocations[2]))
            //        {//Assign the two nodes as neighbours to one another (if they arent already)
            //            if (!CurrentNode.Neighbours.Contains(CompareNode))
            //                CurrentNode.Neighbours.Add(CompareNode);
            //            if (!CompareNode.Neighbours.Contains(CurrentNode))
            //                CompareNode.Neighbours.Add(CurrentNode);
            //        }
            //    }
            //}

            //Now set up a list for every unique vertex location in the entire nav mesh, with each of them pointing to their neighbours
            foreach (NavMeshNode Node in MeshNodes)
            {
                //Get the NavMeshVertex for each of the 3 points of this node
                NavMeshVertex Vertex1 = VertexForPosition(Node.VertexLocations[0]);
                NavMeshVertex Vertex3 = VertexForPosition(Node.VertexLocations[2]);
                NavMeshVertex Vertex2 = VertexForPosition(Node.VertexLocations[1]);
                //Assign these all as neighbours to one another
                Vertex1.LinkVertices(Vertex2);
                Vertex1.LinkVertices(Vertex3);
                Vertex2.LinkVertices(Vertex3);
                //Store these vertices inside their parent mesh node
                Node.NodeVertices.Add(Vertex1);
                Node.NodeVertices.Add(Vertex2);
                Node.NodeVertices.Add(Vertex3);
            }
        }

        //Returns the locations of the 3 vertices which makes up the next triangle in the navigation mesh
        private static Vector3[] GetNextLocations(int CurrentIndex, Vector3[] Vertices, int[] Indices)
        {
            Vector3[] NextLocations = new Vector3[3];
            NextLocations[0] = Vertices[Indices[CurrentIndex]];
            NextLocations[1] = Vertices[Indices[CurrentIndex + 1]];
            NextLocations[2] = Vertices[Indices[CurrentIndex + 2]];
            return NextLocations;
        }

        //Checks the given point against every node in this nav mesh until it finds one which node the point is inside
        //Based on answers found in this question https://gamedev.stackexchange.com/questions/28781/easy-way-to-project-point-onto-triangle-or-plane/152476#152476
        public NavMeshNode FindNodeContainingPoint(Vector3 Point)
        {
            foreach (NavMeshNode Node in MeshNodes)
            {
                //Define the 3 corner points of the triangle
                Vector3 P1 = Node.VertexLocations[0];
                Vector3 P2 = Node.VertexLocations[1];
                Vector3 P3 = Node.VertexLocations[2];
                //Project the polnt onto the plane of the triangle
                Vector3 U = P2 - P1;
                Vector3 V = P3 - P1;
                Vector3 N = Vector3.Cross(U, V);
                Vector3 W = Point - P1;
                //Using the barycentric coordinates to find the point projected onto the plane
                //g = [(UxW).N]/N.N
                Vector3 UxW = Vector3.Cross(U, W);
                float g = Vector3.Dot(UxW, N) / Vector3.Dot(N, N);
                //b = [(WxV).N]/N.N
                Vector3 WxV = Vector3.Cross(W, V);
                float b = Vector3.Dot(WxV, N) / Vector3.Dot(N, N);
                float a = 1 - g - b;
                //Check if the projected point lies within the triangle
                bool ContainsPoint = ((0 <= a) && (a <= 1) &&
                    (0 <= b) && (b <= 1) &&
                    (0 <= g) && (g <= 1));
                //Return this mesh node if this is where the point was found to be inside
                if (ContainsPoint)
                    return Node;
            }

            Log.PrintDebugMessage("Pathfinding.NavMesh this point lies completely outside of the navmesh");
            return null;
        }
    }
}
