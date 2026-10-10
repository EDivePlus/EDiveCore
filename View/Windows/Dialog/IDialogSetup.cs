// Author: František Holubec
// Created: 09.10.2026

using System.Collections.Generic;
using EDIVE.VisualPresets.Presets;

namespace EDIVE.View.Windows.Dialog
{
    // Dialog window context
    public interface IDialogSetup
    {
        VisualPreset Visual { get; }
        IReadOnlyList<DialogButtonSetup> Buttons { get; }
    }
}
