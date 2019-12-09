// ================================================================================================================================
// File:        ClientConnection.cs
// Description: WebSocket implementation of the ClientConnection class
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Server.Misc;
using Server.Time;
using Server.Data;
using Server.Logging;

namespace Server.Networking
{
    public class ClientConnection
    {
        //Connection Information
        public int ClientID;
        public TcpClient Connection;
        public NetworkStream DataStream;
        public byte[] DataBuffer;

        //Connection Settings
        private bool ConnectionUpgraded = false;
        public PointInTime LastCommunication;
        public bool ConnectionDead = false;

        //User Account / Player Character Details
        public AccountData Account = new AccountData();
        public CharacterData Character = new CharacterData();

        //Packet Ordering / Queueing
        private int NextOrderNumber = 0;
        public int GetNextOrderNumber() { return ++NextOrderNumber; }
        private Dictionary<int, NetworkPacket> PacketQueue = new Dictionary<int, NetworkPacket>();
        private Dictionary<int, NetworkPacket> PacketHistory = new Dictionary<int, NetworkPacket>();
        public bool PacketsToResend = false;
        public int ResendFrom = -1;
        public int LastPacketRecieved = 0;

        //Default Constructor
        public ClientConnection(TcpClient Connection)
        {
            //Store the connection and assign a new network ID
            this.Connection = Connection;
            ClientID = ((IPEndPoint)Connection.Client.RemoteEndPoint).Port;
            //Set up the datastream and buffer for listening for messages from the client
            Connection.SendBufferSize = 262144;
            Connection.ReceiveBufferSize = 262144;
            DataStream = Connection.GetStream();
            DataBuffer = new byte[Connection.Available];
            DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null);
            //Set the time of last contact to now
            LastCommunication = new PointInTime();
        }

        public void QueuePacket(NetworkPacket Packet)
        {
            //Add the order number to the front of the packet data
            int OrderNumber = GetNextOrderNumber();
            Packet.AddPacketOrderNumber(OrderNumber);
            //Add it to the queue, and history
            PacketQueue.Add(OrderNumber, Packet);
            PacketHistory.Add(OrderNumber, Packet);
            //Maintain a maximum 150 of the previous packets in memory
            if (PacketHistory.Count > 150)
                PacketHistory.Remove(OrderNumber - 150);
        }
        
        public void TransmitPackets()
        {
            //Copy the queue into a new array and reset it so others can be queued
            Dictionary<int, NetworkPacket> OutgoingPackets = new Dictionary<int, NetworkPacket>(PacketQueue);
            PacketQueue.Clear();
            //Data of all in the queue will be combined into a string for transmission
            string PacketData = "";
            //If the client missed some packets then we need to resend all of them so they can catch back up
            if(PacketsToResend)
            {
                //Make sure the missing packets being requested are still being stored in the history
                if(!PacketHistory.ContainsKey(ResendFrom))
                {
                    //Close clients connections who ask for packets outside of memory, as they cant be caught back up from that
                    ConnectionDead = true;
                    return;
                }
                //Add the data of every packet from the first the client missed, and everything after it
                for (int i = ResendFrom; i < LastPacketRecieved + 1; i++)
                    PacketData += PacketHistory[i].PacketData;
                //No more packets to resend after this
                PacketsToResend = false;
            }
            //Otherwise we just gather up all the packets in the outgoing queue for transmission
            else
            {
                foreach (KeyValuePair<int, NetworkPacket> Packet in OutgoingPackets)
                    PacketData += Packet.Value.PacketData;
            }
            //Transmit any available data to the client
            if(PacketData != "")
            {
                //Frame the data and get the payload size ready for transmission
                byte[] PacketBytes = GetFrameFromString(PacketData);
                int PacketSize = Encoding.UTF8.GetString(PacketBytes).Length;
                //Transmit the data to the client, making sure their connection is still open
                try { DataStream.BeginWrite(PacketBytes, 0, PacketSize, null, null); }
                catch(IOException Exception)
                {
                    //Close the clients connection if its no longer open
                    MessageLog.Error(Exception, "Error transmitting packets to the client");
                    ConnectionDead = true;
                }
            }
        }

        //Frames the message correctly so it can be sent to the client
        private static byte[] GetFrameFromString(string Message)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.Default.GetBytes(Message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)(128 + (int)2);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, responseIdx = 0;

            //Add the frame bytes to the response
            for (i = 0; i < indexStartRawData; i++)
            {
                response[responseIdx] = frame[i];
                responseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[responseIdx] = bytesRaw[i];
                responseIdx++;
            }

