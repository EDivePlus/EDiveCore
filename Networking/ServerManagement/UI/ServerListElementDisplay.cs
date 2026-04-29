// Author: František Holubec
// Created: 13.06.2025

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
        private ServerRecordDisplay _Display;
        
        [SerializeField]
        private TMP_Text _ServerNameText;

        [SerializeField]
        private TMP_Text _ServerIdText;

        [SerializeField]
        private TMP_Text _ClientsCountText;
        
        [SerializeField]
        private Button _ConnectButton;

        private ServerRecord _serverRecord;

        public void SetServer(ServerRecord serverRecord)
        {
            _serverRecord = serverRecord;
            _Display.Terminate();
            _Display.Initialize(serverRecord);

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
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                serverManager.ConnectToServer(_serverRecord);
            }
        }
    }
}
