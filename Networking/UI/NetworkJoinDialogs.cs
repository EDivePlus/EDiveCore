// Author: František Holubec
// Created: 28.10.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows;
using EDIVE.View.Windows.Dialog;
using EDIVE.View.Windows.Screens;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    // Popups for joins from the server list that failed
    public class NetworkJoinDialogs : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private WindowDefinition _DialogWindow;

        [SerializeField]
        [Required]
        [Tooltip("Confirm button retries")]
        private DialogSetup _ConnectFailedDialog;

        [SerializeField]
        [Required]
        private DialogSetup _ServerFullDialog;

        [SerializeField]
        [Required]
        private DialogSetup _VersionMismatchDialog;

        private NetworkServerManager _serverManager;
        private bool _isOpen;

        private void OnEnable()
        {
            _serverManager = AppCore.Services.Get<NetworkServerManager>();
            if (_serverManager) _serverManager.JoinFailed += OnJoinFailed;
        }

        private void OnDisable()
        {
            if (_serverManager) _serverManager.JoinFailed -= OnJoinFailed;
        }

        private void OnJoinFailed(ServerRecord server, JoinError error)
        {
            var dialog = error switch
            {
                JoinError.ServerFull => _ServerFullDialog,
                JoinError.VersionMismatch => _VersionMismatchDialog,
                _ => _ConnectFailedDialog
            };
            ShowAsync(server, dialog).Forget();
        }

        private async UniTaskVoid ShowAsync(ServerRecord server, DialogSetup dialog)
        {
            if (_isOpen || dialog == null)
                return;
            var screen = this.FindInViewTree<ScreenRoot>();
            if (screen == null)
                return;

            _isOpen = true;
            var result = await screen.Open(_DialogWindow, dialog).Closed;
            _isOpen = false;
            if (result is true && dialog == _ConnectFailedDialog && this)
                _serverManager.ConnectToServer(server);
        }
    }
}
