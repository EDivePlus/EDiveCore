// Author: František Holubec
// Created: 06.03.2022
// Copyright (c) Noxgames
// http://www.noxgames.com/

using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ProtoGIS.Scripts.Utils.OdinExtensions.Editor
{
    public class TagSelectorAttributeDrawer : OdinAttributeDrawer<TagSelectorAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueEntry.SmartValue = EditorGUILayout.TagField(label, ValueEntry.SmartValue);
        }
    }
}
