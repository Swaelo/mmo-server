// ================================================================================================================================
// File:        PacketTypes.cs
// Description: Defines all the different types of packets which can be sent over the network
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

namespace Server.Networking
{
    public enum ClientPacketType
    {
        AccountRegistrationRequest = 1,
        AccountLoginRequest = 2,
        CharacterCreationRequest = 3,
        CharacterDataRequest = 4,
        EnterWorldRequest = 5,
        ActiveEntityRequest = 6,
        ActiveItemRequest = 7,
        NewPlayerReady = 8,
        PlayerChatMessage = 9,
        PlayerUpdate = 10,
        PlayerAttack = 11,
        DisconnectionNotice = 12,

        PlayerInventoryRequest = 13,
        PlayerEquipmentRequest = 14,
        PlayerActionBarRequest = 15,
        PlayerTakeItemRequest = 16,
        RemoveInventoryItem = 17,
        EquipInventoryItem = 18,
        UnequipItem = 19,

        PlayerMoveInventoryItem = 20,
        PlayerSwapInventoryItems = 21,
        PlayerSwapEquipmentItem = 22,
        PlayerDropItem = 23,
        PlayerEquipAbility = 24,
        PlayerSwapEquipAbility = 25,
        PlayerUnequipAbility = 26,
        PlayerSwapAbilities = 27,
        PlayerMoveAbility = 28,
        PlayerDropAbility = 29,

        AccountLogoutAlert = 30,
        StillAlive = 31
    };

    public enum ServerPacketType
    {
        AccountRegistrationReply = 1,
        AccountLoginReply = 2,
        CharacterCreationReply = 3,
        CharacterDataReply = 4,

        ActivePlayerList = 5,
        ActiveEntityList = 6,
        ActiveItemList = 7,
        SpawnItem = 8,
        RemoveItem = 9,

        EntityUpdates = 10,
        RemoveEntities = 11,

        PlayerChatMessage = 12,
        PlayerUpdate = 13,
        SpawnPlayer = 14,
        RemovePlayer = 15,

        PlayerInventoryItems = 16,
        PlayerEquipmentItems = 17,
        PlayerActionBarAbilities = 18,
        PlayerTotalItemUpdate = 19
    }
}
