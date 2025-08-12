// Created by Vojtech Bruza

using FishNet.Broadcast;

namespace EDIVE.Networking.Players
{
    public struct PlayerCreationRequestMessage : IBroadcast
    {
        public PlayerProfile profile;
    }
}