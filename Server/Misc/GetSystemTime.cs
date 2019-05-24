// ================================================================================================================================
// File:        GetSystemTime.cs
// Description: Returns the current time in a nicely formatted string
// ================================================================================================================================

using System;

namespace Server.Misc
{
    public static class GetSystemTime
    {
        public static string GetSystemTimeString()
        {
            DateTime Time = System.DateTime.Now;
            bool PM = Time.Hour > 12;
            int Hour = PM ? Time.Hour - 12 : Time.Hour;
            Hour = Hour == 0 ? 12 : Hour;
            string Minute = Time.Minute < 10 ? "0" + Time.Minute : Time.Minute.ToString();
            string Second = Time.Second < 10 ? "0" + Time.Second : Time.Second.ToString();
            return Hour + ":" + Minute + ":" + Second + " " + (PM ? "PM" : "AM");
        }
    }
}
