// Author: František Holubec
// Created: 14.03.2025

#if PARREL_SYNC
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.External.ParrelSync
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct ParrelSyncArgumentsBundle
    {
        [JsonProperty("SyncPlaymode")]
        [SerializeField]
        private bool _SyncPlaymode;

        [JsonProperty("MasterPlaying")]
        [HideInInspector]
        [SerializeField]
        private bool _IsMasterPlaying;
        
        [JsonProperty("Actions")]
        [SerializeReference]
        private List<IParrelSyncAction> _Actions;
        
        public IReadOnlyList<IParrelSyncAction> Actions => _Actions;
        public bool IsMasterPlaying
        {
            get => _IsMasterPlaying;
            set => _IsMasterPlaying = value;
        }

        public bool SyncPlaymode
        {
            get => _SyncPlaymode;
            set => _SyncPlaymode = value;
        }
    }
}
#endif
