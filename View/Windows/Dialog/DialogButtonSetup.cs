// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.VisualPresets.Presets;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    [Serializable]
    public class DialogButtonSetup
    {
        [SerializeField]
        private VisualPreset _Visual = new();

        // Empty closes with null
        [SerializeReference]
        private IDialogResult _Result;

        public VisualPreset Visual => _Visual;
        public object ResultValue => _Result?.Value;
    }
}
