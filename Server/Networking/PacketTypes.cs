// ================================================================================================================================
// File:        PacketTypes.cs
// Description: Defines all the different types of packets which can be sent over the network
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

public enum ClientPacketType
{
    //Account Management Packet Types
    AccountLoginRequest = 1,
    AccountRegistrationRequest = 2,
    CharacterDataRequest = 3,
    CharacterCreationRequest = 4,

    //Game World State Packet Types
    EnterWorldRequest = 5,
    PlayerReadyAlert = 6,

    //Player Communication Packet Types
    PlayerChatMessage = 7,

    //Player Management Packet Types
    LocalPlayerCharacterUpdate = 8,
    LocalPlayerCameraUpdate = 9,

    //System Packet Types
    MissedPacketsRequest = 10,
    StillConnectedReply = 11,
    OutOfSyncAlert = 12
};

public enum ServerPacketType
{
    //Account Management Packet Types
    AccountLoginReply = 1,
    AccountRegistrationReply = 2,
    CharacterDataReply = 3,
    CreateCharacterReply = 4,

    //Game World State Packet Types
    ActivePlayerList = 5,
    ActiveEntityList = 6,
    ActiveItemList = 7,
    InventoryContents = 8,
    EquippedItems = 9,
    SocketedAbilities = 10,

    //Player Communication Packet Types
    PlayerChatMessage = 11,

    //Player Management Packet Types
    UpdateRemotePlayer = 12,
    AddRemotePlayer = 13,
    RemoveRemotePlayer = 14,
    AllowPlayerBegin = 15,

    //System Packet Types
    StillConnectedCheck = 16,
    MissingPacketsRequest = 17,
    KickedFromServer = 18
};