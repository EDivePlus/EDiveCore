// Author: František Holubec
// Created: 09.10.2026

using System.Collections.Generic;
using EDIVE.VisualPresets.Presets;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    [CreateAssetMenu(menuName = "EDIVE/View/Dialog Setup")]
    public class DialogSetup : ScriptableObject, IDialogSetup
    {
        [SerializeField]
        private VisualPreset _Visual = new();

        [SerializeField]
        private List<DialogButtonSetup> _Buttons = new();

        public VisualPreset Visual => _Visual;
        public IReadOnlyList<DialogButtonSetup> Buttons => _Buttons;
    }
}
