// Author: František Holubec
// Created: 15.08.2025

#if FISHNET
using System;
using FishNet.Connection;

namespace EDIVE.Networking.Utils
{
    public static class FishNetExtensions
    {
        public static void WhenLoadedStartScenes(this NetworkConnection connection, Action callback = null)
        {
            if (!connection.LoadedStartScenes())
            {
                connection.OnLoadedStartScenes += OnConnectionOnOnLoadedStartScenes;
                return;
                
                void OnConnectionOnOnLoadedStartScenes(NetworkConnection networkConnection, bool asServer)
                {
                    callback?.Invoke();
                    connection.OnLoadedStartScenes -= OnConnectionOnOnLoadedStartScenes;
                }
            }
            callback?.Invoke();
        }
    }
}
#endif
