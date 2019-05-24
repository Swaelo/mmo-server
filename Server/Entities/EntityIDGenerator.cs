// ================================================================================================================================
// File:        EntityIDGenerator.cs
// Description: Little helper class used to assign unique ID to new entities whenever they are added into the game world
// ================================================================================================================================

using System;
using System.Threading;

namespace Server.Entities
{
    public static class EntityIDGenerator
    {
        public static readonly string Encode = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        private static long PreviousID = DateTime.UtcNow.Ticks;
        public static string GetNextID() => GenerateEntityID(Interlocked.Increment(ref PreviousID));
        
        //I dont remember how exactly this works or where I found this from, sorry
        private static unsafe string GenerateEntityID(long ID)
        {
            char* CharBuffer = stackalloc char[13];
            CharBuffer[0] = Encode[(int)(ID >> 60) & 31];
            CharBuffer[1] = Encode[(int)(ID >> 55) & 31];
            CharBuffer[2] = Encode[(int)(ID >> 50) & 31];
            CharBuffer[3] = Encode[(int)(ID >> 45) & 31];
            CharBuffer[4] = Encode[(int)(ID >> 40) & 31];
            CharBuffer[5] = Encode[(int)(ID >> 35) & 31];
            CharBuffer[6] = Encode[(int)(ID >> 30) & 31];
            CharBuffer[7] = Encode[(int)(ID >> 25) & 31];
            CharBuffer[8] = Encode[(int)(ID >> 20) & 31];
            CharBuffer[9] = Encode[(int)(ID >> 15) & 31];
            CharBuffer[10] = Encode[(int)(ID >> 10) & 31];
            CharBuffer[11] = Encode[(int)(ID >> 5) & 31];
            CharBuffer[12] = Encode[(int)ID & 31];

            return new string(CharBuffer, 0, 13);
        }
    }
}
