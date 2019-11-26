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
    CharacterPositionUpdate = 8,
    CharacterRotationUpdate = 9,
    CharacterMovementUpdate = 10,
    CharacterCameraUpdate = 11,

    //System Packet Types
    MissedPacketsRequest = 12,
    StillConnectedReply = 13,
    OutOfSyncAlert = 14
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
    PlayerPositionUpdate = 12,
    PlayerRotationUpdate = 13,
    PlayerMovementUpdate = 14,
    SpawnOtherPlayer = 15,
    RemoveOtherPlayer = 16,
    PlayerBegin = 17,
    ForceMovePlayer = 18,
    ForceMoveOtherPlayer = 19,

    //System Packet Types
    StillConnectedCheck = 20,
    MissingPacketsRequest = 21,
    KickedFromServer = 22
};