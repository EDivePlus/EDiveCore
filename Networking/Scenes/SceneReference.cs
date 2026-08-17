// Author: František Holubec

using System;
using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using UnityEngine;
#if ADDRESSABLES
using EDIVE.AddressableAssets;
#endif
#if UNITY_EDITOR
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Networking.Scenes
{
    public enum SceneKind
    {
        Direct,
        Addressable
    }

    [Serializable]
    public struct SceneReference
    {
        [HorizontalGroup(26)]
        [SerializeField]
        [HideLabel]
        [CustomValueDrawer("DrawKind")]
        [OnValueChanged("OnKindChanged")]
        private SceneKind _Kind;

        [HorizontalGroup]
        [SerializeField]
        [HideLabel]
        [ShowIf(nameof(_Kind), SceneKind.Direct)]
        private SceneField _DirectScene;

#if ADDRESSABLES
        [HorizontalGroup]
        [SerializeField]
        [HideLabel]
        [ShowIf(nameof(_Kind), SceneKind.Addressable)]
        private SceneAssetReference _AddressableScene;
#endif

        public SceneKind Kind => _Kind;

        public bool IsValid
        {
            get
            {
                switch (_Kind)
                {
#if ADDRESSABLES
                    case SceneKind.Addressable:
                        return _AddressableScene != null && _AddressableScene.RuntimeKeyIsValid();
#endif
                    case SceneKind.Direct:
                        return _DirectScene.IsValid;
                    default:
                        return false;
                }
            }
        }

        public SceneKey Key
        {
            get
            {
                switch (_Kind)
                {
#if ADDRESSABLES
                    case SceneKind.Addressable:
                        return new SceneKey(_AddressableScene?.AssetGUID, true);
#endif
                    case SceneKind.Direct:
                        return new SceneKey(_DirectScene.Name, false);
                    default:
                        throw new InvalidOperationException($"Invalid NetworkSceneKind: {_Kind}");
                }
            }
        }

#if UNITY_EDITOR
        public SceneAsset EditorDirectSceneAsset => _DirectScene.EditorSceneAsset;
#if ADDRESSABLES
        public SceneAsset EditorAddressableSceneAsset => _AddressableScene?.editorAsset;
#endif

        public static SceneReference FromSceneAsset(SceneAsset asset)
        {
            return new SceneReference
            {
                _Kind = SceneKind.Direct,
                _DirectScene = new SceneField(asset)
            };
        }

#if ADDRESSABLES
        public static SceneReference FromAddressableSceneAsset(SceneAsset asset)
        {
            var reference = new SceneReference
            {
                _Kind = SceneKind.Addressable,
                _AddressableScene = new SceneAssetReference()
            };
            reference._AddressableScene.SetEditorAsset(asset);
            return reference;
        }
#endif

        private SceneKind DrawKind(SceneKind value, InspectorProperty property)
        {
            var icon = value switch
            {
                SceneKind.Addressable => FontAwesomeEditorIcons.AtSolid,
                SceneKind.Direct => CustomEditorIcons.GameObject,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            
            var tooltip = value switch
            {
                SceneKind.Addressable => "Addressable scene",
                SceneKind.Direct => "Direct scene",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };

            var iconRect = GUILayoutUtility.GetRect(20, 18, SirenixGUIStyles.Button, GUILayout.ExpandWidth(false), GUILayout.Width(20));
            if (SirenixEditorGUI.IconButton(iconRect, icon, tooltip))
            {
                var menu = new GenericMenu();
                AddKindItem(menu, property, value, SceneKind.Direct);
                AddKindItem(menu, property, value, SceneKind.Addressable);
                menu.DropDown(iconRect);
            }
            return value;
        }

        private static void AddKindItem(GenericMenu menu, InspectorProperty property, SceneKind current, SceneKind kind)
        {
            menu.AddItem(new GUIContent(kind.ToString()), current == kind,
                () => property.Tree.DelayAction(() => ((IPropertyValueEntry<SceneKind>) property.ValueEntry).SmartValue = kind));
        }

        private void OnKindChanged(InspectorProperty property)
        {
#if ADDRESSABLES
            switch (_Kind)
            {
                case SceneKind.Direct:
                    if (EditorAddressableSceneAsset)
                        _DirectScene = new SceneField(EditorAddressableSceneAsset);
                    break;
                case SceneKind.Addressable:
                    if (_DirectScene.EditorSceneAsset)
                    {
                        _AddressableScene ??= new SceneAssetReference();
                        _AddressableScene.SetEditorAsset(_DirectScene.EditorSceneAsset);
                    }
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            property.MarkSerializationRootDirty();
#endif
        }
#endif
    }
}
