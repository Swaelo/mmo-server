using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using Server.Interface;
using Server.Networking.PacketSenders;

namespace Server.Networking.PacketHandlers
{
    public static class PlayerManagementPacketHandler
    {
        //Recieves a players updated position/rotation values
        public static void HandlePlayerUpdate(int ClientID, byte[] PacketData)
        {
            //Log.IncomingPacketsWindow.DisplayNewMessage(ClientID + ": PlayerManagement.CharacterUpdate");

            //Extract the updated player information from the network packet
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            string CharacterName = Reader.ReadString();
            Vector3 CharacterPosition = Maths.VectorTranslate.ConvertVector(Reader.ReadVector3());
            Quaternion CharacterRotation = Reader.ReadQuaternion();

            //Update this players position in the server
            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            //if (Client.ServerCollider != null)
            //    Client.ServerCollider.Position = CharacterPosition;
            //Client.CharacterPosition = CharacterPosition;

            ////Share this updated data out to all the other connected clients
            //List<ClientConnection> OtherClients = ConnectionManager.GetActiveClientsExceptFor(ClientID);
            //SendListPlayerUpdate(OtherClients, CharacterName, CharacterPosition, CharacterRotation);
        }

        //Receives the location where a players attack landed in the world
        public static void HandlePlayerAttack(int ClientID, byte[] PacketData)
        {
            Log.IncomingPacketsWindow.DisplayNewMessage(ClientID + ": PlayerManagement.CharacterAttack");

            //Figure out where the players attack landed
            PacketReader Reader = new PacketReader(PacketData);
            int PacketType = Reader.ReadInt();
            Vector3 AttackPosition = Reader.ReadVector3();
            //Any positions read in from unity need to be converted as the axis directions are different in the BEPU physics engine
            AttackPosition = Maths.VectorTranslate.ConvertVector(AttackPosition);
            Vector3 AttackScale = Reader.ReadVector3();
            Quaternion AttackRotation = Reader.ReadQuaternion();
            //Pass the information about this attack on to the entity manager so it can process which enemies the attack hit
            Entities.EntityManager.HandlePlayerAttack(AttackPosition, AttackScale, AttackRotation);
        }

        //Removes a player from the game once they have stopped playing
        public static void HandlePlayerDisconnect(int ClientID, byte[] PacketData)
        {
            Log.IncomingPacketsWindow.DisplayNewMessage(ClientID + ": PlayerManagement.PlayerDisconnectionNotice");

            ClientConnection Client = ConnectionManager.ActiveConnections[ClientID];
            Entities.EntityManager.HandleClientDisconnect(Client);
            Networking.ConnectionManager.HandleClientDisconnect(Client);
        }
    }
}
