// Created by Vojtech Bruza

using FishNet.Broadcast;

namespace EDIVE.Networking.Players
{
    public struct PlayerCreationRequestMessage : IBroadcast
    {
        public PlayerProfile profile;
    }

    // Sent to the player who asked for joining when there is something wrong (password mismatch...)
    public struct PlayerCreationFailedResponseMessage : IBroadcast
    {
        public string errorText;
    }

    // Sent to notify the server that the player is leaving
    public struct PlayerLeavingMessage : IBroadcast { }

    // Notify all clients which player left
    public struct PlayerLeftMessage : IBroadcast
    {
        public int playerID;
    }
}