// ================================================================================================================================
// File:        CharacterData.cs
// Description: Stores all the current information regarding a clients active player character currently active in the game world
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Data
{
    public class CharacterData
    {
        public string Account;
        public Vector3 Position;
        public Quaternion Rotation;
        public string Name;
        public int Experience;
        public int ExperienceToLevel;
        public int Level;
        public bool IsMale;
    }
}
