// Author: Michal Petr
// Created: 17.03.2026

using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Input.Controls;
using EDIVE.NativeUtils;
using EDIVE.Networking.Players;
using EDIVE.Networking.Utils;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.SaveData;
using EDIVE.XRTools.Controls;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class NetworkAvatarPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetAssigner _IKAssigner;

        [SerializeField, Min(0f)]
        private float _PlayerSummonRadius = 0.75f;

        private NetworkPlayerManager _networkPlayerManager;
        private ServiceHubManager _serviceHubManager;
        private AvatarPlayerSaveData _saveData;

        private readonly SyncVar<AvatarDefinition> _avatarDefinition = new();

        private readonly SyncVar<int> _avatarObjectId = new(0);
        private readonly SyncVarNetworkBehaviourResolver<AvatarController> _avatarResolver = new();

        private void Awake()
        {
            _avatarResolver.BindToSyncVar(_avatarObjectId);
            _avatarResolver.OnChanged += OnAvatarChanged;

            _networkPlayerManager = AppCore.Services.Get<NetworkPlayerManager>();
            _serviceHubManager = AppCore.Services.Get<ServiceHubManager>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
                LoadAndApplySavedAvatar().Forget();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (_saveData != null)
            {
                _saveData.MarkedAsDirty -= OnSaveDataMarkedAsDirty;
                _saveData = null;
            }
        }

        private async UniTaskVoid LoadAndApplySavedAvatar()
        {
            if (_serviceHubManager == null)
                return;

            var ct = this.GetCancellationTokenOnDestroy();
            var result = await _serviceHubManager.GetSaveData<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY, ct);
            if (ct.IsCancellationRequested)
                return;

            _saveData = result.IsSuccess && result.Value != null ? result.Value : new AvatarPlayerSaveData();
            _saveData.ClearDirty();
            _saveData.MarkedAsDirty += OnSaveDataMarkedAsDirty;

            if (_saveData.PlayerAvatar != null && _saveData.PlayerAvatar.IsValid())
                SetAvatar(_saveData.PlayerAvatar);
        }

        private void OnSaveDataMarkedAsDirty()
        {
            var data = _saveData;
            if (_serviceHubManager == null || data == null)
                return;

            data.ClearDirty();
            _serviceHubManager.SetSaveData(AvatarPlayerSaveData.KEY, data, SaveDataDirtyFlag.OnEndOfFrame, this.GetCancellationTokenOnDestroy()).Forget();
        }
        
        private void OnAvatarChanged(AvatarController avatar)
        {
            if (avatar == null) 
                return;
            
            avatar.NetworkObject.SetParent(this);
            
            if (_IKAssigner != null)
                _IKAssigner.Assign(avatar);
            
            avatar.SetLocalPlayer(IsOwner);
        }
        
        [Server]
        private void ServerApplyAvatar(AvatarDefinition avatarDef)
        {
            if (_avatarDefinition.Value != avatarDef)
                _avatarDefinition.Value = avatarDef;
            
            if (avatarDef == null || !avatarDef.IsValid())
            {
                Debug.LogError($"Invalid avatar definition");
                return;
            }
            
            var networkManager = InstanceFinder.NetworkManager;
            
            // Despawn existing avatar if any
            if (_avatarResolver.Value != null)
            {
                networkManager.ServerManager.Despawn(_avatarResolver.Value.gameObject);
                _avatarResolver.SetValue(null);
            }

            // Spawn new avatar
            var netObj = networkManager.GetPooledInstantiated(avatarDef.AvatarPrefab.gameObject, true);
            networkManager.ServerManager.Spawn(netObj, Owner);
            var avatar = netObj.GetComponent<AvatarController>();
            
            _avatarResolver.SetValue(avatar);
        }
        
        [ServerRpc]
        public void SetAvatar(AvatarDefinition avatarDef)
        {
            ServerApplyAvatar(avatarDef);
        }

        public void SelectAvatar(AvatarDefinition avatarDef)
        {
            if (_saveData != null)
                _saveData.PlayerAvatar = avatarDef;

            SetAvatar(avatarDef);
        }

        private Transform GetWorldPoseTransform()
        {
            if (_avatarResolver.Value != null)
                return _avatarResolver.Value.transform;

            return transform;
        }
        
        [Server]
        private void ServerRequestTeleportOwner(Vector3 position, Quaternion rotation)
        {
            TargetRequestTeleport(Owner, position, rotation);
        }

        [TargetRpc]
        private void TargetRequestTeleport(NetworkConnection conn, Vector3 position, Quaternion rotation)
        {
            if (AppCore.Services.TryGet<ControlsManager>(out var cm))
            {
                cm.RequestTeleport(position, rotation);
            }
            else
            {
                Debug.LogWarning("[SUMMON] ControlsManager service not found - cannot teleport.");
            }
        }
        
        [Button]
        public void SummonPlayersToMe()
        {
            if (!Application.isPlaying)
                return;

            if (_networkPlayerManager.LocalPlayer == null)
            {
                Debug.LogWarning("LocalPlayer not ready");
                return;
            }

            var t = _networkPlayerManager.LocalPlayer.gameObject.GetComponent<NetworkAvatarPlayerController>().GetWorldPoseTransform();
            if (t == null)
            {
                Debug.LogWarning("LocalPlayer world pose transform not ready yet");
                return;
            }

            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            var msg = new SummonPlayersToMeRequest(
                sceneName,
                t.position,
                t.rotation,
                _PlayerSummonRadius
            );

            ServerSummonPlayersToMeRequest(msg);
            Debug.Log($"[SUMMON] Request sent. scene='{sceneName}' pos={msg.Position}");
        }
        
        [ServerRpc]
        private void ServerSummonPlayersToMeRequest(SummonPlayersToMeRequest request, NetworkConnection conn = null)
        {
            if (conn == null) return;
            
            if (!_networkPlayerManager.CurrentPlayers.TryGetFirst(p => p != null && p.OwnerId == conn.ClientId, out var summoner))
            {
                Debug.LogWarning($"[SUMMON] No player controller for sender={conn.ClientId}. " +
                                 $"Players on server: {string.Join(", ", _networkPlayerManager.CurrentPlayers.Select(p => p != null ? p.OwnerId.ToString() : "null"))}");
                return;
            }

            var sceneName = request.SceneName;

            var targets = _networkPlayerManager.CurrentPlayers
                .Where(p => p != null &&
                            p.Owner != null &&
                            p.Owner != conn &&
                            InScene(p.Owner))
                .ToList();

            var count = targets.Count;
            for (var i = 0; i < count; i++)
            {
                var t = targets[i];
                var offset = ComputeCircleOffset(i, count, Mathf.Max(0f, request.Radius));
                
                t.GetComponent<NetworkAvatarPlayerController>().ServerRequestTeleportOwner(request.Position + offset, request.Rotation);
            }

            Debug.Log($"[SUMMON] Requested teleport for {count} players in scene '{sceneName}' to sender {conn.ClientId}.");
            return;

            bool InScene(NetworkConnection c) =>
                c != null && c.Scenes != null && c.Scenes.Any(s => s.IsValid() && s.name == sceneName);
        }
        
        private static Vector3 ComputeCircleOffset(int index, int total, float radius)
        {
            if (total <= 1 || radius <= 0f) return Vector3.zero;
            var angle = (Mathf.PI * 2f) * (index / (float)total);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }
    }
}
