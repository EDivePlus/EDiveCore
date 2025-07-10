// Author: František Holubec
// Created: 10.07.2025

using System.Linq;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    public class UniversalTransport : MiddlewareTransport
    {
        [SerializeField]
        private Transport _ServerTransport;

        [SerializeField]
        private Transport _RelayTransport;

        [SerializeField]
        private Transport _DirectTransport;

        public void ResolveTransport(NetworkRuntimeMode mode, string address)
        {
            if (mode == NetworkRuntimeMode.Client)
            {
                if (address.Length == 5 && address.All(char.IsLetterOrDigit))
                {
                    inner = _RelayTransport;
                }
                else
                {
                    inner = _DirectTransport;
                }
            }
            else
            {
                inner = _ServerTransport;
            }
        }
    }
}
