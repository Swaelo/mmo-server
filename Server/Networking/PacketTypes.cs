// ================================================================================================================================
// File:        PacketTypes.cs
// Description: Defines all the different types of packets which can be sent over the network
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

public enum ClientPacketType
{
    //Account Management Packet Types
    AccountLoginRequest = 1,
    AccountLogoutAlert = 2,
    AccountRegistrationRequest = 3,
    CharacterDataRequest = 4,
    CharacterCreationRequest = 5,

    //Game World State Packet Types
    EnterWorldRequest = 6,
    PlayerReadyAlert = 7,

    //Player Communication Packet Types
    PlayerChatMessage = 8,

    //Player Management Packet Types
    PlayerPositionUpdate = 9,
    PlayerRotationUpdate = 10,
    PlayerCameraUpdate = 11,
    PlayAnimationAlert = 12,

    //System Packet Types
    MissedPacketsRequest = 13,
    StillConnectedReply = 14,

    //Combat
    PlayerAttackAlert = 15,
    PlayerRespawnRequest = 16
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
    AddPlayer = 14,
    RemovePlayer = 15,
    AllowPlayerBegin = 16,
    PlayAnimationAlert = 17,

    //System Packet Types
    StillConnectedCheck = 18,
    MissingPacketsRequest = 19,
    KickedFromServer = 20,
    UIMessage = 21,

    //Combat Packet Types
    LocalPlayerTakeHit = 22,
    RemotePlayerTakeHit = 23,
    LocalPlayerDead = 24,
    RemotePlayerDead = 25,
    LocalPlayerRespawn = 26,
    RemotePlayerRespawn = 27
};