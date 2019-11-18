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
        public string Account;  //Name of the account this character belongs to
        public Vector3 Position;    //Characters position in the world
        public Quaternion Rotation; //Character current rotation
        public float CameraZoom;    //How far this characters camera is zoomed out
        public float CameraXRotation;   //Character cameras current X Rotation value
        public float CameraYRotation;   //Character cameras current Y Rotation value
        public string Name; //Characters name
        public int Experience;  //Current EXP value
        public int ExperienceToLevel;   //Amount of EXP needed to reach the next level
        public int Level;   //Current level
        public bool IsMale; //Is the character male
    }
}
