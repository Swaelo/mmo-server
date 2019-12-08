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
        public string Account = "";  //Name of the account this character belongs to
        public string Name = ""; //Characters name
        public bool NewPosition = false;
        public Vector3 Position = Vector3.Zero;    //Characters position in the world
        public Vector3 Movement = Vector3.Zero;    //Characters current input movement vector
        public Quaternion Rotation = Quaternion.Identity; //Character current rotation
        public float CameraZoom = 0f;    //How far this characters camera is zoomed out
        public float CameraXRotation = 0f;   //Character cameras current X Rotation value
        public float CameraYRotation = 0f;   //Character cameras current Y Rotation value
        public int CurrentHealth = 1;   //Current number of Health Points
        public int MaxHealth = 1;   //Current maximum number of Health Points
        public int Experience = 0;  //Current EXP value
        public int ExperienceToLevel = 100;   //Amount of EXP needed to reach the next level
        public int Level = 1;   //Current level
        public bool IsMale = true; //Is the character male
        public bool IsAlive = true; //If the character currently alive

        //Moves character to spawn with default camera settings
        public void SetDefaultValues()
        {
            CurrentHealth = MaxHealth;
            Position = new Vector3(15.068f, 0.079f, 22.025f);
            Rotation = new Quaternion(0f, 0.125f, 0f, -0.992f);
            CameraZoom = 7f;
            CameraXRotation = -14.28f;
            CameraYRotation = 5.449f;
        }
    }
}