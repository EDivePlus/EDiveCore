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
                connection.OnLoadedStartScenes += (_, _) => callback?.Invoke();
                return;
            }
            callback?.Invoke();
        }
    }
}
#endif
