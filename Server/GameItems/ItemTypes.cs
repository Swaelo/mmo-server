// ================================================================================================================================
// File:        ItemTypes.cs
// Description: Defines the different types of items available to the players in the game
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Server.GameItems
{
    public enum ItemType
    {
        NULL = 0,
        Consumable = 1,
        Equipment = 2,
        AbilityGem = 3
    }
}
