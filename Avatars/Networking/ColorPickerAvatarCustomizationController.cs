// Author: Michal Petr
// Created: 16.06.2026

using EDIVE.UIElements.ColorPicker;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class ColorPickerAvatarCustomizationController : AAvatarCustomizationController<ColorVisualID, ColorVisualPresetRecord>
    {
        [SerializeField]
        private ColorPickerController _ColorPickerController;

        private void OnEnable()
        {
            if (_ColorPickerController != null)
                _ColorPickerController.ColorChanged += OnColorChanged;
        }
        private void OnDisable()
        {
            if (_ColorPickerController != null)
                _ColorPickerController.ColorChanged -= OnColorChanged;
        }

        protected override void OnRecordChanged(ColorVisualPresetRecord current)
        {
            if (current != null && _ColorPickerController != null)
                _ColorPickerController.SetColor(current.Color, false);
        }

        private void OnColorChanged(Color newColor)
        {
            SetRecord(new ColorVisualPresetRecord(ChangedVisualID, newColor));
        }
    }
}
