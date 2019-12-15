// ================================================================================================================================
// File:        AxisMarkers.cs
// Description: Adds a bunch of markers into the scene showing the directions of the X and Z axes
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Server.World;

namespace Server.Testing
{
    public static class AxisMarkers
    {
        //Number Model Mesh Handles
        private static int ZeroMeshHandle;
        private static int OneMeshHandle;
        private static int TwoMeshHandle;
        private static int ThreeMeshHandle;
        private static int FourMeshHandle;
        private static int FiveMeshHandle;
        private static int SixMeshHandle;
        private static int SevenMeshHandle;
        private static int EightMeshHandle;
        private static int NineMeshHandle;

        public static void LoadModels()
        {
            ZeroMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Zero.obj", new Vector3(0.5f));
            OneMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\One.obj", new Vector3(0.5f));
            TwoMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Two.obj", new Vector3(0.5f));
            ThreeMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Three.obj", new Vector3(0.5f));
            FourMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Four.obj", new Vector3(0.5f));
            FiveMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Five.obj", new Vector3(0.5f));
            SixMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Six.obj", new Vector3(0.5f));
            SevenMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Seven.obj", new Vector3(0.5f));
            EightMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Eight.obj", new Vector3(0.5f));
            NineMeshHandle = MeshManager.LoadMesh(@"Models\Numbers\Nine.obj", new Vector3(0.5f));
        }

        public static void AddModels(Simulation Simulation)
        {
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(ZeroMeshHandle)), 0.1f)));   //Origin Zero Marker

            Simulation.Statics.Add(new StaticDescription(new Vector3(9.5f, 0.5f, -10f), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(OneMeshHandle)), 0.1f)));   //(10, 10) One Marker
            Simulation.Statics.Add(new StaticDescription(new Vector3(10.5f, 0.5f, -10f), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(ZeroMeshHandle)), 0.1f)));   //(10, 10) Zero Marker

            Simulation.Statics.Add(new StaticDescription(new Vector3(1, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(OneMeshHandle)), 0.1f)));        //X One
            Simulation.Statics.Add(new StaticDescription(new Vector3(2, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(TwoMeshHandle)), 0.1f)));        //X Two
            Simulation.Statics.Add(new StaticDescription(new Vector3(3, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(ThreeMeshHandle)), 0.1f)));      //X Three
            Simulation.Statics.Add(new StaticDescription(new Vector3(4, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(FourMeshHandle)), 0.1f)));       //X Four
            Simulation.Statics.Add(new StaticDescription(new Vector3(5, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(FiveMeshHandle)), 0.1f)));       //X Five
            Simulation.Statics.Add(new StaticDescription(new Vector3(6, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(SixMeshHandle)), 0.1f)));        //X Six
            Simulation.Statics.Add(new StaticDescription(new Vector3(7, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(SevenMeshHandle)), 0.1f)));      //X Seven
            Simulation.Statics.Add(new StaticDescription(new Vector3(8, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(EightMeshHandle)), 0.1f)));      //X Eight
            Simulation.Statics.Add(new StaticDescription(new Vector3(9, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(NineMeshHandle)), 0.1f)));       //X Nine
                                                                        
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -1), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(OneMeshHandle)), 0.1f)));        //Z One
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -2), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(TwoMeshHandle)), 0.1f)));        //Z Two
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -3), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(ThreeMeshHandle)), 0.1f)));      //Z Three
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -4), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(FourMeshHandle)), 0.1f)));       //Z Four
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -5), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(FiveMeshHandle)), 0.1f)));       //Z Five
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -6), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(SixMeshHandle)), 0.1f)));        //Z Six
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -7), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(SevenMeshHandle)), 0.1f)));      //Z Seven
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -8), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(EightMeshHandle)), 0.1f)));      //Z Eight
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, -9), new CollidableDescription(Simulation.Shapes.Add(MeshManager.GetMesh(NineMeshHandle)), 0.1f)));       //Z Nine
        }
    }
}
