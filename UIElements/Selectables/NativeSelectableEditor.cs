// Author: František Holubec
// Created: 25.05.2026

#if UNITY_EDITOR
using UnityEditor.UI;

namespace EDIVE.UIElements.Selectables
{
    // Custom editor for native selectables. Only so it does not draw child property fields
    public class NativeSelectableEditor : SelectableEditor { }
}
#endif