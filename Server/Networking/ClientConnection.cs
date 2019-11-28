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
            //Update the timer tracking when we last had communications with this client
            LastCommunication = new PointInTime();

            //Copy the current packet data into a new array, then clear it out and immediately start using it to start streaming in data again from the client
            int PacketSize = DataStream.EndRead(Result);
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            DataBuffer = new byte[NetworkConnection.Available];

            try { DataStream.BeginRead(DataBuffer, 0, DataBuffer.Length, ReadPacket, null); }
            catch(IOException Exception) { MessageLog.Error(Exception, "Error sending packet to client, their connection no longer exists"); return; }
            
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
                else if(PayloadLength == 127)
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
            //Set the order number for this packet, then place that at the start of the packet data
            NextOutgoingPacketNumber++;
            Packet.AddPacketOrderNumber(NextOutgoingPacketNumber);

            //Store this packet into the previous packets dictionary, maintain the dictionary to only store the last 25 packets sent to that client
            PreviousPackets.Add(NextOutgoingPacketNumber, Packet);
            if (PreviousPackets.Count > 150)
                PreviousPackets.Remove(NextOutgoingPacketNumber - 150);

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

        //Transmits a missing packet back to a client who requested it again
        public void SendMissingPacket(ClientConnection Client, int PacketNumber)
        {
            //First check that this missing packet is still stored in memory
            if(!PreviousPackets.ContainsKey(PacketNumber))
            {
                //Log an error showing this client has requested packets that are no longer being stored in memory and that their connection needs to be closed down
                MessageLog.Print("ERROR: Client requesting missing packet no longer in memory, closing down their connection.");

                //Kick this player from the server, letting them know why they have been kicked
                SystemPacketSender.SendKickedFromServer(Client.NetworkID, "Missed packets from server that are no longer kept in memory, desync unable to be fixed.");

                //Tell all the other clients to remove this character from their game worlds
                List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(Client.NetworkID);
                foreach (ClientConnection OtherClient in OtherClients)
                    PlayerManagementPacketSender.SendRemoveRemotePlayer(OtherClient.NetworkID, Client.Character.Name);

                //Set the client as dead so they are cleaned up and have their data backed up into the database correctly and exit the function
                Client.ClientDead = true;
                return;
            }

            //Otherwise, we fetch the missing packet from the dictionary
            NetworkPacket MissingPacket = PreviousPackets[PacketNumber];

            //Frame the data, convert to binary array, and again to string to get the final packet size
            byte[] PacketData = GetFrameFromString(MissingPacket.PacketData);
            string PacketString = Encoding.UTF8.GetString(PacketData);

            //Then send the missing packet to the client, making sure connection is still open
            try { DataStream.BeginWrite(PacketData, 0, PacketString.Length, null, null); }
            catch(IOException Exception)
            {
                MessageLog.Error(Exception, "Error resending missing packet to dead client connection, marking them for cleanup.");
                ClientDead = true;
            }
        }

        //Any packet with order number -1 has its order number ignored by clients and is processed immediately
        public void SendPacketImmediately(NetworkPacket Packet)
        {
            //First add the -1 order number into the packet data
            Packet.AddPacketOrderNumber(-1);

            //Frame and convert the data into bytes, then into string for reading the payload size properly
            byte[] PacketData = GetFrameFromString(Packet.PacketData);
            string PacketString = Encoding.UTF8.GetString(PacketData);

            //Send the packet to the client, display an error if it couldnt be done
            try { DataStream.BeginWrite(PacketData, 0, PacketString.Length, null, null); }
            catch (IOException Exception) { MessageLog.Error(Exception, "Error trying to immediately send a packet to a client, their connection is no longer active."); }
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