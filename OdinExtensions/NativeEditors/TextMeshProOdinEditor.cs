// Author: František Holubec
// Created: 09.10.2026

using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;

namespace EDIVE.OdinExtensions.NativeEditors
{
    // With Meta XR the OVRTextFixEditor wins, it wraps the same panel
    [CustomEditor(typeof(TextMeshPro))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class TextMeshProOdinEditor : NativeWrapperOdinEditor<TextMeshPro, TMP_EditorPanel>
    {
    }
}
