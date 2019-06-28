// ================================================================================================================================
// File:        WebSocketPacketReader.cs
// Description: for extracting data from network packets received from webgl game clients
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

namespace Server.Networking
{
    public class WebSocketPacketReader
    {
        string PacketString;
        int StringPosition;

        public WebSocketPacketReader(string PacketString)
        {
            this.PacketString = PacketString;
            StringPosition = 0;
        }


        //public int ReadInt()
        //{
        //    int IntValue = (int)PacketString[StringPosition];
        //    StringPosition += 1;
        //    return IntValue;
        //}

        //public float ReadFloat()
        //{
        //    float FloatValue = (float)PacketString[StringPosition];
        //    StringPosition += 1;
        //    return FloatValue;
        //}

        //public string ReadString()
        //{
        //    int StringLength = ReadInt();
        //    string StringValue = PacketString.Substring(StringPosition, StringPosition + StringLength);
        //    StringPosition += StringLength;
        //    return StringValue;
        //}

        //public Vector3 ReadVector3()
        //{
        //    return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        //}

        //public Quaternion ReadQuaternion()
        //{
        //    return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        //}
    }
}