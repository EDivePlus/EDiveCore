// Author: František Holubec
// Created: 09.10.2026

using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.View.Windows.Dialog
{
    // Value a dialog closes with when its button is pressed
    [EnhancedTypeSelector(true, 1)]
    public interface IDialogResult
    {
        object Value { get; }
    }
}
