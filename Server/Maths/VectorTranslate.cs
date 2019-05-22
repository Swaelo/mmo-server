// ================================================================================================================================
// File:        VectorTranslate.cs
// Description: Axis Directions between Unity Engine and BEPU Physics are different, this converts values between the two as needed
// ================================================================================================================================

using System.Numerics;

namespace Server.Maths
{
    public static class VectorTranslate
    {
        public static Vector3 ConvertVector(Vector3 Vector)
        {
            return new Vector3(-Vector.X, Vector.Y, Vector.Z);
        }
    }
}
