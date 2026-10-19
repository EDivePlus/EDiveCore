// Author: Michal Petr
// Created: 21.09.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.SaveData;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public abstract class AAvatarCustomizationController<TVisualID, TRecord> : MonoBehaviour
        where TVisualID : ABaseVisualID
        where TRecord : AVisualPresetRecord<TVisualID>
    {
        [SerializeField]
        private TVisualID _ChangedVisualID;

        private AvatarPlayerSaveData _saveData;
        private TRecord _lastSetRecord;
        private SaveDataService _saveDataService;
        private ClientAuthService _clientAuth;
        private CancellationTokenSource _loadCts;

        protected TVisualID ChangedVisualID => _ChangedVisualID;

        protected virtual void Awake()
        {
            if (!AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub) || serviceHub.SaveData == null)
                return;
            _saveDataService = serviceHub.SaveData;
            _clientAuth = serviceHub.ClientAuth;
            if (_clientAuth != null)
                _clientAuth.OnLoginSucceeded += OnLoggedIn;
            StartLoad();
        }

        protected virtual void OnDestroy()
        {
            if (_clientAuth != null)
                _clientAuth.OnLoginSucceeded -= OnLoggedIn;
            ReleaseSaveData();
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
        }

        // Save data belongs to the logged in account
        private void OnLoggedIn(LoginResponse response) => StartLoad();

        private void StartLoad()
        {
            ReleaseSaveData();
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LoadSaveData(_loadCts.Token).Forget();
        }

        private void ReleaseSaveData()
        {
            if (_saveData != null)
                _saveData.CustomizationChanged -= OnCustomizationChanged;
            _saveData = null;
            _lastSetRecord = null;
        }

        private async UniTask LoadSaveData(CancellationToken cancellationToken)
        {
            var result = await _saveDataService.User.GetSaveDataAsync<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            _saveData = result.Value;
            if (_saveData == null)
                return;

            _saveData.CustomizationChanged += OnCustomizationChanged;
            OnCustomizationChanged(_saveData.CustomizationPreset);
        }

        private void OnCustomizationChanged(VisualPreset preset)
        {
            TRecord current = null;
            preset?.TryGetRecord(_ChangedVisualID, out current);
            if (ReferenceEquals(current, _lastSetRecord))
                return;

            OnRecordChanged(current);
        }

        protected void SetRecord(TRecord record)
        {
            if (_saveData == null || record == null)
                return;

            _lastSetRecord = record;
            _saveData.CustomizationPreset = (_saveData.CustomizationPreset ?? new VisualPreset()).WithRecord(record);
        }

        protected abstract void OnRecordChanged(TRecord current);
    }
}
