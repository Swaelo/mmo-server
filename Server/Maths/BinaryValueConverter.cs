// ================================================================================================================================
// File:        BinaryValueConverter.cs
// Description: Helper functions to convert binary values to decimal and vice versa
// ================================================================================================================================

using System;
using System.Text;
using System.Collections.Generic;

namespace Server.Maths
{
    public static class BinaryValueConverter
    {
        /// <summary>
        /// Given an array of integers which each represents a single bit value, returns the equivalent integer value
        /// </summary>
        /// <param name="Bits">Array of bit values, this should be in binary format</param>
        /// <returns></returns>
        public static int BinaryToDecimal(int[] Bits)
        {
            //Get the bit values as a single string
            string BitString = BitsToBinaryString(Bits);

            //Use the string to find the final decimal value to be returned
            return Convert.ToInt32(BitString, 2);
        }

        /// <summary>
        /// Combines the two bytes together and converts them into decimal value
        /// </summary>
        /// <param name="FirstByte">The first byte value</param>
        /// <param name="SecondByte">The second byte value</param>
        /// <returns></returns>
        public static int BinaryToDecimal(int[] FirstByte, int[] SecondByte)
        {
            //Convert each byte into its own string
            string FirstByteString = BitsToBinaryString(FirstByte);
            string SecondByteString = BitsToBinaryString(SecondByte);
            //Combine the two strings, converting that to decimal as the final value
            return Convert.ToInt32(FirstByteString + SecondByteString, 2);
        }

        public static int BinaryToDecimal(int[] FirstByte, int[] SecondByte, int[] ThirdByte, int[] FourthByte, int[] FifthByte, int[] SixthByte, int[] SeventhByte, int[] EighthByte)
        {
            //Convert each byte into its own string
            string FirstByteString = BitsToBinaryString(FirstByte);
            string SecondByteString = BitsToBinaryString(SecondByte);
            string ThirdByteString = BitsToBinaryString(ThirdByte);
            string FourthByteString = BitsToBinaryString(FourthByte);
            string FifthByteString = BitsToBinaryString(FifthByte);
            string SixthByteString = BitsToBinaryString(SixthByte);
            string SeventhByteString = BitsToBinaryString(SeventhByte);
            string EighthByteString = BitsToBinaryString(EighthByte);
            //Combine the 4 strings, converting that to decimal as the final value
            return Convert.ToInt32(FirstByteString + SecondByteString + ThirdByteString + FourthByteString + FifthByteString + SixthByteString + SeventhByteString + EighthByteString, 2);
        }

        /// <summary>
        /// Takes a byte value and converts it to its corresponding ASCII character value
        /// </summary>
        /// <param name="ByteValue">Array on ints, each representing a single bit in the byte to be converted</param>
        /// <returns></returns>
        public static string BinaryToString(int[] ByteValue)
        {
            string BitString = BitsToBinaryString(ByteValue);
            byte[] Bytes = BytesFromBinaryString(BitString);
            return Encoding.ASCII.GetString(Bytes);
        }

        /// <summary>
        /// Takes a binary string value and converts it to a hex string value
        /// </summary>
        /// <param name="BinaryString">The binary string to be converted into hex</param>
        /// <returns></returns>
        public static string BinaryStringToHexString(string BinaryString)
        {
            StringBuilder Result = new StringBuilder(BinaryString.Length / 8 + 1);

            int Mod4Len = BinaryString.Length % 8;
            if (Mod4Len != 0)
                BinaryString = BinaryString.PadLeft(((BinaryString.Length / 1) + 1) * 8, '0');

            for(int i = 0; i < BinaryString.Length; i += 8)
            {
                string EightBits = BinaryString.Substring(i, 8);
                Result.AppendFormat("{0:X2}", Convert.ToByte(EightBits, 2));
            }

            return Result.ToString();
        }

        public static int BinaryStringToDecimal(string BinaryString)
        {
            return Convert.ToInt32(BinaryString, 2);
        }

        public static byte[] BytesFromBinaryString(string BinaryString)
        {
            var List = new List<byte>();
            for(int i = 0; i < BinaryString.Length; i += 8)
            {
                string T = BinaryString.Substring(i, 8);
                List.Add(Convert.ToByte(T, 2));
            }
            return List.ToArray();
        }

        /// <summary>
        /// Takes an array of bit values and converts them to a binary string
        /// </summary>
        /// <param name="Bits"></param>
        /// <returns></returns>
        public static string BitsToBinaryString(int[] Bits)
        {
            //Collapse all the bit values into a single string then return it
            string BitString = "";
            for (int i = 0; i < Bits.Length; i++)
                BitString += Bits[i];
            return BitString;
        }

        public static string ByteStringToBinaryString(string ByteString)
        {
            string BinaryString = "";
            for (int i = 0; i < 8; i++)
                BinaryString += ByteString[i];
            return BinaryString;
        }
    }
}