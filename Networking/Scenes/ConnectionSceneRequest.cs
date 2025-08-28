// Author: František Holubec
// Created: 28.08.2025

using FishNet.Broadcast;

namespace EDIVE.Networking.Scenes
{
    public struct ConnectionSceneRequest : IBroadcast
    {
        public readonly string SceneName;
        public readonly ConnectionSceneRequestOperation Operation; 
        
        public ConnectionSceneRequest(string sceneName, ConnectionSceneRequestOperation operation)
        {
            SceneName = sceneName;
            Operation = operation;
        }
    }

    public enum ConnectionSceneRequestOperation
    {
        Load,
        Unload
    }
}
