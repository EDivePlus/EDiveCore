using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.EditorUtils.SubAssets
{
    public class SubAssetEditorWindow : OdinEditorWindow, IHasCustomMenu
    {
        [ShowInInspector]
        [LabelWidth(100)]
        private Object _currentAsset;

        [ShowInInspector]
        [PropertySpace]
        [DisableContextMenu]
        [CustomValueDrawer(nameof(CustomSubAssetValueDrawer))]
        [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        private ObservableCollection<Object> _subAssets = new ObservableCollection<Object>();

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value; 
                if (!_isLocked)
                {
                    OnSelectionChanged();
                }
            }
        }
        
        private GUIStyle _lockButtonStyle;
        private GUIStyle LockButtonStyle => _lockButtonStyle ??= "IN LockButton";
        
        [MenuItem("Assets/SubAsset Editor")]
        public static void OpenFromMenu() 
        {
            GetWindow<SubAssetEditorWindow>("SubAsset Editor").Show();
        }

        protected override void Initialize()
        {
            base.Initialize();
            _subAssets.CollectionChanged += OnSubAssetsChanged;
            OnSelectionChanged();
            Selection.selectionChanged += OnSelectionChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            // On select new asset.
            var active = Selection.activeObject;
            if (!IsLocked && active && _currentAsset != active && !(active is SceneAsset))
            {
                if(AssetDatabase.IsMainAsset(active))
                    SetAsset(active);
                else
                {
                    var mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(active)); 
                    SetAsset(mainAsset);
                }
            }
        }
        
        private void SetAsset(Object asset)
        {
            _currentAsset = asset;
            RefreshCurrent();
        }

        private void RefreshCurrent()
        {
            _subAssets.CollectionChanged -= OnSubAssetsChanged;
            var assetPath = AssetDatabase.GetAssetPath(_currentAsset);
            AssetDatabase.LoadMainAssetAtPath(assetPath);
            _subAssets.Clear();
            _subAssets.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .Where(x => x != _currentAsset &&  x != null && 0 == (x.hideFlags & HideFlags.HideInHierarchy))
                .Distinct()
                .ToList());
            _subAssets.CollectionChanged += OnSubAssetsChanged;
        }
        
        private void OnSubAssetsChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (notifyCollectionChangedEventArgs.Action != NotifyCollectionChangedAction.Add) return;
            var newItems = new List<Object>();
            foreach (var newItem in notifyCollectionChangedEventArgs.NewItems)
            {
                if (!(newItem is Object newObject)) continue;
                newItems.Add(newObject);
            }
            SubAssetUtility.MoveAssets(newItems, _currentAsset);
            RefreshCurrent();
        }
        
        private Object CustomSubAssetValueDrawer(Object value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            if (value == null) return value;
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(new GUIContent(AssetPreview.GetMiniThumbnail(value)), GUILayout.Width(20));
            EditorGUI.BeginChangeCheck();
            value.name = EditorGUILayout.DelayedTextField(value.name);
            if (EditorGUI.EndChangeCheck())
            {
                AssetDatabase.SaveAssets();
            }
            
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            callNextDrawer(label);
            EditorGUILayout.EndVertical();
            
            if (SirenixEditorGUI.IconButton(EditorIcons.Redo, 18, 18, "Export"))
            {
                SubAssetUtility.ExportSubAssetToCurrentFolder(value);
            }
            
            if (SirenixEditorGUI.IconButton(EditorIcons.X, 18, 18, "Delete"))
            {
                SubAssetUtility.DeleteSubAsset(value);
            }
            
            EditorGUILayout.EndHorizontal();
            return value;
        }
        
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), IsLocked, () => {
                IsLocked = !IsLocked;
            });
        }
        
        private void ShowButton(Rect buttonPos)
        {
            IsLocked = GUI.Toggle(buttonPos, IsLocked, GUIContent.none,  LockButtonStyle);
        }
    }
}
