﻿// ================================================================================================================================
// File:        ValidInputCheckers.cs
// Description: Contains some helper functions for checking if username / accountnames provided by clients are valid for use
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Misc
{
    public class ValidInputCheckers
    {
        //Checks if the string is the name of a valid variable type that can be used to set values in the database
        public static bool IsValidVariableType(string VariableType)
        {
            if (VariableType == "string")
                return true;
            if (VariableType == "integer")
                return true;
            if (VariableType == "float")
                return true;
            return false;
        }

        //Checks if the variable name matches one that exists in the accounts database tables
        public static bool IsAccountVariableNameValid(string VariableName)
        {
            if (VariableName == "Username")
                return true;
            if (VariableName == "Password")
                return true;
            if (VariableName == "CharacterCount")
                return true;
            if (VariableName == "FirstCharacterName")
                return true;
            if (VariableName == "SecondCharacterName")
                return true;
            if (VariableName == "ThirdCharacterName")
                return true;
            return false;
        }

        //Checks if the variable name matches on that exists in the characters database tables
        public static bool IsCharacterVariableNameValid(string VariableName)
        {
            if (VariableName == "Account")
                return true;
            if (VariableName == "Name")
                return true;
            if (VariableName == "Position")
                return true;
            if (VariableName == "Movement")
                return true;
            if (VariableName == "Rotation")
                return true;
            if (VariableName == "CameraZoom")
                return true;
            if (VariableName == "CameraXRotation")
                return true;
            if (VariableName == "CameraYRotation")
                return true;
            if (VariableName == "CurrentHealth")
                return true;
            if (VariableName == "MaxHealth")
                return true;
            if (VariableName == "Experience")
                return true;
            if (VariableName == "ExperienceToLevel")
                return true;
            if (VariableName == "Level")
                return true;
            if (VariableName == "IsMale")
                return true;
            return false;
        }

        //Checks if a given username or password contains any banned characters
        public static bool IsValidUsername(string Username)
        {
            for (int i = 0; i < Username.Length; i++)
            {
                //letters and numbers are allowed
                if (Char.IsLetter(Username[i]) || Char.IsNumber(Username[i]))
                    continue;
                //Dashes, Periods and Underscores are allowed
                if (Username[i] == '-' || Username[i] == '.' || Username[i] == '_')
                    continue;

                //Absolutely anything else is banned
                return false;
            }
            return true;
        }

        //Given an invalid username/password it returns the reason why it is invalid for use
        public static string InvalidUsernameReason(string Username)
        {
            //Loop through all the characters in the username
            for (int i = 0; i < Username.Length; i++)
            {
                //letters and numbers are fine
                if (Char.IsLetter(Username[i]) || Char.IsNumber(Username[i]))
                    continue;
                //Dashes, Periods and Underscores are allowed
                if (Username[i] == '-' || Username[i] == '.' || Username[i] == '_')
                    continue;

                //Absolutely anything else is banned
                return ("contains '" + Username[i] + "'");
            }
            return "";
        }

        //Checks if a new character name is valid (character vs account names have different rules)
        public static bool IsValidCharacterName(string CharacterName)
        {
            //Empty names are not allowed
            if (CharacterName == "")
                return false;

            //Spaces are not allowed in character names
            if (CharacterName.Contains(' '))
                return false;

            //Now just run the name through the username checker to detect any other illegal characters
            return IsValidUsername(CharacterName);
        }

        //Given an invalid character name, it returns the reason why it is invalid for use
        public static string GetInvalidCharacterNameReason(string CharacterName)
        {
            //Check for empty name
            if (CharacterName == "")
                return "Empty names are not allowed";

            //Check for name with spaces
            if (CharacterName.Contains(' '))
                return "Spaces are not allowed in character names";

            //Check each character in the name individually
            for (int i = 0; i < CharacterName.Length; i++)
            {
                //letters and numbers are fine
                if (Char.IsLetter(CharacterName[i]) || Char.IsNumber(CharacterName[i]))
                    continue;
                //dashes, periods and underscores are allowed
                if (CharacterName[i] == '-' || CharacterName[i] == '.' || CharacterName[i] == '_')
                    continue;

                //Absolutely any other characters are banned from being used in character names
                return ("Cannot use '" + CharacterName[i] + "'s in character names");
            }

            return "";
        }
    }
}
