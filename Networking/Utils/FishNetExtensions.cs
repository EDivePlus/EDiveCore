// Author: František Holubec
// Created: 15.08.2025

#if FISHNET
using System;
using FishNet.Connection;

namespace EDIVE.Networking.Utils
{
    public static class FishNetExtensions
    {
        public static void WhenLoadedStartScenes(this NetworkConnection connection, bool asServer, Action callback = null)
        {
            if (!connection.LoadedStartScenes(asServer))
            {
                connection.OnLoadedStartScenes += OnConnectionOnOnLoadedStartScenes;
                return;
                
                void OnConnectionOnOnLoadedStartScenes(NetworkConnection networkConnection, bool server)
                {
                    if (asServer != server)
                        return;
                    
                    callback?.Invoke();
                    connection.OnLoadedStartScenes -= OnConnectionOnOnLoadedStartScenes;
                }
            }
            callback?.Invoke();
        }
    }
}
#endif
