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
        /// <summary>
        /// Handles a client players updated character values to be shared all across the network
        /// </summary>
        /// <param name="ClientID">NetworkID of the target client</param>
        /// <param name="Packet">Packet containing the information were after</param>
        public static void HandlePlayerCharacterUpdate(int ClientID, ref NetworkPacket Packet)
        {
            //Log what we are doing here
            CommunicationLog.LogIn(ClientID + " Player Character Update");

            //Extract all the values from the packet data
            Vector3 Position = Packet.ReadVector3();
            Vector3 Movement = Packet.ReadVector3();
            Quaternion Rotation = Packet.ReadQuaternion();

            //Try getting the ClientConnection object who sent this packet to us
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);

            //Display an erro and exit from the function if they couldnt be found
            if(Client == null)
            {
                MessageLog.Print("ERROR: " + ClientID + " ClientConnection object couldnt be found, Player Character Update could not be performed.");
                return;
            }

            //Update the values in the ClientConnection object
            Client.CharacterPosition = Position;
            Client.CharacterMovement = Movement;
            Client.CharacterRotation = Rotation;

            //Set its NewPosition flag so it gets updated in the physics scene
            Client.NewPosition = true;

            //Share these new values to all the other clients in the game right now
            List<ClientConnection> OtherClients = ClientSubsetFinder.GetInGameClientsExceptFor(ClientID);
            foreach (ClientConnection OtherClient in OtherClients)
                PlayerManagementPacketSender.SendUpdateRemotePlayer(OtherClient.NetworkID, Client.CharacterName, Position, Movement, Rotation);
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
            ClientConnection Client = ConnectionManager.GetClientConnection(ClientID);

            //Display an error and exit from the function if they couldnt be found
            if(Client == null)
            {
                MessageLog.Print("ERROR: " + ClientID + " ClientConnection object couldnt be found, Player Camera Update could not be performed.");
                return;
            }

            //Store them in the ClientConnection object
            Client.CameraZoom = Zoom;
            Client.CameraXRotation = XRotation;
            Client.CameraYRotation = YRotation;
        }
    }
}