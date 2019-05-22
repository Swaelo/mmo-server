// ================================================================================================================================
// File:        PacketWriter.cs
// Description: Class used to define an array of information that is going to be send through the network to one of the clients
//              While the object is active, more values can be added dynamically and it will keep everything formatted correctly
//              When all the data has been added it can be acquired in the formatted array with the ToArray function which can be
//              passed onto the ConnectionManager class to be sent through the network to the target game client which is connected
// ================================================================================================================================

using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking
{
    public class PacketWriter
    {
        //Define a list of byte which will be the current set of data, added on to over time until the packet is ready to be sent
        List<byte> DataBuffer;

        //Default constructor
        public PacketWriter()
        {
            //initialize the data buffer
            DataBuffer = new List<byte>();
        }

        //Returns the databuffer as a normal byte array
        public byte[] ToArray()
        {
            return DataBuffer.ToArray();
        }

        //Converts an integer value to byte format and adds it to the front of the data buffer
        public void WriteInt(int IntValue)
        {
            DataBuffer.AddRange(BitConverter.GetBytes(IntValue));
        }

        //Converts an floating point value to byte format and adds it to the front of the data buffer
        public void WriteFloat(float FloatValue)
        {
            DataBuffer.AddRange(BitConverter.GetBytes(FloatValue));
        }

        //Converts a string value to byte format and adds it to the front of the data buffer
        public void WriteString(string StringValue)
        {
            //Before writing in the string value, put an integer value beforehand indicating the length of the string so it can be read out properly later on
            DataBuffer.AddRange(BitConverter.GetBytes(StringValue.Length));
            //Now add the whole string to the end of the buffer
            DataBuffer.AddRange(Encoding.ASCII.GetBytes(StringValue));
        }

        //Converts an Vector3 value (3 floats) to byte format and adds it to the front of the data buffer
        public void WriteVector3(Vector3 VectorValue)
        {
            WriteFloat(VectorValue.X);
            WriteFloat(VectorValue.Y);
            WriteFloat(VectorValue.Z);
        }

        //Converts a Quaternion value (4 floats) to byte format and adds it to the front of the data buffer
        public void WriteQuaternion(Quaternion QuaternionValue)
        {
            WriteFloat(QuaternionValue.X);
            WriteFloat(QuaternionValue.Y);
            WriteFloat(QuaternionValue.Z);
            WriteFloat(QuaternionValue.W);
        }
    }
}
