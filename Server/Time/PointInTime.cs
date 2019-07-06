// ================================================================================================================================
// File:        PointInTime.cs
// Description: Defines a single point in time, has helper functions to check how long ago that point in time was
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;

namespace Server.Time
{
    public class PointInTime
    {
        private DateTime BirthTime;  //Exactly when this object was created

        //Default Constructor sets the member variables to track exactly when this object was created
        public PointInTime()
        {
            BirthTime = DateTime.Now;
        }

        //Returns the total number of seconds that have passed since this object was created
        public int AgeInSeconds()
        {
            //Get the current time
            DateTime CurrentTime = DateTime.Now;
            //Calculate how many seconds have passed since this objects birth and the current time value
            int Seconds = (int)(BirthTime - CurrentTime).TotalSeconds;
            //Return the number of seconds that have passed since the objects creation
            return -Seconds;
        }
    }
}