            return response;
        }

        /// <summary>
        /// Triggers after completion of asynchronous datastream reading has completed transmitting data
        /// </summary>
        /// <param name="Result">Represents the status of an asynchronous operation</param>
        private void ReadPacket(IAsyncResult Result)
        {
            //Read in the size of the payload data, making sure the connection is still opened
            int PacketSize = -1;
            try { PacketSize = DataStream.EndRead(Result); }
            //Print an error message, flag the client as dead and exit the function if any exception occurs
            catch (IOException Exception)
            {
                MessageLog.Error(Exception, "Error reading packet size, connection no longer open.");
                ConnectionDead = true;
                return;
            }

            //Copy the data buffer over into a new array and reset the buffer array for reading in the next packet
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            DataBuffer = new byte[Connection.Available];
            //Immediately start reading packets again from the client back into the data buffer, making sure the connection is still open
            try { DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null); }
            //Print an error, flag the client as dead and exit the function if any exception occurs
            catch (IOException Exception)
            {
                MessageLog.Error(Exception, "Error registering packet reader function, client connection no longer open.");
                ConnectionDead = true;
                return;
            }

            //If the connection is new then complete the handshake and upgrade the connection to websockets
            if (!ConnectionUpgraded)
                UpgradeConnection(PacketBuffer);
            //Otherwise we need to extract the packet data from the buffer and decode it so we can read and process the messages within
            else if (PacketSize > 0)
            {
                //Visit https://tools.ietf.org/html/rfc6455#section-5.2 for more information on how this decoding works
                //Lets first extract the data from the first byte
                byte FirstByte = PacketBuffer[0];
                bool FIN = DataExtractor.ReadBit(FirstByte, 0);   //Value of 1 indicates if this is the final fragment of the message, this first fragment MAY also be the final fragment
                bool RSV1 = DataExtractor.ReadBit(FirstByte, 1);  //Set to 0 unless an extension is negotatied that defines meanings for non-zero values. Unexpected non-zero values means we should close down the connection.
                bool RSV2 = DataExtractor.ReadBit(FirstByte, 2);
                bool RSV3 = DataExtractor.ReadBit(FirstByte, 3);
                bool[] OpCode = DataExtractor.ReadBits(FirstByte, 4, 7);

                //Extracting the second byte from the packet buffer
                byte SecondByte = PacketBuffer[1];
                bool MASK = DataExtractor.ReadBit(SecondByte, 0);

                //Before we go any further we need to figure out the size of the payload data, as this may effect where we read the rest of the data from
                //Converting the 2nd byte to a binary string, then converting bits 1-7 to decimal gives us the first possible length value of the payload data
                string SecondByteBinary = BinaryConverter.ByteToBinaryString(PacketBuffer[1]);
                string PayloadBinary = SecondByteBinary.Substring(1, 7);
                int PayloadLength = BinaryConverter.BinaryStringToDecimal(PayloadBinary);

                //Byte indices where we will begin reading in the decoding mask and payload data later on, these will be updated if we needed to read extra bytes to find out the payload length
                int DecodingMaskIndex = 2;
                int PayloadDataIndex = 6;

                //PayloadLength between 0-125 represents the actual length value
                //With a length equal to 126, we read bytes 2-3 to find the length value
                if (PayloadLength == 126)
                {
                    //Bytes 2 and 3 interpreted as 16bit unsigned integer give the PayloadLength
                    byte[] LengthBytes = DataExtractor.ReadBytes(PacketBuffer, 2, 3);
                    PayloadBinary = BinaryConverter.ByteArrayToBinaryString(LengthBytes);
                    PayloadLength = BinaryConverter.BinaryStringToDecimal(PayloadBinary);
                    //Increment the DecodingMask and PayloadData indices by 2, as 3,4 contained the payload length
                    DecodingMaskIndex += 2;
                    PayloadDataIndex += 2;
                }
                //Write a length equal to 127, we read bytes 2-9 to find the length value
                else if (PayloadLength == 127)
                {
                    //Bytes 2-9 interpreted as a 64bit unsigned integer give the PayloadLength
                    byte[] LengthBytes = DataExtractor.ReadBytes(PacketBuffer, 2, 9);
                    PayloadBinary = BinaryConverter.ByteArrayToBinaryString(LengthBytes);
                    PayloadLength = BinaryConverter.BinaryStringToDecimal(PayloadBinary);
                    DecodingMaskIndex += 8;
                    PayloadDataIndex += 8;
                }

                //Extract the decoding mask bytes from the packet buffer
                byte[] DecodingMask = new byte[4] { PacketBuffer[DecodingMaskIndex], PacketBuffer[DecodingMaskIndex + 1], PacketBuffer[DecodingMaskIndex + 2], PacketBuffer[DecodingMaskIndex + 3] };

                //Extract the payload data from the packet buffer, using the mask to decode each byte as we extract it from the packet buffer
                byte[] PayloadData = new byte[PayloadLength];
                for (int i = 0; i < PayloadLength; i++)
                    PayloadData[i] = (byte)(PacketBuffer[PayloadDataIndex + i] ^ DecodingMask[i % 4]);

                //Convert the PayloadData array into an ASCII string
                string FinalMessage = Encoding.ASCII.GetString(PayloadData);

                //If the FinalMessage value comes through as "\u0003?" then the connection has been closed from the client side, so we need to set
                //them as dead so they get cleaned up by the simulation
                if (FinalMessage == "\u0003?")
                    ConnectionDead = true;
                //Otherwise we just pass the message onto the packet handler as normal so it can be processed accordingly
                else
                    PacketHandler.ReadClientPacket(ClientID, FinalMessage);
            }
        }

        /// <summary>
        /// Handshakes with a new network client, upgrading their connection to WebSocket from HTTP
        /// </summary>
        private void UpgradeConnection(byte[] PacketBuffer)
        {
            //Convert the data in the packet buffer into string format
            string PacketData = Encoding.UTF8.GetString(PacketBuffer);

            //Make sure the new client sent a proper GET request before we complete the handshake
            if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(Encoding.UTF8.GetString(PacketBuffer)))
            {
                //Return the correct response to complete the handshake and upgrade the client to websockets
                string EOL = "\r\n";
                byte[] HandshakeResponse = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols" + EOL
                    + "Connection: Upgrade" + EOL
                    + "Upgrade: websocket" + EOL
                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                        System.Security.Cryptography.SHA1.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(
                                new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(PacketData).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                )
                            )
                        ) + EOL
                        + EOL);

                //Send the completed handshake response to the client
                DataStream.BeginWrite(HandshakeResponse, 0, HandshakeResponse.Length, null, null);
            }

            //Take note that we have completed upgrading this clients connection
            ConnectionUpgraded = true;
        }
    }
}