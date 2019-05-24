// ================================================================================================================================
// File:        ClientConnection.cs
// Description: Handles a network connection between the server and a single game client, helps transfer packets between the two and
//              keeps track of what account and character is being used by that game client
// ================================================================================================================================

using System;
using System.Net;
using System.Net.Sockets;
using BepuPhysics;
using System.Numerics;
using Server.Interface;

namespace Server.Networking
{
    public class ClientConnection
    {
        public int NetworkID;  //Each currently connected client has a unique ID number (their IP address)
        public bool InGame = false; //Tracks if this client is active in the game world or not
        public string AccountName;  //The name of the account this client is currently logged into
        public string CharacterName;    //The name of the character this client is currently playing with
        public Vector3 CharacterPosition;   //Where this clients currently active game character is currently located in the game world
        public TcpClient NetworkConnection; //The current TCP connection between the server and this client
        public NetworkStream DataStream;    //Current datastream for sending information back and forth between this client and the game server
        public byte[] DataBuffer;   //Buffer where data is written from the data stream until the current stream has finished writing its data

        //Physics simulation variables
        public int BodyHandle = -1;
        public BodyDescription PhysicsBody;

        //default constructor
        public ClientConnection(TcpClient NewConnection)
        {
            //Save the newly received client connection, assign it an ID number and set up the data stream between the two
            NetworkConnection = NewConnection;
            NetworkID = ((IPEndPoint)NetworkConnection.Client.RemoteEndPoint).Port;

            //TODO: setting NetworkID to just the port number? this may end up with duplicated network ID's
            //Console.WriteLine("New client assigned network ID: " + NetworkID + "\nThis is unique right? If not, recode ClientConnection constructor to assign a real unique network ID value.");

            NetworkConnection.SendBufferSize = 4096;
            NetworkConnection.ReceiveBufferSize = 4096;
            DataStream = NetworkConnection.GetStream();
            DataBuffer = new byte[4096];
            //data stream is set up, start listening for information sent from client
            DataStream.BeginRead(DataBuffer, 0, 4096, ReadPacket, null);
        }

        //Once datastream finishes transmitting a packet, copies the data out to a new array and processes it, performing whatever game logic needs to be addressed
        private void ReadPacket(IAsyncResult Result)
        {
            //Read in the size of the packet that was sent to us from the client
            //Check for IO exceptions while reading the packet size incase the network connection has been lost
            int PacketSize = 0;
            try { PacketSize = DataStream.EndRead(Result); }
            catch(System.IO.IOException)
            {
                Log.PrintDebugMessage("Networking.ClientConnection readpacket io exception, closing client connection");
                ConnectionManager.CloseConnection(this);
                return;
            }
            //Also need to make sure packet size isnt 0, this means the client shut down the network connection on their end, triggered an end to the datastream sending us here
            if(PacketSize == 0)
            {
                Log.PrintDebugMessage("Networking.ClientConnection ReadPacket PacketSize == 0, client shut down connection from their end, closing connection");
                ConnectionManager.CloseConnection(this);
                return;
            }

            //Copy the data from the ASyncBuffer over to a new array so it can be handled
            byte[] PacketBuffer = new byte[PacketSize];
            Array.Copy(DataBuffer, PacketBuffer, PacketSize);
            PacketReceiver.ReadClientPacket(NetworkID, PacketBuffer);

            //we can immediately start using the ASyncBuffer to start reading in data again
            DataStream.BeginRead(DataBuffer, 0, NetworkConnection.ReceiveBufferSize, ReadPacket, null);
        }
    }
}
