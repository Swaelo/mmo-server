// ================================================================================================================================
// File:        VectorTranslate.cs
// Description: Axis Directions between Unity Engine and BEPU Physics are different, this converts values between the two as needed
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;

namespace Server.Misc
{
    public static class VectorTranslate
    {
        public static Vector3 ConvertVector(Vector3 Vector)
        {
            return new Vector3(-Vector.X, Vector.Y, Vector.Z);
        }
    }
}
