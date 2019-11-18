// ================================================================================================================================
// File:        BinaryConverter.cs
// Description: Helper functions used by the ClientConnection class when reading in data sent from game clients
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;

namespace Server.Misc
{
    public static class BinaryConverter
    {
        /// <summary>
        /// Converts a binary string to its decimal integer value
        /// </summary>
        /// <param name="BinaryString">The string of 1's and 0's to be converted</param>
        /// <returns>Integer value which matches the binary number being represented by the given string</returns>
        public static int BinaryStringToDecimal(string BinaryString)
        {
            return Convert.ToInt32(BinaryString, 2);
        }

        public static UInt16 BinaryStringToUInt16(string BinaryString)
        {
            return Convert.ToUInt16(BinaryString, 2);
        }

        public static UInt64 BinaryStringToUInt64(string BinaryString)
        {
            return Convert.ToUInt64(BinaryString, 64);
        }

        /// <summary>
        /// Converts an array of bit values into a binary string
        /// </summary>
        /// <param name="ByteArray">The array of bytes that will be converted to binary</param>
        /// <returns></returns>
        public static string ByteArrayToBinaryString(byte[] ByteArray)
        {
            string BinaryString = "";
            foreach (byte Byte in ByteArray)
                BinaryString += ByteToBinaryString(Byte);
            return BinaryString;
        }

        /// <summary>
        /// Converts a single byte into a binary string
        /// </summary>
        /// <param name="Byte"></param>
        /// <returns></returns>
        public static string ByteToBinaryString(byte Byte)
        {
            return Convert.ToString(Byte, 2).PadLeft(8, '0');
        }
    }
}