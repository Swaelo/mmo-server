// ================================================================================================================================
// File:        Trig.cs
// Description: Axis Directions between Unity Engine and BEPU Physics are different, this converts values between the two as needed
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;

namespace Server.Misc
{
    public static class Trig
    {
        public static float DegreesToRadians(float AngleInDegrees)
        {
            return (float)Math.PI * AngleInDegrees / 180;
        }

        public static float RadiansToDegrees(float AngleInRadians)
        {
            return (float)(AngleInRadians * (180 / Math.PI));
        }
    }
}