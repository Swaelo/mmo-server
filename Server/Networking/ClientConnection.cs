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
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Server.Misc;
using Server.Time;
using Server.Logging;

namespace Server.Networking
{
    public class ClientConnection
    {
        //Connection Settings
        public int NetworkID;   //Each client has a unique network ID so they dont get mixed up
        public TcpClient NetworkConnection; //The servers network connection to this client
        private bool ConnectionUpgraded = false;    //When clients first connect we need to handshake them
        public NetworkStream DataStream;    //Information is transmitted back and forth with this
        public byte[] DataBuffer;   //Data is streamed into here during asynchronous reading
        public PointInTime LastCommunication;   //The exact moment we last had communications with this client connection

        //Account / Character settings
        public bool WaitingToEnter = false; //Set when client is ready to enter the game, any with this flag set are added into the physics scene at the start of each update
        public bool InGame = false; //Tracks if each client is actually in the game world with one of their characters, flagged after we have spawned them into the physics scene
        public string AccountName = "";  //The account this user is currently logged into
        public string CharacterName = "";    //The name of the character this user is currently playing with
        public Vector3 CharacterPosition = Vector3.Zero;   //The world position of the character this user is currently playing with
        public Quaternion CharacterRotation;    //Current rotation of the character this user is currently playing with
        public bool NewPositionReceived = false;    //Flag set when a new updated position value has been recieved from the game client
        public Vector3 NewPosition = Vector3.Zero;  //New updated position value sent to use from the game client

        //Physics settings
        public Capsule PhysicsShape;    //The character collider physics shape objeect in the world simulation
        public TypedIndex ShapeIndex;   //Index to the current collider physics shape object
        public RigidPose ShapePose;
        public BodyActivityDescription ActivityDescription;
        public CollidableDescription PhysicsDescription;    //Description of the clients character collider physics object in the world simulation
        public BodyDescription PhysicsBody; //The world simulation physics object for this clients character controller when they are in the game world
        public int BodyHandle = -1; //The reference handle pointing to this clients world simulation physics object for this clients character controller

        public bool ClientDead = false; //Flag set by the ConnectionManager when it detects that this client has been disconnected and needs to be cleaned up

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="NewConnection">ConnectionManager detects new client connections then uses it to make this</param>
        public ClientConnection(TcpClient NewConnection)
        {
            //Store the connection to the new client and assign them a new network ID
            NetworkConnection = NewConnection;
            NetworkID = ((IPEndPoint)NetworkConnection.Client.RemoteEndPoint).Port;

            //Set the time of last communication to the moment the object is first created
            LastCommunication = new PointInTime();

            //Set up the datastream and buffer, then start listening for messages from the client
            NetworkConnection.SendBufferSize = 4096;
            NetworkConnection.ReceiveBufferSize = 4096;
            DataStream = NetworkConnection.GetStream();
            DataBuffer = new byte[NetworkConnection.Available];
            DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null);
        }

        /// <summary>
        /// Triggers after completion of asynchronous datastream reading has completed transmitting data
        /// </summary>
        /// <param name="Result">Represents the status of an asynchronous operation</param>
        private void ReadPacket(IAsyncResult Result)
        {
            //Update the timer tracking when we last had communications with this client
            LastCommunication = new PointInTime();

            //Copy the current packet data into a new array, then clear it out and immediately start using it to start streaming in data again from the client
            int PacketSize = DataStream.EndRead(Result);
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            DataBuffer = new byte[NetworkConnection.Available];

            //Make sure the socket connection is still open before we try reading more data from the stream
            try { DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null); }
            catch (IOException Exception) { MessageLog.Error(Exception, "Error trying to read data from the network stream"); }

            //Upgrade this clients connection if it is brand new
            if (!ConnectionUpgraded)
                UpgradeConnection(PacketBuffer);
            //Otherwise we need to extract the clients message from the buffer and decode it to become readable again
            else if (PacketSize != 0)
            {
                //When recieving messages from clients they will be encoded, visit https://tools.ietf.org/html/rfc6455#section-5.2 for more information on how decoding works

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

                //With a length between 0-125 we continue as normal
                //With a length equal to 126, we read bytes 3-4 to find the actual length
                if (PayloadLength == 126)
                {
                    byte[] PayloadBytes = DataExtractor.ReadBytes(PacketBuffer, 3, 4);
                    PayloadBinary = BinaryConverter.ByteArrayToBinaryString(PayloadBytes);
                    PayloadLength = BinaryConverter.BinaryStringToDecimal(PayloadBinary);
                    //Increment the DecodingMask and PayloadData indices by 2, as 3,4 contained the payload length
                    DecodingMaskIndex += 2;
                    PayloadDataIndex += 2;
                }
                //With a length equal to 127, we read bytes 3-10 to find the actual length
                else if (PayloadLength == 127)
                {
                    byte[] PayloadBytes = DataExtractor.ReadBytes(PacketBuffer, 3, 10);
                    PayloadBinary = BinaryConverter.ByteArrayToBinaryString(PayloadBytes);
                    PayloadLength = BinaryConverter.BinaryStringToDecimal(PayloadBinary);
                    //Increment the DecodingMask and PayloadData indices by 8, as bytes 3-10 contained the payload length
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
                    ClientDead = true;
                //Otherwise we just pass the message onto the packet handler as normal so it can be processed accordingly
                else
                    PacketHandler.ReadClientPacket(NetworkID, FinalMessage);
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

        //Transmits a message to this client 
        public void SendPacket(string PacketMessage)
        {
            //Put the network frame around the message and convert that to bytes
            byte[] PacketData = GetFrameFromString(PacketMessage);
            string PacketString = Encoding.UTF8.GetString(PacketData);

            //Make sure the connection to this client is still open before we begin writing data to their stream
            try { DataStream.BeginWrite(PacketData, 0, PacketData.Length, null, null); }
            catch (IOException Exception)
            {
                //Catch IO Exception caused from trying to send packet to a dead client connection, mark them as needing to be cleaned up
                MessageLog.Error(Exception, "Error sending packet data to client #" + NetworkID + ", their connection has been lost.");
                ClientDead = true;
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
            for(i = 0; i < indexStartRawData; i++)
            {
                response[responseIdx] = frame[i];
                responseIdx++;
            }

            //Add the data bytes to the response
            for(i = 0; i < length; i++)
            {
                response[responseIdx] = bytesRaw[i];
                responseIdx++;
            }

            return response;
        }
    }
}