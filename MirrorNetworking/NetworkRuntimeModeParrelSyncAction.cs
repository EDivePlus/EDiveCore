// Author: František Holubec
// Created: 22.03.2025

using System;
using EDIVE.External.ParrelSync;
using UnityEngine;

namespace EDIVE.MirrorNetworking
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
