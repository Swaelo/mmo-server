// ================================================================================================================================
// File:        PacketReader.cs
// Description: Class for extracting data from network packets received from game clients
// ================================================================================================================================

using System;
using System.Text;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking
{
    public class PacketReader
    {
        //The current set of data that was recieved and our current position in the array while reading through it all
        byte[] PacketData;
        int PacketPosition;

        //Default constructor
        public PacketReader(byte[] PacketData)
        {
            //Copy all the data into the class member variable array
            this.PacketData = new byte[PacketData.Length];
            Array.Copy(PacketData, this.PacketData, PacketData.Length);
            //Start reading it from the beginning
            PacketPosition = 0;
        }

        //Reads the next block of data as an integer value
        public int ReadInt()
        {
            //Extract the integer value from the data array
            int IntValue = BitConverter.ToInt32(PacketData, PacketPosition);
            //Move the current array position forward 4 bytes (size of int), so the next read call is ready
            PacketPosition += 4;
            //Return the value extracted from the data array
            return IntValue;
        }

        //Reads the next block of data as a floating point value
        public float ReadFloat()
        {
            //Extract the floating point value from the data array
            float FloatValue = BitConverter.ToSingle(PacketData, PacketPosition);
            //Move the array position forward 4 bytes (size of float)
            PacketPosition += 4;
            //Return the value extracted from the data array
            return FloatValue;
        }

        //Reads the next block of data as a string value
        public string ReadString()
        {
            //Before every string is a single integer value placed into the data array letting us know how long the string is to follow it, read that value first
            int StringLength = ReadInt();
            //Now read the string value from the data array
            string StringValue = Encoding.ASCII.GetString(PacketData, PacketPosition, StringLength);
            //Move the array position forward to the end of the string
            PacketPosition += StringLength;
            //return the string value extract from the data packet
            return StringValue;
        }

        //Reads the next block of data as a vector3 value (3 floats in a row)
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        //Reads the next block of data as a quaternion value (4 floats in a row)
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }
    }
}
