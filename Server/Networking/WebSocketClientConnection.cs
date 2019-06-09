// ================================================================================================================================
// File:        WebSocketClientConnection.cs
// Description: WebSocket implementation of the ClientConnection class
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using Server.Interface;
using Server.Maths;

namespace Server.Networking
{
    public class WebSocketClientConnection
    {
        public int NetworkID;
        public TcpClient NetworkConnection;
        public NetworkStream DataStream;
        public byte[] DataBuffer;

        public WebSocketClientConnection(TcpClient NewConnection)
        {
            NetworkConnection = NewConnection;
            NetworkID = ((IPEndPoint)NetworkConnection.Client.RemoteEndPoint).Port;

            NetworkConnection.SendBufferSize = 4096;
            NetworkConnection.ReceiveBufferSize = 4096;
            DataStream = NetworkConnection.GetStream();
            DataBuffer = new byte[NetworkConnection.Available];
            DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null);
        }

        private void ReadPacket(IAsyncResult Result)
        {
            int PacketSize = DataStream.EndRead(Result);
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            DataBuffer = new byte[NetworkConnection.Available];
            DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null);

            String Data = Encoding.UTF8.GetString(PacketBuffer);
            //Handshake new client connections
            if(new System.Text.RegularExpressions.Regex("^GET").IsMatch(Data))
            {
                const string eol = "\r\n";
                byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                    + "Connection: Upgrade" + eol
                    + "Upgrade: websocket" + eol
                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                        System.Security.Cryptography.SHA1.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(
                                new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(Data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                )
                            )
                        ) + eol
                        + eol);

                DataStream.BeginWrite(response, 0, response.Length, null, null);
            }
            else if(PacketSize != 0)
            {
                string FirstByte = Convert.ToString(PacketBuffer[0], 2).PadLeft(8, '0');
                string OpCode = "" + FirstByte[4] + FirstByte[5] + FirstByte[6] + FirstByte[7];
                Log.PrintDebugMessage("FIN:" + FirstByte[0] + " RSV1:" + FirstByte[1] + " RSV2:" + FirstByte[2] + " RSV3:" + FirstByte[3] + " OpCode:" + OpCode);

                string SecondByte = Convert.ToString(PacketBuffer[1], 2).PadLeft(8, '0');
                string PayloadLength = "" + SecondByte[1] + SecondByte[2] + SecondByte[3] + SecondByte[4] + SecondByte[5] + SecondByte[6] + SecondByte[7];
                int PayloadLengthValue = BinaryValueConverter.BinaryStringToDecimal(PayloadLength);
                Log.PrintDebugMessage("MASK:" + SecondByte[0] + " PayloadLength:" + PayloadLengthValue);

                //Extract the masking key, then the encoded message bytes, then use the key to decode them
                byte[] Mask = null;
                byte[] Encoded = null;
                byte[] Decoded = null;

                //Payload Length between 0 and 125 means we already have the payload length figured out
                if(PayloadLengthValue >= 0 && PayloadLengthValue <= 125)
                {
                    //The next 4 bytes are the mask bytes used to decode the message
                    Mask = new byte[4] { PacketBuffer[2], PacketBuffer[3], PacketBuffer[4], PacketBuffer[5]};

                    //Get the encoded message bytes
                    Encoded = new byte[PayloadLengthValue];
                    for(int i = 0; i < PayloadLengthValue; i++)
                        Encoded[i] = PacketBuffer[6 + i];

                    //Use the decoding algorithm to find the final string message sent from the client
                    Decoded = new byte[PayloadLengthValue];
                    for (int i = 0; i < Encoded.Length; i++)
                        Decoded[i] = (byte)(Encoded[i] ^ Mask[i % 4]);
                }
                //Payload Length of 126 means the following two bytes are the length
                else if(PayloadLengthValue == 126)
                {
                    //Get the binary values of the next two bytes, then combine them all together and convert to decimal value to get the proper PayloadLengthValue
                    string ThirdByte = Convert.ToString(PacketBuffer[2], 2).PadLeft(8, '0');
                    string ThirdByteBinary = BinaryValueConverter.ByteStringToBinaryString(ThirdByte);
                    string FourthByte = Convert.ToString(PacketBuffer[3], 2).PadLeft(8, '0');
                    string FourthByteBinary = BinaryValueConverter.ByteStringToBinaryString(FourthByte);
                    string TotalBinaryString = ThirdByteBinary + FourthByteBinary;
                    PayloadLengthValue = BinaryValueConverter.BinaryStringToDecimal(TotalBinaryString);

                    //The next 4 bytes are the mask bytes used to decode the message
                    Mask = new byte[4] { PacketBuffer[4], PacketBuffer[5], PacketBuffer[6], PacketBuffer[7] };

                    //Get the encoded message bytes
                    Encoded = new byte[PayloadLengthValue];
                    for (int i = 0; i < PayloadLengthValue; i++)
                        Encoded[i] = PacketBuffer[8 + i];

                    //Use the decoding algorithm to find the final string message sent from the client
                    Decoded = new byte[PayloadLengthValue];
                    for (int i = 0; i < Encoded.Length; i++)
                        Decoded[i] = (byte)(Encoded[i] ^ Mask[i % 4]);
                }
                //Payload Length of 127 means the following 8 bytes are the length
                else if(PayloadLengthValue == 127)
                {
                    //Get the binary values of the next eight bytes, then combine them all together and convert to decimal value to get the proper PayloadLengthValue
                    string ThirdByte = Convert.ToString(PacketBuffer[2], 2).PadLeft(8, '0');
                    string ThirdByteBinary = BinaryValueConverter.ByteStringToBinaryString(ThirdByte);
                    string FourthByte = Convert.ToString(PacketBuffer[3], 2).PadLeft(8, '0');
                    string FourthByteBinary = BinaryValueConverter.ByteStringToBinaryString(FourthByte);
                    string FifthByte = Convert.ToString(PacketBuffer[4], 2).PadLeft(8, '0');
                    string FifthByteBinary = BinaryValueConverter.ByteStringToBinaryString(FifthByte);
                    string SixthByte = Convert.ToString(PacketBuffer[5], 2).PadLeft(8, '0');
                    string SixthByteBinary = BinaryValueConverter.ByteStringToBinaryString(SixthByte);
                    string SeventhByte = Convert.ToString(PacketBuffer[6], 2).PadLeft(8, '0');
                    string SeventhByteBinary = BinaryValueConverter.ByteStringToBinaryString(SeventhByte);
                    string EighthByte = Convert.ToString(PacketBuffer[7], 2).PadLeft(8, '0');
                    string EighthByteBinary = BinaryValueConverter.ByteStringToBinaryString(EighthByte);
                    string NinthByte = Convert.ToString(PacketBuffer[8], 2).PadLeft(8, '0');
                    string NinthByteBinary = BinaryValueConverter.ByteStringToBinaryString(NinthByte);
                    string TenthByte = Convert.ToString(PacketBuffer[9], 2).PadLeft(8, '0');
                    string TenthByteBinary = BinaryValueConverter.ByteStringToBinaryString(TenthByte);
                    string TotalBinaryString = ThirdByteBinary + FourthByteBinary + FifthByteBinary + SixthByteBinary + SeventhByteBinary + EighthByteBinary + NinthByteBinary + TenthByteBinary;
                    PayloadLengthValue = BinaryValueConverter.BinaryStringToDecimal(TotalBinaryString);

                    //The next 4 bytes are the mask bytes used to decode the message
                    Mask = new byte[4] { PacketBuffer[10], PacketBuffer[11], PacketBuffer[12], PacketBuffer[13] };

                    //Get the encoded message bytes
                    Encoded = new byte[PayloadLengthValue];
                    for (int i = 0; i < PayloadLengthValue; i++)
                        Encoded[i] = PacketBuffer[14 + i];

                    //Use the decoding algorithm to find the final string message sent from the client
                    Decoded = new byte[PayloadLengthValue];
                    for (int i = 0; i < Encoded.Length; i++)
                        Decoded[i] = (byte)(Encoded[i] ^ Mask[i % 4]);
                }

                //Loop through each of the decoded bytes, converting each to its proper string value, appending onto the FinalMessage
                string FinalMessage = "";
                for(int i = 0; i < Decoded.Length; i++)
                {
                    //Convert each decoded byte into binary string
                    string DecodedString = Convert.ToString(Decoded[i], 2).PadLeft(8, '0');
                    //Next convert the binary string into base 2 bytes array
                    List<byte> List = new List<byte>();
                    for(int j = 0; j < DecodedString.Length; j += 8)
                    {
                        string T = DecodedString.Substring(j, 8);
                        List.Add(Convert.ToByte(T, 2));
                    }
                    //Finally turn the base2 bytes array into ASCII string value
                    string Text = Encoding.ASCII.GetString(List.ToArray());

                    //Append each section of the decoded message onto the final string to build the complete message
                    FinalMessage += Text;
                }
                
                //Now we finally have the message that was sent to us from the game client
                Log.PrintDebugMessage("Final Client Message: " + FinalMessage);
            }
        }
    }
}