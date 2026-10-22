// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.ServiceHub.Lobby;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    // Players for the Lobby update, guests get a pass from ServiceHub
    internal class ServiceHubSessionPlayers : IDisposable
    {
        private readonly LobbyService _lobby;
        private readonly Action _changed;
        private readonly NetworkPlayerManager _playerManager;
        private readonly CancellationTokenSource _cancellation = new();

        private readonly Dictionary<NetworkPlayerController, Action<NetworkUserInfo>> _infoHandlers = new();
        private readonly Dictionary<NetworkPlayerController, Action<string>> _deviceHandlers = new();
        private readonly Dictionary<NetworkPlayerController, string> _guestIds = new();
        private readonly HashSet<NetworkPlayerController> _requesting = new();

        private string _secret;

        public ServiceHubSessionPlayers(LobbyService lobby, Action changed)
        {
            _lobby = lobby;
            _changed = changed;

            if (!AppCore.Services.TryGet(out _playerManager))
                return;

            _playerManager.PlayerRegistered += Track;
            _playerManager.PlayerUnregistered += Untrack;
            foreach (var player in _playerManager.CurrentPlayers)
                Track(player);
        }

        // New registration is a new session, old passes are gone
        public void SetSecret(string secret)
        {
            if (_secret == secret)
                return;

            _secret = secret;
            _guestIds.Clear();
            if (_playerManager == null)
                return;
            foreach (var player in _playerManager.CurrentPlayers)
                Refresh(player);
        }

        public List<SessionPlayer> BuildList()
        {
            var list = new List<SessionPlayer>();
            if (_playerManager == null)
                return list;

            foreach (var player in _playerManager.CurrentPlayers)
            {
                var info = player.AuthUserInfo;
                if (info == null)
                    continue;

                if (!info.IsGuest)
                {
                    list.Add(new SessionPlayer { UserId = info.Id, Device = player.Device });
                }
                else if (_guestIds.TryGetValue(player, out var guestId))
                {
                    list.Add(new SessionPlayer { GuestId = guestId, Name = info.Name, Device = player.Device });
                }
                else
                {
                    // Retry a failed pass with every update
                    RequestPass(player).Forget();
                }
            }
            return list;
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            if (_playerManager == null)
                return;

            _playerManager.PlayerRegistered -= Track;
            _playerManager.PlayerUnregistered -= Untrack;
            foreach (var player in new List<NetworkPlayerController>(_infoHandlers.Keys))
                Untrack(player);
        }

        private void Track(NetworkPlayerController player)
        {
            if (_infoHandlers.ContainsKey(player))
                return;

            Action<NetworkUserInfo> infoHandler = _ => Refresh(player);
            Action<string> deviceHandler = _ => _changed();
            _infoHandlers[player] = infoHandler;
            _deviceHandlers[player] = deviceHandler;
            player.AuthUserInfoChanged += infoHandler;
            player.DeviceChanged += deviceHandler;
            Refresh(player);
        }

        private void Untrack(NetworkPlayerController player)
        {
            if (_infoHandlers.Remove(player, out var infoHandler))
                player.AuthUserInfoChanged -= infoHandler;
            if (_deviceHandlers.Remove(player, out var deviceHandler))
                player.DeviceChanged -= deviceHandler;
            _guestIds.Remove(player);
            _changed();
        }

        private void Refresh(NetworkPlayerController player)
        {
            var info = player.AuthUserInfo;
            if (info is { IsGuest: true })
            {
                if (!_guestIds.ContainsKey(player))
                    RequestPass(player).Forget();
            }
            else
            {
                // Signed in meanwhile, lists the user instead
                _guestIds.Remove(player);
            }
            _changed();
        }

        private async UniTaskVoid RequestPass(NetworkPlayerController player)
        {
            var secret = _secret;
            if (string.IsNullOrEmpty(secret) || !_requesting.Add(player))
                return;

            try
            {
                var response = await _lobby.RequestGuestPassAsync(secret, player.AuthUserInfo?.Name, _cancellation.Token);
                if (!response.IsSuccess || response.Result == null)
                {
                    Debug.LogWarning($"[ServiceHubSessionPlayers] Guest pass failed: {response.ErrorMessage}");
                    return;
                }

                // Left, signed in or new session meanwhile
                if (secret != _secret || !_infoHandlers.ContainsKey(player) || player.AuthUserInfo is not { IsGuest: true })
                    return;

                _guestIds[player] = response.Result.GuestId;
                player.SendGuestPass(response.Result.Pass);
                _changed();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _requesting.Remove(player);
            }
        }
    }
}
