// ================================================================================================================================
// File:        DataExtractor.cs
// Description: Helper functions used by the ClientConnection class when reading messages sent in from game clients
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

namespace Server.Misc
{
    public static class DataExtractor
    {
        /// <summary>
        /// Returns a subset of bytes from an initial larger set
        /// </summary>
        /// <param name="ByteArray">The array containing the bytes to be extracted</param>
        /// <param name="StartingIndex">Index in the ByteArray of the first byte value to be extracted</param>
        /// <param name="EndingIndex">Index in the ByteArray of the last byte value to be extracted</param>
        /// <returns>Array of all the byte values that were requested</returns>
        public static byte[] ReadBytes(byte[] ByteArray, int StartIndex, int EndIndex)
        {
            //First figure out how many byte values are being requested, then create an array to store them all
            int ByteCount = EndIndex - StartIndex + 1;
            byte[] ByteValues = new byte[ByteCount];

            //Extract each of the requested bytes, storing each of them into the new array
            int CurrentIndex = StartIndex;
            for (int i = 0; i < ByteCount; i++)
                ByteValues[i] = ByteArray[CurrentIndex++];

            //Return the array of requested byte values
            return ByteValues;
        }

        /// <summary>
        /// Returns the value of a single bit within the given byte
        /// </summary>
        /// <param name="Byte">The byte which contains the desired bit value</param>
        /// <param name="BitIndex">The index of the bit value to be retrieved</param>
        /// <returns>The value of the bit that was requested</returns>
        public static bool ReadBit(byte Byte, int BitIndex)
        {
            return (Byte & (1 << BitIndex - 1)) != 0;
        }

        /// <summary>
        /// Extracts a set of bit values from the given Byte and returns them in an array
        /// </summary>
        /// <param name="Byte">The byte containing the bit values</param>
        /// <param name="StartIndex">Index inside the byte of the first value requested</param>
        /// <param name="EndIndex">Index inside the byte of the last value requested</param>
        /// <returns>An array of bit values that was requested</returns>
        public static bool[] ReadBits(byte Byte, int StartIndex, int EndIndex)
        {
            //First figure out how many bit values are being requested, then create an array to store them all
            int BitCount = EndIndex - StartIndex + 1;
            bool[] BitValues = new bool[BitCount];

            //Now extract each bit value, storing them all into the new array
            int CurrentIndex = StartIndex;
            for (int i = 0; i < BitCount; i++)
                BitValues[i] = ReadBit(Byte, CurrentIndex++);

            //Return the array of requested bit values
            return BitValues;
        }
    }
}