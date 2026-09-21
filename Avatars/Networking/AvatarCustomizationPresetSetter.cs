// Author: Michal Petr
// Created: 21.09.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.ServiceHub;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Avatars.Networking
{
    public class AvatarCustomizationPresetSetter : MonoBehaviour
    {
        public enum TriggerType
        {
            Select,
            Activate
        }

        [SerializeField]
        private XRBaseInteractable _Interactable;

        [SerializeField]
        private TriggerType _Trigger = TriggerType.Activate;

        [SerializeField]
        private VisualPreset _Preset = new();

        private void Awake()
        {
            if (_Interactable == null)
                TryGetComponent(out _Interactable);
        }

        private void OnEnable()
        {
            if (_Interactable == null)
                return;

            if (_Trigger == TriggerType.Select)
                _Interactable.selectEntered.AddListener(OnSelectEntered);
            else
                _Interactable.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            if (_Interactable == null)
                return;

            _Interactable.selectEntered.RemoveListener(OnSelectEntered);
            _Interactable.activated.RemoveListener(OnActivated);
        }

        private void OnSelectEntered(SelectEnterEventArgs _) => ApplyPreset().Forget();
        private void OnActivated(ActivateEventArgs _) => ApplyPreset().Forget();

        private async UniTaskVoid ApplyPreset()
        {
            if (!AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub) || serviceHub.SaveData == null)
            {
                Debug.LogWarning("Save data service not available, cannot set customization preset.");
                return;
            }

            var result = await serviceHub.SaveData.User.GetSaveDataAsync<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY, destroyCancellationToken);
            var saveData = result.Value;
            if (saveData == null)
                return;

            var merged = saveData.CustomizationPreset ?? new VisualPreset();
            foreach (var record in _Preset.EnumerateValidRecords())
                merged = merged.WithRecord(record);

            saveData.CustomizationPreset = merged;
        }
    }
}
