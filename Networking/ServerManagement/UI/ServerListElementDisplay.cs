// Author: František Holubec
// Created: 13.06.2025

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EnhancedUI.EnhancedScroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Networking.ServerManagement.UI
{
    public class ServerListElementDisplay : EnhancedScrollerCellView
    {
        [SerializeField]
        private TMP_Text _ServerNameText;

        [SerializeField]
        private TMP_Text _ServerIdText;
        
        [SerializeField]
        private TMP_Text _ClientsCountText;

        [SerializeField]
        private Button _ConnectButton;

        private AServerRecord _serverRecord;

        public void SetRoom(AServerRecord serverRecord)
        {
            _serverRecord = serverRecord;

            if (_ServerNameText)
                _ServerNameText.text = _serverRecord.ServerName;

            if (_ServerIdText)
                _ServerIdText.text = $"{_serverRecord.ServerID}";
            
            if (_ClientsCountText)
                _ClientsCountText.text = $"{_serverRecord.CurrentPlayers}/{_serverRecord.MaxPlayers}";

            if (_ConnectButton)
            {
                _ConnectButton.onClick.RemoveAllListeners();
                _ConnectButton.onClick.AddListener(OnConnectClicked);
            }
        }

        private void OnConnectClicked()
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            var serverManager = AppCore.Services.Get<NetworkServerManager>();
            UniTask.Void(async () =>
            {
                serverManager.CurrentServer = _serverRecord;
                var success = await _serverRecord.PrepareForConnect();
                if (!success)
                {
                    serverManager.CurrentServer = null;
                    return;
                }

                networkManager.StartRuntime(NetworkRuntimeMode.Client);
            });
        }
    }
}
