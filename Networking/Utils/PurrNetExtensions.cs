// Author: František Holubec
// Created: 12.05.2026

using PurrNet;
using PurrNet.Transports;

namespace EDIVE.Networking.Utils
{
    public static class PurrNetExtensions
    {
        public static bool TryGetCurrentTransport<T>(this NetworkManager networkManager, out T transport) where T : GenericTransport
        {
            if (networkManager.transport is T direct)
            {
                transport = direct;
                return true;
            }

            if (networkManager.transport is CompositeTransport composite)
                return composite.TryGetTransport(out transport);

            transport = null;
            return false;
        }
    }
}
