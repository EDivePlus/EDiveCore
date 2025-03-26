// Author: František Holubec
// Created: 26.03.2025

#if UNITY_EDITOR
using System;
using EDIVE.OdinExtensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    [UsedImplicitly]
    [DrawerPriority(0, 100, 0)]
    public class JsonAssetFieldAttributeDrawer<T> : OdinAttributeDrawer<JsonAssetFieldAttribute, T> where T : ScriptableObject
    {
        private ValueResolver<JsonSerializer> _jsonSerializerResolver;

        protected override void Initialize()
        {
            if (!string.IsNullOrEmpty(Attribute.Serializer))
                _jsonSerializerResolver = ValueResolver.Get<JsonSerializer>(Property, Attribute.Serializer);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_jsonSerializerResolver);
            EditorGUILayout.BeginHorizontal();
            CallNextDrawer(label);

            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
            GUIHelper.PushGUIEnabled(true);
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.BracketsCurlySolid, "Open JSON editor"))
            {
                var serializer = _jsonSerializerResolver != null && !_jsonSerializerResolver.HasError ? _jsonSerializerResolver?.GetValue() : JsonSerializer.CreateDefault();
                JsonEditWindow.OpenWindow(
                    () =>
                    {
                        try
                        {
                            return JsonAssetUtils.SerializeAsset(serializer, ValueEntry.SmartValue).ToString(Formatting.Indented);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            return string.Empty;
                        }
                    },
                    json =>
                    {
                        try
                        {
                            JsonAssetUtils.PopulateAsset(serializer, json, ValueEntry.SmartValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    });
            }
            GUIHelper.PopGUIEnabled();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

