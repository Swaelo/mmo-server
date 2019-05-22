// ================================================================================================================================
// File:        EquipmentSlots.cs
// Description: Defines all the different equipment slots available to the player
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Server.GameItems
{
    public enum EquipmentSlot
    {
        NULL = 0,
        Head = 1,   //Helmets
        Back = 2,   //Cloaks/Backpacks
        Neck = 3,   //Necklaces/Amulets
        LeftShoulder = 4,   //Pauldrons
        RightShoulder = 5,  //Pauldrons
        Chest = 6,  //Shirts/Chest Armour
        LeftGlove = 7,  //Gloves/Gauntlets
        RightGlove = 8, //Gloves/Gauntlets
        Legs = 9,   //Pants/Leg Armour
        LeftHand = 10,   //Primary Weapons
        RightHand = 11,  //Shields / Secondary Weapons
        LeftFoot = 12,   //Shoes/Boots
        RightFoot = 13  //Shoes/Boots
    }
}
