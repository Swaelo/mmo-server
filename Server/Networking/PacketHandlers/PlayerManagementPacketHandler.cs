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
        public static NetworkPacket GetValuesPlayerPositionUpdate(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayerPositionUpdate);
            Packet.WriteVector3(ReadFrom.ReadVector3());
            return Packet;
        }
        public static void HandlePlayerPositionUpdate(int ClientID, ref NetworkPacket Packet)
        {
            Vector3 Position = Packet.ReadVector3();
            ClientConnection Client = ConnectionManager.GetClient(ClientID);
            if(Client != null)
            {
                Client.Character.Position = Position;
                Client.Character.NewPosition = true;
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(ClientID))
                    PlayerManagementPacketSender.SendPlayerPositionUpdate(OtherClient.ClientID, Client.Character);
            }
        }

        public static NetworkPacket GetValuesPlayerRotationUpdate(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayerRotationUpdate);
            Packet.WriteQuaternion(ReadFrom.ReadQuaternion());
            return Packet;
        }
        public static void HandlePlayerRotationUpdate(int ClientID, ref NetworkPacket Packet)
        {
            Quaternion Rotation = Packet.ReadQuaternion();
            ClientConnection Client = ConnectionManager.GetClient(ClientID);
            if(Client != null)
            {
                Client.Character.Rotation = Rotation;
                Client.Character.NewRotation = true;
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(ClientID))
                    PlayerManagementPacketSender.SendPlayerRotationUpdate(OtherClient.ClientID, Client.Character);
            }
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesPlayerCameraUpdate(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayerCameraUpdate);
            Packet.WriteFloat(ReadFrom.ReadFloat());
            Packet.WriteFloat(ReadFrom.ReadFloat());
            Packet.WriteFloat(ReadFrom.ReadFloat());
            return Packet;
        }

        /// <summary>
        /// Handles a client players updated camera values to be stored in their ClientConnection, until
        /// they either leave or the server is being shutdown, to then be backed up into the database
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="Packet">Packet containing the information were after</param>
        public static void HandlePlayerCameraUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Player Camera Update");

            //Extract all the values from the packet data
            float Zoom = Packet.ReadFloat();
            float XRotation = Packet.ReadFloat();
            float YRotation = Packet.ReadFloat();

            //Try getting the ClientConnection object who sent this packet to us
            ClientConnection Client = ConnectionManager.GetClient(ClientID);

            //Display an error and exit from the function if they couldnt be found
            if(Client == null)
            {
                MessageLog.Print("ERROR: " + ClientID + " ClientConnection object couldnt be found, Player Camera Update could not be performed.");
                return;
            }

            //Store them in the ClientConnection object
            Client.Character.CameraZoom = Zoom;
            Client.Character.CameraXRotation = XRotation;
            Client.Character.CameraYRotation = YRotation;
        }

        //Retrives values for an account login request
        public static NetworkPacket GetValuesPlayAnimationAlert(NetworkPacket ReadFrom)
        {
            NetworkPacket Packet = new NetworkPacket();
            Packet.WriteType(ClientPacketType.PlayAnimationAlert);
            Packet.WriteString(ReadFrom.ReadString());
            return Packet;
        }

        public static void HandlePlayAnimationAlert(int ClientID, ref NetworkPacket Packet)
        {
            CommunicationLog.LogIn(ClientID + " Play Animation Alert");
            string AnimationName = Packet.ReadString();
            ClientConnection Client = ConnectionManager.GetClient(ClientID);
            if(Client == null)
            {
                MessageLog.Print("ERROR: Client not found, unable to handle play animation alert.");
                return;
            }
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendPlayAnimationAlert(OtherClient.ClientID, Client.Character.Name, AnimationName);
        }
    }
}