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
        private List<SceneAsset> _allScenes;

        protected override void Initialize()
        {
            base.Initialize();
            Refresh();
        }

        private void Refresh()
        {
            _allScenes ??= new List<SceneAsset>();
            _allScenes.Clear();
            
            if (Attribute.OnlyBuildScenes)
            {
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    if (string.IsNullOrEmpty(scenePath)) continue;

                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset == null) continue;

                    _allScenes.Add(sceneAsset);
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
                        _allScenes.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
                    }
                }
            }

            _allScenes.Sort((sceneA, sceneB) => string.Compare(sceneA.name, sceneB.name, StringComparison.Ordinal));
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var sceneAsset = GetSceneAsset(ValueEntry.SmartValue, Attribute.ReferenceType);
            var isInvalidScene = !string.IsNullOrWhiteSpace(ValueEntry.SmartValue) && sceneAsset == null;
            var buttonLabel = isInvalidScene ? $"{ValueEntry.SmartValue} (INVALID)" : ValueEntry.SmartValue;
            
            EditorGUILayout.BeginHorizontal();
            {
                GenericSelector<string>.DrawSelectorDropdown(label, buttonLabel, rect =>
                {
                    var scenesDropdown = GetScenesDropdown();
                    if (isInvalidScene) scenesDropdown = scenesDropdown.Prepend(buttonLabel);
                
                    var selector = new GenericSelector<string>("Scenes", false, x => x, scenesDropdown);
                    selector.SetSelection(ValueEntry.SmartValue);
                    selector.SelectionTree.DefaultMenuStyle.Height = 22;
                    selector.SelectionTree.Config.DrawSearchToolbar = true;
                    selector.SelectionTree.Config.AutoFocusSearchBar = true;
                    selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);
                    selector.SelectionConfirmed += selection =>
                    {
                        ValueEntry.SmartValue = selection.FirstOrDefault();
                    };
                    var window = selector.ShowInPopup(rect);
                    window.OnClose += selector.SelectionTree.Selection.ConfirmSelection;
                    return selector;
                });
            
                GUILayout.Space(4);
                EditorGUI.BeginDisabledGroup(sceneAsset == null);
                var pingRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(pingRect, FontAwesomeEditorIcons.LocationCrosshairsSolid))
                {
                    EditorGUIUtility.PingObject(sceneAsset);
                }
                EditorGUI.EndDisabledGroup();
            
                GUILayout.Space(4);
            
                var refreshRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(refreshRect, EditorIcons.Refresh))
                {
                    Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<string> GetScenesDropdown()
        {
            switch (Attribute.ReferenceType)
            {
                case SceneReferenceType.Path:
                    return _allScenes.Select(AssetDatabase.GetAssetPath).Prepend(string.Empty);
                case SceneReferenceType.Name:
                    return _allScenes.Select(scene => scene.name).Prepend(string.Empty);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private SceneAsset GetSceneAsset(string scene, SceneReferenceType sceneReferenceType)
        {
            foreach (var sceneAsset in _allScenes)
            {
                if(sceneAsset == null) continue;
                switch (sceneReferenceType)
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
