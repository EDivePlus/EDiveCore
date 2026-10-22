// Author: František Holubec
// Created: 21.10.2026

using System;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Players
{
    // Who a player is in ServiceHub and what they share
    [Serializable]
    public class StreamPlayerState : IPackedAuto
    {
        [SerializeField]
        private string _Identity;

        [SerializeField]
        private string _Source;

        [SerializeField]
        private bool _Open;

        // User or guest ID, used to ask for their view
        public string Identity => _Identity;
        public string Source => _Source;
        public bool Open => _Open;
        public bool IsSharing => !string.IsNullOrEmpty(_Source);

        public static StreamPlayerState Empty => new(null, null, false);

        public StreamPlayerState(string identity, string source, bool open)
        {
            _Identity = string.IsNullOrEmpty(identity) ? null : identity;
            _Source = string.IsNullOrEmpty(source) ? null : source;
            _Open = open;
        }

        public bool Matches(StreamPlayerState other) => other != null && _Identity == other._Identity && _Source == other._Source && _Open == other._Open;
    }
}
