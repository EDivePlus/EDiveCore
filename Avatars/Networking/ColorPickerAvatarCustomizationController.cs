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
                _ColorPickerController.ColorCommitted += OnColorCommitted;
        }
        private void OnDisable()
        {
            if (_ColorPickerController != null)
                _ColorPickerController.ColorCommitted -= OnColorCommitted;
        }

        protected override void OnRecordChanged(ColorVisualPresetRecord current)
        {
            if (current != null && _ColorPickerController != null)
                _ColorPickerController.SetColor(current.Color, false);
        }

        private void OnColorCommitted(Color newColor)
        {
            SetRecord(new ColorVisualPresetRecord(ChangedVisualID, newColor));
        }
    }
}
