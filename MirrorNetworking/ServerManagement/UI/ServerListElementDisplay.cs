// Author: František Holubec
// Created: 13.06.2025

using EDIVE.Core;
using EnhancedUI.EnhancedScroller;
using LightReflectiveMirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MirrorNetworking.ServerManagement.UI
{
    public class ServerListElementDisplay : EnhancedScrollerCellView
    {
        [SerializeField]
        private TMP_Text _ServerNameText;

        [SerializeField]
        private TMP_Text _ServerIdText;

        [SerializeField]
        private TMP_Text _ServerDataText;

        [SerializeField]
        private TMP_Text _ClientsCountText;

        [SerializeField]
        private Button _ConnectButton;

        private Room _data;

        public void SetRoom(Room data)
        {
            _data = data;

            if (_ServerNameText)
                _ServerNameText.text = data.serverName;

            if (_ServerIdText)
                _ServerIdText.text = data.serverId;

            if (_ServerDataText)
                _ServerDataText.text = data.serverData;

            if (_ClientsCountText)
                _ClientsCountText.text = $"{data.currentPlayers}/{data.maxPlayers}";

            if (_ConnectButton)
            {
                _ConnectButton.onClick.RemoveAllListeners();
                _ConnectButton.onClick.AddListener(OnConnectClicked);
            }
        }

        private void OnConnectClicked()
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            networkManager.networkAddress = _data.serverId;
            networkManager.StartRuntime(NetworkRuntimeMode.Client);
        }
    }
}
