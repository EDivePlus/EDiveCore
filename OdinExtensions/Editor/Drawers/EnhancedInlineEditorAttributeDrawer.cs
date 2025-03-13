using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
#pragma warning disable
    
    public static class EnhancedInlineEditorAttributeDrawer
    {
        public static int CurrentInlineEditorDrawDepth { get; set; }
        
        public static int UniversalMaxCurrentInlineEditorDrawDepth => Mathf.Max(CurrentInlineEditorDrawDepth, InlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth);
        public static int UniversalMinCurrentInlineEditorDrawDepth => Mathf.Min(CurrentInlineEditorDrawDepth, InlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth);
    }
    
    [DrawerPriority(0,100,0)]
    public class EnhancedInlineEditorAttributeDrawer<T> : OdinAttributeDrawer<EnhancedInlineEditorAttribute, T>, IDisposable
        where T : ScriptableObject 
    {
        private LocalPersistentContext<bool> _isExpanded;
        private Vector2 _scrollPos;
        private UnityEditor.Editor _editor;
        private ValueResolver<bool> _conditionResolver;
        private ValueResolver<Color> _colorResolver;
        private ScriptableObject _currentValue;

        private GUIStyle _richTextFoldoutStyle;
        private GUIStyle RichTextFoldoutStyle => _richTextFoldoutStyle ??= _richTextFoldoutStyle = new GUIStyle(SirenixGUIStyles.Foldout) {richText = true};
        
        protected override void Initialize()
        {
            _isExpanded = this.GetPersistentValue("IsVisible", Attribute.HasDefinedExpanded && Attribute.Expanded);
            _conditionResolver = ValueResolver.Get<bool>(Property, Attribute.Condition, true);
            _currentValue = ValueEntry.SmartValue;
            
            _colorResolver = ValueResolver.Get(Property, Attribute.Color, Attribute.DefaultColor);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (Property.Parent == null)
            {
                CallNextDrawer(label);
                return;
            }
            
            ValueResolver.DrawErrors(_conditionResolver, _colorResolver);
            if (Attribute.Condition != null)
            {
                if (!_conditionResolver.HasError && !_conditionResolver.GetValue())
                {
                    CallNextDrawer(label);
                    return;
                }
            }

            EditorGUILayout.BeginVertical();
            
            if (!Attribute.HideFrame)
            {
                var previousBgColor = GUI.backgroundColor;
                if (Attribute.HasColorDefined)
                {
                    var backgroundColor = Attribute.DefaultColor;
                    if (Attribute.Color != null && !_colorResolver.HasError)
                    {
                        backgroundColor = _colorResolver.GetValue();
                    
                    }
                    GUI.backgroundColor = backgroundColor;
                }
                
                SirenixEditorGUI.BeginBox();
                
                GUI.backgroundColor = previousBgColor;
            }
            if (ValueEntry.SmartValue != null)
            {
                var showObjectField = Attribute.ObjectFieldVisibility == ObjectFieldVisibilityMode.Shown;
                var showLabel = label != null && 
                                (Attribute.LabelVisibility == LabelVisibilityMode.Shown || 
                                 (showObjectField && Attribute.LabelVisibility == LabelVisibilityMode.ShowIfObjectFieldVisible));
                
                if (showObjectField || showLabel)
                {
                    if (!Attribute.HideFrame) SirenixEditorGUI.BeginBoxHeader();
                    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22));
                    
                    var labelWidth = showLabel ? EditorGUIUtility.labelWidth : 15;
                    EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(labelWidth));
                    if (!Attribute.HasDefinedExpanded)
                    {
                        _isExpanded.Value = showLabel ? 
                            SirenixEditorGUI.Foldout(_isExpanded.Value, label, RichTextFoldoutStyle) : 
                            SirenixEditorGUI.Foldout(_isExpanded.Value, (string) null, SirenixGUIStyles.Foldout);
                    }
                    else
                    {
                        if (showLabel)
                        {
                            EditorGUILayout.PrefixLabel(label, new GUIStyle(SirenixGUIStyles.Label){richText = true}); 
                        }
                        _isExpanded.Value = true;
                    } 

                    EditorGUILayout.EndHorizontal();
                    if (showObjectField)
                    {
                        GUILayout.Space(-2);
                        CallNextDrawer(null);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!Attribute.HideFrame) SirenixEditorGUI.EndBoxHeader();  
                }
                else
                {
                    _isExpanded.Value = true;
                }
            }
            else
            {
                if (Attribute.ObjectFieldVisibility != ObjectFieldVisibilityMode.Hidden)
                {
                    CallNextDrawer(Attribute.LabelVisibility != LabelVisibilityMode.Hidden ? label : null);
                }
                else if (Attribute.LabelVisibility != LabelVisibilityMode.Hidden)
                {
                    EditorGUILayout.LabelField(label, new GUIStyle(SirenixGUIStyles.Label){richText = true});
                }
                if (_editor != null) Object.DestroyImmediate(_editor);
                if (!Attribute.HideFrame) SirenixEditorGUI.EndBox();
                EditorGUILayout.EndVertical();
                return;
            }

            if (_currentValue != ValueEntry.SmartValue && _editor != null)
            {
                Object.DestroyImmediate(_editor);
                _currentValue = ValueEntry.SmartValue;
            }
                
            GUIHelper.PushHierarchyMode(false);
            if (SirenixEditorGUI.BeginFadeGroup(this, _isExpanded.Value))
            {
                GUIHelper.PushGUIEnabled(!Attribute.ReadOnlyContent);
                GUIHelper.PushIndentLevel(EditorGUI.indentLevel + Attribute.ContentIndent);
                if (!Mathf.Approximately(Attribute.MaxHeight, 0))
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayoutOptions.MaxHeight(200f));
                var showMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = false;
                EditorGUI.BeginChangeCheck();
                if (_editor == null)
                    UnityEditor.Editor.CreateCachedEditor(ValueEntry.SmartValue, null, ref _editor);

                if (_editor != null)
                {
                    // Many of unity editors will not work if the header is not drawn.
                    // So lets draw it anyway. -_-
                    GUIHelper.BeginDrawToNothing();
                    _editor.DrawHeader();
                    GUIHelper.EndDrawToNothing();
                    
                    var monoScriptInEditor = GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor;
                    GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor = false;
                    EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth++;
                    try
                    {
                        _editor.OnInspectorGUI();
                    }
                    finally
                    {
                        EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth--;
                        GlobalConfig<GeneralDrawerConfig>.Instance.ShowMonoScriptInEditor = monoScriptInEditor;
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    var baseValueEntry = Property.BaseValueEntry;
                    if (baseValueEntry != null)
                    {
                        for (var index = 0; index < baseValueEntry.ValueCount; ++index)
                            baseValueEntry.ApplyChanges();
                    }
                }
                EditorGUI.showMixedValue = showMixedValue;
                if (!Mathf.Approximately(Attribute.MaxHeight, 0))
                    EditorGUILayout.EndScrollView();
                
                GUIHelper.PopIndentLevel();
                GUIHelper.PopGUIEnabled();
            }
            SirenixEditorGUI.EndFadeGroup();
            GUIHelper.PopHierarchyMode();
            if (!Attribute.HideFrame) SirenixEditorGUI.EndBox();
            EditorGUILayout.EndVertical();
        }

        public void Dispose()
        {
            if (_editor != null)
            {
                try
                {
                    Object.DestroyImmediate(_editor);
                }
                finally
                {
                    _editor = null; 
                }
            }

        }
    }
}
