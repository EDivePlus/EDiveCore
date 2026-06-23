// Author: František Holubec
// Created: 28.08.2025

using PurrNet.Packing;

namespace EDIVE.Networking.Scenes
{
    public struct ConnectionSceneRequest : IPackedAuto
    {
        public readonly SceneKey Scene;
        public readonly ConnectionSceneRequestOperation Operation;

        public ConnectionSceneRequest(SceneKey scene, ConnectionSceneRequestOperation operation)
        {
            Scene = scene;
            Operation = operation;
        }
    }

    public enum ConnectionSceneRequestOperation
    {
        Join,
        Leave
    }
}
