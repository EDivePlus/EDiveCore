// Author: František Holubec
// Created: 15.04.2025

using Mirror;

namespace EDIVE.MirrorNetworking.Scenes
{
    public struct LoadSceneMessage : NetworkMessage
    {
        public string SceneID;

        public LoadSceneMessage(string sceneId)
        {
            SceneID = sceneId;
        }
    }
}
