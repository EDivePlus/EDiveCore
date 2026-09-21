// Author: Michal Petr
// Created: 21.09.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.ServiceHub;
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

        protected TVisualID ChangedVisualID => _ChangedVisualID;

        protected virtual void Awake()
        {
            var saveDataService = AppCore.Services.Get<ServiceHubManager>().SaveData;
            if (saveDataService == null)
                return;
            LoadSaveData(saveDataService).Forget();
        }

        protected virtual void OnDestroy()
        {
            if (_saveData != null)
                _saveData.CustomizationChanged -= OnCustomizationChanged;
        }

        private async UniTask LoadSaveData(SaveDataService saveDataService)
        {
            var result = await saveDataService.User.GetSaveDataAsync<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY, destroyCancellationToken);
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
