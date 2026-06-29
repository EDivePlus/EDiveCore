// Author: Michal Petr
// Created: 17.03.2026

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Extensions.Random;
using EDIVE.Input.Controls;
using EDIVE.NativeUtils;
using EDIVE.Networking.Players;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.SaveData;
using EDIVE.VisualPresets.Presets;
using PurrNet;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class NetworkAvatarPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetAssigner _IKAssigner;

        [SerializeField]
        private Transform _AvatarRoot;
        
        [ShowCreateNew]
        [SerializeField]
        private List<AvatarDefinition> _DefaultAvatars = new();

        [SerializeField, Min(0f)]
        private float _PlayerSummonRadius = 0.75f;

        private NetworkPlayerManager _networkPlayerManager;
        private SaveDataService _saveDataService;
        private AvatarPlayerSaveData _saveData;

        private readonly SyncVar<AvatarDefinition> _avatarDefinition = new(ownerAuth: true);

        private readonly SyncLazyRef<AvatarController> _avatar = new(true);
        
        private readonly SyncVar<VisualPreset> _customizationPreset = new(ownerAuth: true);

        private void Awake()
        {
            _avatar.onChanged += OnAvatarChanged;
            _customizationPreset.onChanged += OnSyncCustomizationChanged;
            _networkPlayerManager = AppCore.Services.Get<NetworkPlayerManager>();
            _saveDataService = AppCore.Services.Get<ServiceHubManager>().SaveData;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _avatar.onChanged -= OnAvatarChanged;
            _customizationPreset.onChanged -= OnSyncCustomizationChanged;
        }

        protected override void OnSpawned()
        {
            if (isOwner)
                LoadAndApplySavedAvatar(destroyCancellationToken).Forget();
        }

        protected override void OnDespawned()
        {
            if (_saveData != null)
            {
                _saveData.CustomizationChanged -= OnAvatarCustomizationChanged;
                _saveData = null;
            }
        }

        private async UniTaskVoid LoadAndApplySavedAvatar(CancellationToken ct)
        {
            if (_saveDataService == null)
                return;
            
            var result = await _saveDataService.User.GetSaveDataAsync<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY, ct);

            _saveData = result.Value;

            _saveData.PlayerAvatar ??= _DefaultAvatars.RandomItem();
            _saveData.CustomizationPreset ??= _saveData.PlayerAvatar.DefaultCustomizations;
            if (_saveData.PlayerAvatar != null && _saveData.PlayerAvatar.IsValid())
                SetAvatar(_saveData.PlayerAvatar);

            _saveData.CustomizationChanged += OnAvatarCustomizationChanged;
        }

        private void OnAvatarChanged(AvatarController old, AvatarController value)
        {
            if (value == null)
                return;
            
            if (_IKAssigner != null)
                _IKAssigner.Assign(value);

            value.SetLocalPlayer(isOwner);
            
            if (_customizationPreset.value != null)
                value.ApplyCustomizationPreset(_customizationPreset.value);
        }

        public void SetAvatar(AvatarDefinition avatarDef)
        {
            if (_saveData != null)
                _saveData.PlayerAvatar = avatarDef;
            
            if (_avatarDefinition.value != avatarDef)
                _avatarDefinition.value = avatarDef;

            if (avatarDef == null || !avatarDef.IsValid())
            {
                Debug.LogError("Invalid avatar definition");
                return;
            }
            
            if (_avatar.value != null)
            {
                Destroy(_avatar.value.gameObject);
                _avatar.value = null;
            }
            
            var avatar = Instantiate(avatarDef.AvatarPrefab, _AvatarRoot, false);
            if (owner.HasValue)
                avatar.GiveOwnership(owner.Value);
            _avatar.value = avatar;

            if (_saveData?.CustomizationPreset != null)
                _customizationPreset.value = _saveData.CustomizationPreset;
        }

        private void OnSaveDataCustomizationChanged(VisualPreset preset)
        {
            _customizationPreset.value = preset;
        }

        private void OnSyncCustomizationChanged(VisualPreset preset)
        {
            if (_avatar.value != null && preset != null)
                _avatar.value.ApplyCustomizationPreset(preset);
        }

        public Transform GetWorldPoseTransform()
        {
            if (_avatar.value != null)
                return _avatar.value.transform;

            return transform;
        }

        private void ServerRequestTeleportOwner(Vector3 position, Quaternion rotation)
        {
            if (!isServer) return; // Replaces FishNet's [Server] attribute.
            if (!owner.HasValue) return;
            TargetRequestTeleport(owner.Value, position, rotation);
        }
        
        [TargetRpc]
        private void TargetRequestTeleport(PlayerID target, Vector3 position, Quaternion rotation)
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
        private void ServerSummonPlayersToMeRequest(SummonPlayersToMeRequest request, RPCInfo info = default)
        {
            var sender = info.sender;

            if (!_networkPlayerManager.CurrentPlayers.TryGetFirst(p => p != null && p.owner.HasValue && p.owner.Value == sender, out _))
            {
                Debug.LogWarning($"[SUMMON] No player controller for sender={sender}. " +
                                 $"Players on server: {string.Join(", ", _networkPlayerManager.CurrentPlayers.Select(p => p != null && p.owner.HasValue ? p.owner.Value.ToString() : "null"))}");
                return;
            }
            
            var targets = _networkPlayerManager.CurrentPlayers
                .Where(p => p != null && p.owner.HasValue && p.owner.Value != sender)
                .ToList();

            var sceneName = request.SceneName;
            var count = targets.Count;
            for (var i = 0; i < count; i++)
            {
                var t = targets[i];
                var offset = ComputeCircleOffset(i, count, Mathf.Max(0f, request.Radius));

                t.GetComponent<NetworkAvatarPlayerController>().ServerRequestTeleportOwner(request.Position + offset, request.Rotation);
            }

            Debug.Log($"[SUMMON] Requested teleport for {count} players in scene '{sceneName}' to sender {sender}.");
        }

        private static Vector3 ComputeCircleOffset(int index, int total, float radius)
        {
            if (total <= 1 || radius <= 0f) return Vector3.zero;
            var angle = (Mathf.PI * 2f) * (index / (float)total);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }
    }
}
