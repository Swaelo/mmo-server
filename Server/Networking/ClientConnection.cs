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
using BepuPhysics;
using BepuPhysics.Collidables;
using Server.Misc;
using Server.Time;
using Server.Data;
using Server.Logging;
using Server.Networking.PacketSenders;

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
        public bool ClientDead = false; //Flag set by the ConnectionManager when it detects that this client has been disconnected and needs to be cleaned up
        public bool ClientDeSynced = false; //Set when a client has missed packets outside of memory, they will request all data needed to resync then tell us when to disable this

        //Objects containing all the information about the account this client is logged in to, and the character they are currently playing with
        public AccountData Account = new AccountData();
        public CharacterData Character = new CharacterData();

        //If we miss some packets from a client, we need to store any later packets in the dictionary until we receive the ones that are missing, before all can be processed
        public Dictionary<int, NetworkPacket> WaitingToProcess = new Dictionary<int, NetworkPacket>();
        public bool WaitingForMissingPackets = false;   //Set when we detect we have missed some packets, disabled once all have been received and processed to catch back up again
        public int FirstMissingPacketNumber;    //The packet order number of the first missing packet that we are currently waiting for
        public int NewestPacketWaitingToProcess;    //The order number of the newest packet that is waiting to be processed (newer packets may be received and added here before we
        //receive the older missing packets back from the client that we requested
        public int NextOutgoingPacketNumber = 0;    //Order number for the next packet to be sent to this client
        public int GetNextOutgoingPacketNumber() { NextOutgoingPacketNumber++; return NextOutgoingPacketNumber; }
        private Dictionary<int, NetworkPacket> PreviousPackets = new Dictionary<int, NetworkPacket>();  //Dictionary storing the last 25 packets sent to this client
        public int LastPacketReceived = 0;    //Order number for the next packet to be recieved from this client

        public Dictionary<int, NetworkPacket> PacketQueue = new Dictionary<int, NetworkPacket>();
        public Dictionary<int, NetworkPacket> SecondaryPacketQueue = new Dictionary<int, NetworkPacket>();
        public Dictionary<int, NetworkPacket> PacketHistory = new Dictionary<int, NetworkPacket>();
        private bool MainQueueTransmitting = false;

        //Account / Character settings
        public bool WaitingToEnter = false; //Set when client is ready to enter the game, any with this flag set are added into the physics scene at the start of each update
        public bool InGame = false; //Tracks if each client is actually in the game world with one of their characters, flagged after we have spawned them into the physics scene

        //Physics settings
        public Capsule PhysicsShape;    //The character collider physics shape objeect in the world simulation
        public TypedIndex ShapeIndex;   //Index to the current collider physics shape object
        public RigidPose ShapePose;
        public BodyActivityDescription ActivityDescription;
        public CollidableDescription PhysicsDescription;    //Description of the clients character collider physics object in the world simulation
        public BodyDescription PhysicsBody; //The world simulation physics object for this clients character controller when they are in the game world
        public int BodyHandle = -1; //The reference handle pointing to this clients world simulation physics object for this clients character controller

        //Returns a NetworkPacket from the list of previous packets sent to this client
        public NetworkPacket GetPreviousPacket(int PacketOrderNumber)
        {
            //Return null if the packet cannot be found in the dictionary
            if (!PreviousPackets.ContainsKey(PacketOrderNumber))
                return null;
            return PreviousPackets[PacketOrderNumber];
        }

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
            NetworkConnection.SendBufferSize = 262144;
            NetworkConnection.ReceiveBufferSize = 262144;
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
            //Read in the size of the payload data, making sure the connection is still opened
            int PacketSize = -1;
            try { PacketSize = DataStream.EndRead(Result); }
            //Print an error message, flag the client as dead and exit the function if any exception occurs
            catch(IOException Exception)
            {
                MessageLog.Error(Exception, "Error reading packet size, connection is no longer open.");
                ClientDead = true;
                return;
            }

            //Copy the data buffer over into a new array and reset the buffer array for reading in the next packet
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            DataBuffer = new byte[NetworkConnection.Available];
            //Immediately start reading packets again from the client back into the data buffer, making sure the connection is still open
            try { DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null); }
            //Print an error, flag the client as dead and exit the function if any exception occurs
            catch(IOException Exception)
            {
                MessageLog.Error(Exception, "Error reregistering packet reader function to start accepting packet data from the client again.");
                ClientDead = true;
                return;
            }

            //If the connection is new then complete the handshake and upgrade the connection to websockets
            if (!ConnectionUpgraded)
                UpgradeConnection(PacketBuffer);
            //Otherwise we need to extract the packet data from the buffer and decode it so we can read and process the messages within
            else if(PacketSize > 0)
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

        public void SendPacket(string PacketData)
        {
            NetworkPacket NewPacket = new NetworkPacket(PacketData);
            SendPacket(NewPacket);
        }

        //Transmits a packet to this client, and stores it in the list of previously sent packets
        public void SendPacket(NetworkPacket Packet)
        {
            //Check if we should be transmitting packets right now
            if (!Program.World.PacketQueueEnabled)
                return;

            //Frame the packet data and convert into a byte array ready for transmission
            byte[] PacketData = GetFrameFromString(Packet.PacketData);
            string PacketString = Encoding.UTF8.GetString(PacketData);

            //Now try sending to the client, making sure the connection is still active
            try { DataStream.BeginWrite(PacketData, 0, PacketString.Length, null, null); }
            catch(IOException Exception)
            {
                MessageLog.Error(Exception, "Error sending packet to dead client connection, marking them for cleanup.");
                ClientDead = true;
            }
        }

        //Transmits a range of missing packets back to the client who requested them to be resent
        public void ResendMissingPackets(ClientConnection Client, int FirstMissingPacketNumber, int LastMissingPacketNumber)
        {
            //First check that these missing packets are still stored in memory
            if(!PreviousPackets.ContainsKey(FirstMissingPacketNumber))
            {
                //Log an error and kick the player from the server
                MessageLog.Print("ERROR: Client requested missing packets no longer stored in memory, they must reconnect to continue playing.");
                SystemPacketSender.SendKickedFromServer(Client.NetworkID, "Missed packets no longer kept in memory, you must reconnect to continue playing.");
                //Mark the client as dead so they get cleaned up and removed from everyones gameworld
                Client.ClientDead = true;
                return;
            }

            //Combine the data of every missing packet into a single string object
            string MissingPacketsData = "";
            for (int i = FirstMissingPacketNumber; i < LastMissingPacketNumber + 1; i++)
            {
                MissingPacketsData += PreviousPackets[i].PacketData;
                MessageLog.Print("Sending missing packet #" + i + " back to client.");
            }

            //Frame the data and convert to binary, then gert the total payload size
            byte[] PacketData = GetFrameFromString(MissingPacketsData);
            int PayloadSize = Encoding.UTF8.GetString(PacketData).Length;

            //Send the missing packets to the client
            try { DataStream.BeginWrite(PacketData, 0, PayloadSize, null, null); }
            catch (IOException Exception)
            {
                MessageLog.Error(Exception, "Error sending missing packets back to now dead client, marking for cleanup.");
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

        //Adds a NetworkPacket to the outgoing packets queue
        public void QueuePacket(NetworkPacket Packet)
        {
            //Add the next packet order number onto the front of the packets data
            int OrderNumber = GetNextOutgoingPacketNumber();
            Packet.AddPacketOrderNumber(OrderNumber);

            //Add it into whatever queue we should be using
            if (!MainQueueTransmitting)
                PacketQueue.Add(OrderNumber, Packet);
            else
                SecondaryPacketQueue.Add(OrderNumber, Packet);

            //Store all packets in the history dictionary by their order numbers
            PacketHistory.Add(OrderNumber, Packet);

            //Maintain a maximum history size of 150 packets
            if (PacketHistory.Count > 150)
                PacketHistory.Remove(OrderNumber - 150);
        }

        //Use the secondary queue while we transmit all packets to the client, then change back to the main queue afterwards
        public void TransmitPackets()
        {
            //Reset the secondary queue then enable the flag so it gets used while be transmit the others
            SecondaryPacketQueue.Clear();
            MainQueueTransmitting = true;

            //Create a new string, and combine the data of every packet in the main queue into it
            string TotalData = "";
            foreach (KeyValuePair<int, NetworkPacket> Packet in PacketQueue)
                TotalData += Packet.Value.PacketData;

            //Transmit the total set of data if theres some to be sent
            if(TotalData != "")
            {
                MessageLog.Print("Sending Packet #" + NextOutgoingPacketNumber);

                //Frame the packet data and convert to bytes array, compute the total payload size
                byte[] PacketData = GetFrameFromString(TotalData);
                int PayloadLength = Encoding.UTF8.GetString(PacketData).Length;

                //Try sending to the client, making sure the connection is still active
                try { DataStream.BeginWrite(PacketData, 0, PayloadLength, null, null); }
                catch(IOException Exception)
                {
                    MessageLog.Error(Exception, "Error sending packet to dead client connections, marking them for cleanup.");
                    ClientDead = true;
                }
            }

            //Reset the MainQueue, copy anything in SecondaryQueue over to it, then reenable the main queue again
            PacketQueue.Clear();
            foreach (KeyValuePair<int, NetworkPacket> Packet in SecondaryPacketQueue)
                PacketQueue.Add(Packet.Key, Packet.Value);
            MainQueueTransmitting = false;
        }
    }
}