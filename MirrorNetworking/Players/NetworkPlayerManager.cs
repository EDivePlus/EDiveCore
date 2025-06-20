using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.MirrorNetworking;
using EDIVE.MirrorNetworking.Players;
using EDIVE.MirrorNetworking.Scenes;
using EDIVE.NativeUtils;
using Edive.Networking;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UVRN.Player
{
    /// <summary>
    /// This class should manage all stuff related to the players and their connections establishing.
    /// But right now it is kind of overlapping with the functionallity in the UVRN_Player class
    /// so it could use a bit of refactor and cleanup.
    /// </summary>
    public class NetworkPlayerManager : ALoadableServiceBehaviour<NetworkPlayerManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private NetworkPlayerConfig _PlayerConfig;

        [ShowCreateNew]
        [SerializeField]
        private AvatarDefinition _DefaultAvatar;

        [ShowCreateNew]
        [SerializeField]
        private AWordGenerator _PlayerNameGenerator;

        public NetworkPlayerConfig PlayerConfig => _PlayerConfig;
        private NetworkSceneManager _networkSceneManager;
        private MasterNetworkManager _networkManager;

        public uint LocalPlayerID { get => NetworkClient.localPlayer.netId; }

        private PlayerProfile _playerProfile;
        public PlayerProfile PlayerProfile => _playerProfile ??= CreatePlayerProfile();

        public List<NetworkPlayerController> CurrentPlayers { get; } = new();
        private readonly Dictionary<int, UniTaskCompletionSource<NetworkPlayerController>> _controllerRequests = new();

        private const float CONTROLLER_REQUEST_TIMEOUT = 20f;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = AppCore.Services.Get<MasterNetworkManager>();
            _networkSceneManager = AppCore.Services.Get<NetworkSceneManager>();

            _networkManager.ServerStarted.AddListener(Server_OnStart);

            _networkManager.ClientStarted.AddListener(Client_OnStart);
            _networkSceneManager.ClientSceneChanged.AddListener(Client_OnSceneChanged);
            _networkManager.ClientStopped.AddListener(Client_OnStop);

            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
            dependencies.Add(typeof(NetworkSceneManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (AppCore.Services.TryGet(out _networkManager))
            {
                _networkManager.ServerStarted.RemoveListener(Server_OnStart);
                _networkManager.ClientStarted.RemoveListener(Client_OnStart);
                _networkManager.ClientStopped.RemoveListener(Client_OnStop);
            }

            if (AppCore.Services.TryGet(out _networkSceneManager))
            {
                _networkSceneManager.ClientSceneChanged.RemoveListener(Client_OnSceneChanged);
            }
        }

        public async UniTask<NetworkPlayerController> AwaitPlayerControllerWithConnectionID(int connectionId)
        {
            if (CurrentPlayers.TryGetFirst(c => c.ConnectionID == connectionId, out var player))
            {
                return player;
            }

            if (!_controllerRequests.TryGetValue(connectionId, out var request))
            {
                request = new UniTaskCompletionSource<NetworkPlayerController>();
                _controllerRequests[connectionId] = request;
            }

            try
            {
                return await request.Task.Timeout(TimeSpan.FromSeconds(CONTROLLER_REQUEST_TIMEOUT));
            }
            catch (TimeoutException)
            {
                _controllerRequests.Remove(connectionId);
            }
            return null;
        }

        public void RegisterPlayer(NetworkPlayerController player)
        {
            if (CurrentPlayers.Contains(player))
                return;

            player.AwaitConnectionID().ContinueWith(connectionId =>
            {
                if (_controllerRequests.TryGetValue(connectionId, out var request))
                {
                    request.TrySetResult(player);
                    _controllerRequests.Remove(connectionId);
                }
            }).Forget();

            CurrentPlayers.Add(player);
        }

        public void UnregisterPlayer(NetworkPlayerController player)
        {
            CurrentPlayers.Remove(player);
        }

        private PlayerProfile CreatePlayerProfile()
        {
            if (_playerProfile != null)
                return _playerProfile;

            _playerProfile = new PlayerProfile()
            {
                username = GeneratePlayerName(),
                password = "",
                role = "guest",
                color = Color.HSVToRGB(Random.Range(0f, 1f), .75f, .75f),
                avatarId = _DefaultAvatar.UniqueID,
            };
            return _playerProfile;
        }

        public string GeneratePlayerName()
        {
            return _PlayerNameGenerator ? _PlayerNameGenerator.Generate() : $"Player_{Random.Range(1000, 9999)}";
        }

        public void Server_OnStart()
        {
            NetworkServer.RegisterHandler<PlayerCreationRequestMessage>(Server_OnPlayerCreationRequest);
            NetworkServer.RegisterHandler<PlayerLeavingMessage>(Server_OnPlayerLeavingMessage);
        }

        public void Client_OnStart()
        {
            NetworkClient.RegisterHandler<PlayerCreationFailedResponseMessage>(Client_OnPlayerCreationFailedResponse);
            NetworkClient.RegisterHandler<PlayerLeftMessage>(Client_OnPlayerLeftMessage);
        }

        private void Server_OnPlayerCreationRequest(NetworkConnectionToClient conn, PlayerCreationRequestMessage request)
        {
            if (!ValidateProfile(request.profile))
            {
                Debug.LogError("Player does not have permission to join.");
                var invalidLoginResponse = new PlayerCreationFailedResponseMessage()
                {
                    errorText = "Invalid login credentials"
                };
                conn.Send(invalidLoginResponse);
                // TODO these messages do not get sent because the player is disconnected before it recieves the reason
                // the coroutine is just a temporary fix
                StartCoroutine(DisconnectAfter(conn, 1));
                return;
            }

            InstantiatePlayer(conn, request.profile);
        }

        private IEnumerator DisconnectAfter(NetworkConnection conn, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            conn.Disconnect();
        }

        private bool ValidateProfile(PlayerProfile profile)
        {
            if (_PlayerConfig.Roles.Count == 0)
                return true;

            foreach(var r in _PlayerConfig.Roles)
            {
                if (r.Role == profile.role && (string.IsNullOrEmpty(r.Password) || r.Password == profile.password))
                    return true;
            }
            return false;
        }

        private void InstantiatePlayer(NetworkConnectionToClient conn, PlayerProfile profile)
        {
            var go = Instantiate(_networkManager.playerPrefab);
            go.name = $"Player_{conn.connectionId}";
            var player = go.GetComponent<NetworkPlayerController>();

            player.ApplyProfile(conn, profile, conn.connectionId);
            CurrentPlayers.Add(player);

            NetworkServer.AddPlayerForConnection(conn, go);
            Debug.Log($"Instantiated a new player for connection ID {conn.connectionId} with netID {conn.identity.netId}");
        }

        private void Client_OnPlayerCreationFailedResponse(PlayerCreationFailedResponseMessage message)
        {
            Debug.Log($"Could not join server: {message.errorText}");
        }

        public void Client_OnSceneChanged()
        {
            if (NetworkClient.localPlayer == null)
            {
                if (NetworkClient.connection == null)
                {
                    Debug.LogError("AddPlayer requires a valid NetworkClient.connection.");
                    return;
                }

                if (!NetworkClient.ready)
                {
                    Debug.LogError("AddPlayer requires a ready NetworkClient.");
                    return;
                }

                if (NetworkClient.connection.identity != null)
                {
                    Debug.LogError("NetworkClient.AddPlayer: a PlayerController was already added. Did you call AddPlayer twice?");
                    return;
                }

                SendPlayerCreationRequest();
            }
        }

        private void SendPlayerCreationRequest()
        {
            var playerCreationRequest = new PlayerCreationRequestMessage()
            {
                profile = PlayerProfile
            };
            NetworkClient.connection.Send(playerCreationRequest);
            Debug.Log("Sending request for player creation.");
            // TODO conn.identity should be the player object!! so it is not here yet... I cannot use identity.netId...
            // nor conn.connectionId, because the clients don't know client (own or others) IDs
        }


        private void Client_OnPlayerLeftMessage(PlayerLeftMessage message)
        {
            Debug.Log("Player left (Player ID: " + message.playerID + ")");
        }

        private void Server_OnPlayerLeavingMessage(NetworkConnection conn, PlayerLeavingMessage message)
        {
            var player = conn.identity.GetComponent<NetworkPlayerController>();
            CurrentPlayers.RemoveAll(r => r.ConnectionID == player.ConnectionID);
            conn.Disconnect();
            Debug.Log($"Player left (Player ID: {player.ConnectionID})");
            var leftMessage = new PlayerLeftMessage { playerID = player.ConnectionID };
            NetworkServer.SendToAll(leftMessage);
        }

        private void Client_OnStop()
        {
            var mess = new PlayerLeavingMessage();
            NetworkClient.connection.Send(mess);
        }
    }
}
