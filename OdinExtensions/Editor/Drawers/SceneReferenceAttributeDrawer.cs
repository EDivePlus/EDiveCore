using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class SceneReferenceAttributeDrawer : OdinAttributeDrawer<SceneReferenceAttribute, string>
    {
        private List<SceneAsset> _availableScenes;

        protected override void Initialize()
        {
            base.Initialize();
            RefreshAvailableScenes();
        }

        private void RefreshAvailableScenes()
        {
            _availableScenes ??= new List<SceneAsset>();
            _availableScenes.Clear();
            
            if (Attribute.OnlyBuildScenes)
            {
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    if (string.IsNullOrEmpty(scenePath)) continue;

                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset == null) continue;

                    _availableScenes.Add(sceneAsset);
                }
            }
            else
            {
                var guids = AssetDatabase.FindAssets("t:Scene");
                if (guids != null)
                {
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        _availableScenes.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
                    }
                }
            }
            _availableScenes.Sort((sceneA, sceneB) => string.Compare(sceneA.name, sceneB.name, StringComparison.Ordinal));
        }

        private bool IsSceneValid(string sceneString, out SceneAsset sceneAsset, out string label)
        {
            sceneAsset = GetSceneAsset(sceneString);
            var isValid = sceneAsset != null || string.IsNullOrWhiteSpace(sceneString);
            label = isValid ? ValueEntry.SmartValue : $"{ValueEntry.SmartValue} (INVALID)";
            return isValid;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var dropdownRect = SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            var isSceneValid = IsSceneValid(ValueEntry.SmartValue, out var sceneAsset, out var sceneLabel);
            
            var iconRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(iconRect, FontAwesomeEditorIcons.SquareCaretDownSolid, "Select"))
            {
                CreateSelector(dropdownRect);
            }
            if (isSceneValid)
            {
                
                EditorGUI.BeginChangeCheck();
                var newSceneAsset = (SceneAsset) SirenixEditorFields.UnityObjectField(sceneAsset, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    ValueEntry.SmartValue = GetSceneString(newSceneAsset);
                }
            }
            else
            {
                EditorGUILayout.TextField(sceneLabel);
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }
        
        private OdinSelector<string> CreateSelector(Rect rect)
        {
            RefreshAvailableScenes();
            var isSceneValid = IsSceneValid(ValueEntry.SmartValue, out _, out var sceneLabel);
            var scenesDropdown = GetScenesDropdown();
            if (!isSceneValid) scenesDropdown = scenesDropdown.Prepend(sceneLabel);

            var selector = new GenericSelector<string>("Scenes", false, x => x, scenesDropdown);
            selector.SetSelection(ValueEntry.SmartValue);
            selector.SelectionTree.DefaultMenuStyle.Height = 22;
            selector.SelectionTree.Config.DrawSearchToolbar = true;
            selector.SelectionTree.Config.AutoFocusSearchBar = true;
            selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);
            selector.SelectionConfirmed += selection =>
            {
                var selected = selection.FirstOrDefault();
                if (!isSceneValid && string.IsNullOrEmpty(selected))
                    return;

                ValueEntry.SmartValue = selected;
            };
            var window = selector.ShowInPopup(rect);
            window.OnClose += selector.SelectionTree.Selection.ConfirmSelection;
            return selector;
        }
        
        private IEnumerable<string> GetScenesDropdown()
        {
            switch (Attribute.ReferenceType)
            {
                case SceneReferenceType.Path:
                    return _availableScenes.Select(AssetDatabase.GetAssetPath).Prepend(string.Empty);
                case SceneReferenceType.Name:
                    return _availableScenes.Select(scene => scene.name).Prepend(string.Empty);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private string GetSceneString(SceneAsset sceneAsset)
        {
            if (sceneAsset == null) 
                return string.Empty;
            
            return Attribute.ReferenceType switch
            {
                SceneReferenceType.Path => AssetDatabase.GetAssetPath(sceneAsset),
                SceneReferenceType.Name => sceneAsset.name,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        private SceneAsset GetSceneAsset(string scene)
        {
            foreach (var sceneAsset in _availableScenes)
            {
                if (sceneAsset == null) continue;
                switch (Attribute.ReferenceType)
                {
                    case SceneReferenceType.Path:
                        if (AssetDatabase.GetAssetPath(sceneAsset) == scene) return sceneAsset;
                        break;
                    
                    case SceneReferenceType.Name: 
                        if (sceneAsset.name == scene) return sceneAsset;
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            return null;
        }
    }
}
