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
    LocalPlayerCharacterUpdate = 9,
    LocalPlayerCameraUpdate = 10,
    LocalPlayerPlayAnimationAlert = 11,

    //System Packet Types
    MissedPacketsRequest = 12,
    StillConnectedReply = 13,

    //Combat
    PlayerAttackAlert = 14,
    PlayerRespawnRequest = 15
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
    RemotePlayerPlayAnimationAlert = 16,

    //System Packet Types
    StillConnectedCheck = 17,
    MissingPacketsRequest = 18,
    KickedFromServer = 19,

    //Combat Packet Types
    LocalPlayerTakeHit = 20,
    RemotePlayerTakeHit = 21,
    LocalPlayerDead = 22,
    RemotePlayerDead = 23,
    LocalPlayerRespawn = 24,
    RemotePlayerRespawn = 25
};