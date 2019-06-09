// ================================================================================================================================
// File:        BitValueGetter.cs
// Description: Given a byte value, returns the value of one of its bits
// ================================================================================================================================

using System;
using System.Collections.Generic;

namespace Server.Maths
{
    public static class BitValueGetter
    {
        /// <summary>
        /// Returns the value of a single bit within the given byte
        /// </summary>
        /// <param name="Byte">The byte which contains the desired bit value</param>
        /// <param name="BitNumber">The index of the bit value to be retrieved</param>
        /// <returns></returns>
        public static int GetBitValue(byte Byte, int BitNumber)
        {
            bool BitValue = (Byte & (1 << BitNumber - 1)) != 0;
            return BitValue ? 1 : 0;
        }

        /// <summary>
        /// Returns an array of bit values that have been extracted from a given byte
        /// </summary>
        /// <param name="Byte">The byte which contains the desired bit values</param>
        /// <param name="FirstBitNumber">The index of the first bit value to be extracted</param>
        /// <param name="LastBitNumber">The index of the last bit value to be extracted</param>
        /// <returns></returns>
        public static int[] GetBitValues(byte Byte, int FirstBitNumber, int LastBitNumber)
        {
            //Figure out how many bit values need to be retrieved, then create an array to store them all
            int BitCount = Math.Abs(FirstBitNumber - LastBitNumber) + 1;
            int[] BitValues = new int[BitCount];

            //Grab each of the required bit values, storing each of them into the array
            for (int i = 0; i < BitCount; i++)
                BitValues[i] = GetBitValue(Byte, i);

            //Return the final array containing all the requested bit values
            return BitValues;
        }

        /// <summary>
        /// Returns an array of 8 integers, matching the 8 bit values in the given Byte
        /// </summary>
        /// <param name="Byte">The byte value that is to be converted to an int array</param>
        /// <returns></returns>
        public static int[] GetByteValues(byte Byte)
        {


            int[] BitValues = new int[8];
            for (int i = 0; i < 8; i++)
                BitValues[i] = GetBitValue(Byte, i);
            return BitValues;
        }
    }
}