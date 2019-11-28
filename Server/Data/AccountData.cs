// ================================================================================================================================
// File:        AccountData.cs
// Description: Stores all the current information regarding a users account
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using Server.Data;
using Server.Database;
using Server.Logging;

namespace Server.Data
{
    public class AccountData
    {
        public string Username = "";    //Name of the account
        public string Password = "";    //Password to log into the account
        public int CharacterCount = 0;  //How many characters exist on this account
        public string FirstCharacterName = "";  //Name of the first character
        public string SecondCharacterName = ""; //Name of the second character
        public string ThirdCharacterName = "";  //Name of the third character

        //Returns a new CharacterData object containing all of that characters data
        public CharacterData GetCharactersData(int CharacterNumber)
        {
            //Make sure the character who's data is being requested exists
            if(CharacterNumber == 1 && FirstCharacterName == "")
            {
                MessageLog.Print("ERROR: This account doesnt have a 1st character, so its data cannot be provded.");
                return null;
            }
            if(CharacterNumber == 2 && SecondCharacterName == "")
            {
                MessageLog.Print("ERROR: This account doesnt have a 2nd character, so its data cannot be provided.");
                return null;
            }
            if(CharacterNumber == 3 && ThirdCharacterName == "")
            {
                MessageLog.Print("ERROR: This account doesnt have a 3rd character, so its data cannot be provided.");
                return null;
            }

            //Fetch the characters data and return it
            return CharactersDatabase.GetCharacterData(
                CharacterNumber == 1 ? FirstCharacterName :
                CharacterNumber == 2 ? SecondCharacterName :
                ThirdCharacterName);
        }
    }
}