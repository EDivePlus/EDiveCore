// Author: Michal Petr
// Created: 16.06.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.SaveData;
using EDIVE.UIElements.ColorPicker;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class ColorPickerAvatarCustomizationController : MonoBehaviour
    {
        [SerializeField]
        private ColorVisualID _ChangedVisualID;
        
        [SerializeField]
        private ColorPickerController _ColorPickerController;

        private SaveDataService _saveDataService;
        private AvatarPlayerSaveData _saveData;

        private void Awake()
        {
            _saveDataService = AppCore.Services.Get<ServiceHubManager>().SaveData;
            if (_saveDataService == null)
                return;
            LoadSaveData().Forget();
        }
        
        private async UniTask LoadSaveData()
        {
            var result = await _saveDataService.User.GetSaveDataAsync<AvatarPlayerSaveData>(AvatarPlayerSaveData.KEY);

            _saveData = result.Value;

            if (_saveData.CustomizationPreset != null &&
                _saveData.CustomizationPreset.TryGetRecord(_ChangedVisualID, out ColorVisualPresetRecord record))
            {
                _ColorPickerController.SetColor(record.Color, false);
            }
        }

        private void OnEnable()
        {
            _ColorPickerController.ColorChanged += OnColorChanged;
        }

        private void OnDisable()
        {
            _ColorPickerController.ColorChanged -= OnColorChanged;
        }
        
        private void OnColorChanged(Color newColor)
        {
            var preset = new ColorVisualPresetRecord(_ChangedVisualID, newColor);
            var visualPreset = new VisualPreset(preset);
            _saveData.CustomizationPreset = visualPreset;
        }
    }
}
