// ================================================================================================================================
// File:        MeshContent.cs
// Description: 
// ================================================================================================================================

using BepuUtilities;
using ContentLoader;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ContentLoader
{
    public struct TriangleContent
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
    }

    public class MeshContent : IContent
    {
        public TriangleContent[] Triangles;

        public ContentType ContentType { get { return ContentType.Mesh; } }

        public MeshContent(TriangleContent[] triangles)
        {
            Triangles = triangles;
        }
    }
}
