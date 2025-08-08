// Author: František Holubec
// Created: 22.03.2025

#if UNITY_EDITOR
using System;
using EDIVE.External.ParrelSync;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    [Serializable]
    public class NetworkRuntimeModeParrelSyncAction : IParrelSyncAction
    {
        [SerializeField]
        private NetworkRuntimeMode _RuntimeMode;

        public void OnPlayModeStarted()
        {
            NetworkUtils.EditorRuntimeMode = _RuntimeMode;
        }
    }
}
#endif
