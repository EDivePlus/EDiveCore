// Author: Michal Petr
// Created: 21.09.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.ServiceHub;
using EDIVE.Utils.Activations;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Avatars.Networking
{
    public class AvatarCustomizationPresetSetter : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;
        
        [SerializeField]
        private VisualPreset _Preset = new();

        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(Activate);
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(Activate);
        }

        private void Activate() => ApplyPreset().Forget();

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
