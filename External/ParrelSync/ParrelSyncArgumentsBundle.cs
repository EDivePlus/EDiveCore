// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.External.ParrelSync
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct ParrelSyncArgumentsBundle : IEquatable<ParrelSyncArgumentsBundle>
    {
        [JsonProperty("ID")]
        [SerializeField]
        private string _ID;

        [JsonProperty("MasterPlaying")]
        [SerializeField]
        private bool _IsMasterPlaying;

        public string ID => _ID;
        public bool IsMasterPlaying
        {
            get => _IsMasterPlaying;
            set => _IsMasterPlaying = value;
        }

        public bool Equals(ParrelSyncArgumentsBundle other)
        {
            return _ID == other._ID && _IsMasterPlaying == other._IsMasterPlaying;
        }

        public override bool Equals(object obj)
        {
            return obj is ParrelSyncArgumentsBundle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_ID, _IsMasterPlaying);
        }
    }
}
#endif
