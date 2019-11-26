// ================================================================================================================================
// File:        PlayerManagementPacketHandler.cs
// Description: Manages packets sent from game clients regarding the current state of the player characters
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Numerics;
using Server.Networking.PacketSenders;
using Server.Logging;
using Server.Enums;
using Quaternion = BepuUtilities.Quaternion;

namespace Server.Networking.PacketHandlers
{
    public class PlayerManagementPacketHandler
    {
        //Handles clients player characters giving us updated position/rotation/movement values
        public static void HandlePositionUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing
            CommunicationLog.LogIn(ClientID + " Position Update");
            //Extract values from the packet
            string CharacterName = Packet.ReadString();
            Vector3 CharacterPosition = Packet.ReadVector3();

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Update the values in the ClientConnection object
            ClientConnection.CharacterPosition = CharacterPosition;
            ClientConnection.NewPosition = true;
            //Share this clients new position values with all the other clients currently playing
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendPlayerPositionUpdate(OtherClient.NetworkID, CharacterName, CharacterPosition);
        }
        public static void HandleRotationUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing
            CommunicationLog.LogIn(ClientID + " Rotation Update");
            //Extract values from network packet
            string CharacterName = Packet.ReadString();
            Quaternion CharacterRotation = Packet.ReadQuaternion();

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Update values in the ClientConnection object
            ClientConnection.CharacterRotation = CharacterRotation;
            //Share this clients new rotation values with all the other clients currently playing
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendPlayerRotationUpdate(OtherClient.NetworkID, CharacterName, CharacterRotation);
        }
        public static void HandleMovementUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing
            CommunicationLog.LogIn(ClientID + " Movement Update");
            //Extract values from network packet
            string CharacterName = Packet.ReadString();
            Vector3 CharacterMovement = Packet.ReadVector3();

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Update values in the ClientConnection object
            ClientConnection.CharacterMovement = CharacterMovement;
            //Share this clients new movement values with all the other clients currently playing
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendPlayerMovementUpdate(OtherClient.NetworkID, CharacterName, CharacterMovement);
        }

        //Handles a clients player camera zoom / rotation values that we need to store back into the database
        public static void HandlePlayerCameraUpdate(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " camera update");

            //Extract all values from the packet data
            string CharacterName = Packet.ReadString();
            float CameraZoom = Packet.ReadFloat();
            float XRotation = Packet.ReadFloat();
            float YRotation = Packet.ReadFloat();

            //Get this ClientConnection and make sure we were able to find them
            ClientConnection ClientConnection = ConnectionManager.GetClientConnection(ClientID);
            if (ClientConnection == null)
            {
                MessageLog.Print("ERROR: Client not found.");
                return;
            }

            //Store the values within
            ClientConnection.CameraZoom = CameraZoom;
            ClientConnection.CameraXRotation = XRotation;
            ClientConnection.CameraYRotation = YRotation;
        }
    }
}