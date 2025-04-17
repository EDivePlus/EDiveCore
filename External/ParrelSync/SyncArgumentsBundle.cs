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
    public class SyncArgumentsBundle
    {
        [JsonProperty("Name")]
        [SerializeField]
        private string _Name = "Clone";

        [JsonProperty("SyncPlay")]
        [SerializeField]
        private bool _SyncPlay= true;

        [JsonProperty("SyncStop")]
        [SerializeField]
        private bool _SyncStop = true;

        [JsonProperty("MasterPlaying")]
        [HideInInspector]
        [SerializeField]
        private bool _IsMasterPlaying;
        
        [JsonProperty("Actions")]
        [SerializeReference]
        private List<IParrelSyncAction> _Actions = new();
        
        public IReadOnlyList<IParrelSyncAction> Actions => _Actions;
        public bool IsMasterPlaying
        {
            get => _IsMasterPlaying;
            set => _IsMasterPlaying = value;
        }

        public bool SyncPlay
        {
            get => _SyncPlay;
            set => _SyncPlay = value;
        }

        public bool SyncStop
        {
            get => _SyncStop;
            set => _SyncStop = value;
        }

        public string Name
        {
            get => _Name;
            set => _Name = value;
        }
    }
}
#endif
