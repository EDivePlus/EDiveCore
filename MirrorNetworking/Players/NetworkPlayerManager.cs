using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.MirrorNetworking;
using EDIVE.MirrorNetworking.Players;
using EDIVE.MirrorNetworking.Scenes;
using Edive.Networking;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

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

        private NetworkSceneManager _networkSceneManager;
        private MasterNetworkManager _networkManager;

        public uint LocalPlayerID { get => NetworkClient.localPlayer.netId; }

        public Signal<string> Client_OnInvalidLogin { get; } = new();

        // TODO remove this in the future and use the Mirror auth instead
        // this is just to be able to show the message in the lobby when disconnected from the server due to invalid login
        // this class is not destroyed therefore the message gets transfered
        public string failedAuthMessage { get; private set; }

        public static Dictionary<uint, NetworkPlayerController> Client_ConnectedPlayers { get; }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = AppCore.Services.Get<MasterNetworkManager>();
            _networkSceneManager = AppCore.Services.Get<NetworkSceneManager>();


            _networkManager.ServerStarted.AddListener(Server_OnStart);
            _networkManager.ServerClientDisconnecting.AddListener(Server_RemovePlayer);

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
                _networkManager.ServerClientDisconnecting.RemoveListener(Server_RemovePlayer);
                _networkManager.ClientStarted.RemoveListener(Client_OnStart);
                _networkManager.ClientStopped.RemoveListener(Client_OnStop);
            }

            if (AppCore.Services.TryGet(out _networkSceneManager))
            {
                _networkSceneManager.ClientSceneChanged.RemoveListener(Client_OnSceneChanged);
            }
        }


        public static bool TryGetPlayer(uint id, out NetworkPlayerController player)
        {
            Client_ConnectedPlayers.TryGetValue(id, out player);
            return player != null;
        }


        private void Server_RemovePlayer(NetworkConnection conn)
        {
            // conn.identity should be the player object
            var player = conn.identity?.GetComponent<NetworkPlayerController>();
            if (player != null)
            {
                Debug.LogError("Non-critical: Could not remove null player");
                return;
            }
        }

        public void Server_OnStart()
        {
            NetworkServer.RegisterHandler<PlayerCreationRequestMessage>(Server_OnPlayerCreationRequest);
            NetworkServer.RegisterHandler<PlayerInteractionMessage>(Server_OnPlayerInteractionMessage);
            NetworkServer.RegisterHandler<PlayerActionMessage>(Server_OnPlayerActionMessage);
            NetworkServer.RegisterHandler<PlayerLeavingMessage>(Server_OnPlayerLeavingMessage);
        }

        public void Client_OnStart()
        {
            NetworkClient.RegisterHandler<PlayerCreationFailedResponseMessage>(Client_OnPlayerCreationFailedResponse);
            NetworkClient.RegisterHandler<PlayerLeftMessage>(Client_OnPlayerLeftMessage);
            // clear the message when connecting again
            failedAuthMessage = "";
        }

        private void Server_OnPlayerCreationRequest(NetworkConnectionToClient conn, PlayerCreationRequestMessage request)
        {
            if (!ValidateProfile(request.profile))
            {
                Debug.Log("Player does not have permission to join.");
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
            var player = go.GetComponent<NetworkPlayerController>();

            player.ApplyProfile(profile, conn.connectionId);

            NetworkServer.AddPlayerForConnection(conn, go);
            Debug.Log($"Instantiated a new player for connection ID {conn.connectionId} with netID {conn.identity.netId}");
        }

        private void Client_OnPlayerCreationFailedResponse(PlayerCreationFailedResponseMessage message)
        {
            Debug.Log("Could not join server: " + message.errorText);
            Client_OnInvalidLogin.Dispatch(message.errorText);
            failedAuthMessage = message.errorText;
        }

        // Send player creation request
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
                profile = PlayerProfileManager.LOCAL_PROFILE
            };
            NetworkClient.connection.Send(playerCreationRequest);
            Debug.Log("Sending request for player creation.");
            // TODO conn.identity should be the player object!! so it is not here yet... I cannot use identity.netId...
            // nor conn.connectionId, because the clients don't know client (own or others) IDs
        }

        private void SendActionNotificationToServer(string act)
        {
            var action = new PlayerActionMessage()
            {
                playerID = LocalPlayerID,
                action = act
            };
            NetworkClient.connection.Send(action);
        }

        private void SendInteractionNotificationToServer(uint interactedWithID, string action)
        {
            var interactionMessage = new PlayerInteractionMessage()
            {
                interactorID = LocalPlayerID,
                interacteeID = interactedWithID,
                interaction = action
            };
            NetworkClient.connection.Send(interactionMessage);
        }

        private void Client_OnPlayerLeftMessage(PlayerLeftMessage message)
        {
            Debug.Log("Player left (Player ID: " + message.playerID + ")");
        }

        private void Server_OnPlayerLeavingMessage(NetworkConnection conn, PlayerLeavingMessage message)
        {
            var player = conn.identity.GetComponent<NetworkPlayerController>();
            conn.Disconnect();
            Debug.Log($"Player left (Player ID: {player.ConnectionID})");
            var leftMessage = new PlayerLeftMessage { playerID = player.ConnectionID };
            NetworkServer.SendToAll(leftMessage);
        }

        private void Server_OnPlayerActionMessage(NetworkConnection conn, PlayerActionMessage message)
        {
            //UVRN_Logger.Instance.LogEntry(message.playerID.ToString(), $"Action performed: {message.action}");
        }

        private void Server_OnPlayerInteractionMessage(NetworkConnection conn, PlayerInteractionMessage message)
        {
            //UVRN_Logger.Instance.LogEntry(message.interactorID.ToString(), $"Interacting with {message.interacteeID}: {message.interaction}");
        }

        private void Client_OnStop()
        {
            var mess = new PlayerLeavingMessage();
            NetworkClient.connection.Send(mess);
        }
    }
}
