// Author: František Holubec
// Created: 09.10.2026

using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;

namespace EDIVE.OdinExtensions.NativeEditors
{
    // Exact type only, subclasses keep their own editors
    [CustomEditor(typeof(TextMeshProUGUI))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class TextMeshProUGUIOdinEditor : NativeWrapperOdinEditor<TextMeshProUGUI, TMP_EditorPanelUI>
    {
    }
}
