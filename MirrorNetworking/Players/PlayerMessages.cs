// Created by Vojtech Bruza
using Mirror;

namespace UVRN.Player
{
    public struct PlayerCreationRequestMessage : NetworkMessage
    {
        public PlayerProfile profile;
    }

    // Sent to the player who asked for joining when there is something wrong (password mismatch...)
    public struct PlayerCreationFailedResponseMessage : NetworkMessage
    {
        public string errorText;
    }

    // Sent to notify the server about player interactions (muting...)
    public struct PlayerInteractionMessage : NetworkMessage
    {
        // player interatcting
        public uint interactorID;
        // player interacted with
        public uint interacteeID;
        public string interaction;
    }

    // Sent to notify the server about player actions
    public struct PlayerActionMessage : NetworkMessage
    {
        // player interatcting
        public uint playerID;
        public string action;
    }

    // Sent to notify the server that the player is leaving
    public struct PlayerLeavingMessage : NetworkMessage { }

    // Notify all clients which player left
    public struct PlayerLeftMessage : NetworkMessage
    {
        public int playerID;
    }

    public enum Role
    {
        None,
        Host,
        Cohost,
        Participant
    }
}