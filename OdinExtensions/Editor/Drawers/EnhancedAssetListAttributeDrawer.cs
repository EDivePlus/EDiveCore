using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
#pragma warning disable
    [DrawerPriority(DrawerPriorityLevel.AttributePriority)]
    public sealed class EnhancedAssetListAttributeDrawer<TList, TElement> : OdinAttributeDrawer<EnhancedAssetListAttribute, TList>, IDefinesGenericMenuItems, IDisposable
        where TList : IList<TElement> where TElement : ScriptableObject
    {
        private static readonly NamedValue[] customFilterMethodArgs = new NamedValue[]
        {
            new NamedValue("asset", typeof(TElement))
        };

        private AssetList _assetList;
        private PropertyTree _propertyTree;
        private InspectorProperty _listProperty;
        private ValueResolver<string> _pathResolver;
        private ValueResolver<string> _tagsResolver;
        private ValueResolver<string> _layerNamesResolver;

        private string _currentPath;
        private string _currentTags;
        private string _currentLayerNames;
        
        protected override void Initialize()
        {
            var property = Property;
            var entry = ValueEntry;
            var attribute = Attribute;

            _pathResolver = ValueResolver.GetForString(Property, Attribute.Path);
            _tagsResolver = ValueResolver.GetForString(Property, Attribute.Tags);
            _layerNamesResolver = ValueResolver.GetForString(Property, Attribute.LayerNames);

            _assetList = new AssetList();
            _assetList.AutoPopulate = attribute.AutoPopulate;
            _assetList.ShowInlineEditor = attribute.ShowInlineEditor;
            _assetList.AssetNamePrefix = attribute.AssetNamePrefix;
            _assetList.List = entry;
            _assetList.CollectionResolver = property.ChildResolver as IOrderedCollectionResolver;
            _assetList.Property = entry.Property;

            if (attribute.Tags != null && _tagsResolver.HasError)
            {
                UpdateTags(_tagsResolver.GetValue());
            }

            if (attribute.LayerNames != null && _layerNamesResolver.HasError)
            {
                UpdateLayerNames(_layerNamesResolver.GetValue());
            }

            if (attribute.Path != null && !_pathResolver.HasError)
            {
                _currentPath = _pathResolver.GetValue();
                UpdatePath(_currentPath != null ? _currentPath.Split(';') : new string[]{});
            }

            if (attribute.CustomFilterMethod != null)
            {
                _assetList.CustomFilterMethod = ValueResolver.Get<bool>(Property, attribute.CustomFilterMethod, customFilterMethodArgs);
            }

            if (Event.current != null)
            {
                // We can get away with lag on load.
                _assetList.MaxSearchDurationPrFrameInMS = 20;
                _assetList.EnsureListPopulation();
            }

            _assetList.MaxSearchDurationPrFrameInMS = 1;

            _propertyTree = PropertyTree.Create(_assetList);
            _propertyTree.UpdateTree();

            _listProperty = _propertyTree.GetPropertyAtPath("toggleableAssets");
        }

        public void Dispose()
        {
            _propertyTree?.Dispose();
            _listProperty?.Dispose();
        }

        private void UpdateTags(string newTags)
        {
            if (string.IsNullOrWhiteSpace(newTags))
            {
                _assetList.Tags = null;
                return;
            }

            _assetList.Tags = newTags.Trim().Split(',').Select(i => i.Trim()).ToArray();
        }

        private void UpdateLayerNames(string newLayerNames)
        {
            if (string.IsNullOrWhiteSpace(newLayerNames))
            {
                _assetList.LayerNames = null;
                return;
            }

            _assetList.LayerNames = newLayerNames.Trim().Split(',').Select(i => i.Trim()).ToArray();
        }

        private void UpdatePath(params string[] newPaths)
        {
            newPaths = newPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (newPaths.Length == 0)
            {
                _assetList.AssetsFolderLocations = null;
                _assetList.PrettyPaths = null;
                return;
            }

            var assetsFolderLocations = new List<DirectoryInfo>();
            var prettyPaths = new List<string>();
            
            foreach (var newPath in newPaths)
            {
                var path = newPath.TrimStart('/', ' ').TrimEnd('/', ' ');
                if (path.StartsWith("Assets/"))
                {
                    path = path.Substring(7);
                }
                assetsFolderLocations.Add(new DirectoryInfo(Application.dataPath + "/Assets/" + path.Trim('/', ' ') + "/"));
                prettyPaths.Add("/" + path.Trim('/', ' ').TrimStart('/'));
            }
            
            _assetList.AssetsFolderLocations = assetsFolderLocations.ToArray();
            _assetList.PrettyPaths = prettyPaths.ToArray();
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var property = Property;
            var entry = ValueEntry;
            var attribute = Attribute;

            ValueResolver.DrawErrors(_pathResolver, _layerNamesResolver, _tagsResolver);
            
            if (property.ValueEntry.WeakSmartValue == null)
                return;

            if (Attribute.Path != null)
            {
                if (!_pathResolver.HasError)
                {
                    var oldPath = _currentPath;
                    _currentPath = _pathResolver.GetValue();
                    if (oldPath != _currentPath)
                    {
                        UpdatePath(_currentPath != null ? _currentPath.Split(';') : new string[]{});
                        _assetList.Rescan();
                    }  
                }
            }
            
            if (Attribute.Tags != null)
            {
                if (!_tagsResolver.HasError)
                {
                    var oldTags = _currentTags;
                    _currentTags = _tagsResolver.GetValue();
                    if (oldTags != _currentTags)
                    {
                        UpdateTags(_currentTags);
                        _assetList.Rescan();
                    }  
                }
            }
            
            if (Attribute.LayerNames != null)
            {
                if (!_layerNamesResolver.HasError)
                {
                    var oldLayerNames = _currentLayerNames;
                    _currentLayerNames = _layerNamesResolver.GetValue();
                    if (oldLayerNames != _currentLayerNames)
                    {
                        UpdateLayerNames(_currentLayerNames);
                        _assetList.Rescan();
                    }  
                }
            }

            _propertyTree.GetRootProperty(0).Label = label;

            _listProperty.State.Enabled = Property.State.Enabled;
            _listProperty.State.Expanded = Property.State.Expanded;

            if (Event.current.type == EventType.Layout)
            {
                _assetList.Property = entry.Property;
                _assetList.EnsureListPopulation();
                _assetList.SetToggleValues();
            }

            if (_assetList.CustomFilterMethod != null && _assetList.CustomFilterMethod.HasError)
            {
                _assetList.CustomFilterMethod.DrawError();
            }

            _assetList.Property = entry.Property;
            _propertyTree.Draw(false);

            Property.State.Enabled = _listProperty.State.Enabled;
            Property.State.Expanded = _listProperty.State.Expanded;

            if (Event.current.type == EventType.Used)
            {
                _assetList.UpdateList();
            }
        }
        
        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            if (_assetList == null)
            {
                return;
            }

            if (_assetList.List.SmartValue.Count != _assetList.ToggleableAssets.Count)
            {
                genericMenu.AddItem(new GUIContent("Include All"), false, () => { _assetList.UpdateList(true); });
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent("Include All"));
            }
        }

        [Serializable, ShowOdinSerializedPropertiesInInspector]
        private class AssetList
        {
            [HideInInspector]
            public bool AutoPopulate;
            
            [HideInInspector]
            public bool ShowInlineEditor;

            [HideInInspector]
            public string AssetNamePrefix;

            [HideInInspector]
            public string[] LayerNames;

            [HideInInspector]
            public string[] Tags;

            [HideInInspector]
            public IPropertyValueEntry<TList> List;

            [HideInInspector]
            public IOrderedCollectionResolver CollectionResolver;

            [HideInInspector]
            public DirectoryInfo[] AssetsFolderLocations;

            [HideInInspector]
            public string[] PrettyPaths;

            [HideInInspector]
            public ValueResolver<bool> CustomFilterMethod;

            [HideInInspector]
            public InspectorProperty Property;

            public List<ToggleableAsset> ToggleableAssets
            {
                get
                {
                    return toggleableAssets;
                }
            }

            [SerializeField]
            [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true, DraggableItems = false, OnTitleBarGUI = "OnListTitlebarGUI", ShowItemCount = false)]
            [DisableContextMenu(true, true)]
            [HideReferenceObjectPicker]
            private List<ToggleableAsset> toggleableAssets = new List<ToggleableAsset>();

            [SerializeField]
            [HideInInspector]
            private HashSet<TElement> toggledAssets = new HashSet<TElement>();

            [SerializeField]
            [HideInInspector]
            private Dictionary<TElement, ToggleableAsset> toggleableAssetLookup = new Dictionary<TElement, ToggleableAsset>();

            [NonSerialized]
            public bool IsPopulated = false;

            [NonSerialized]
            public double MaxSearchDurationPrFrameInMS = 1;

            [NonSerialized]
            public int NumberOfResultsToSearch = 0;

            [NonSerialized]
            public int TotalSearchCount = 0;

            [NonSerialized]
            public int CurrentSearchingIndex = 0;

            [NonSerialized]
            private IEnumerator populateListRoutine;

            private IEnumerator PopulateListRoutine()
            {
                while (true)
                {
                    if (IsPopulated)
                    {
                        yield return null;
                        continue;
                    }

                    var seenObjects = new HashSet<Object>();
                    toggleableAssets.Clear();
                    toggleableAssetLookup.Clear();

                    IEnumerable<AssetUtilities.AssetSearchResult> allAssets;
#pragma warning disable CS0618 // Type or member is obsolete
                    if (PrettyPaths == null || PrettyPaths.Length == 0)
                    {
                        allAssets = EnhancedAssetUtilities.GetAllAssetsOfTypeWithProgress(typeof(TElement), null);
                    }
                    else
                    {
                        allAssets = EnhancedAssetUtilities.GetAllAssetsOfTypeWithProgress(typeof(TElement), PrettyPaths.Select(p => "Assets/" + p.TrimStart('/')).ToArray());
                    }
#pragma warning restore CS0618 // Type or member is obsolete

                    var layers = LayerNames?.Select(LayerMask.NameToLayer).ToArray();

                    var sw = new Stopwatch();
                    sw.Start();

                    foreach (var p in allAssets)
                    {
                        if (sw.Elapsed.TotalMilliseconds > MaxSearchDurationPrFrameInMS)
                        {
                            NumberOfResultsToSearch = p.NumberOfResults;
                            CurrentSearchingIndex = p.CurrentIndex;

                            GUIHelper.RequestRepaint();
                            //this.SetToggleValues(startIndex);

                            yield return null;
                            sw.Reset();
                            sw.Start();
                        }

                        var asset = p.Asset;

                        if (asset != null && seenObjects.Add(asset))
                        {
                            var go = asset as Component != null ? (asset as Component).gameObject : asset as GameObject == null ? null : asset as GameObject;

                            var assetName = go == null ? asset.name : go.name;

                            if (AssetNamePrefix != null && assetName.StartsWith(AssetNamePrefix, StringComparison.InvariantCultureIgnoreCase) == false)
                            {
                                continue;
                            }

                            if (AssetsFolderLocations != null)
                            {
                                var isInDirectory = false;
                                var path = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath + "/" + AssetDatabase.GetAssetPath(asset)));
                                foreach (var assetsFolderLocation in AssetsFolderLocations)
                                {
                                    if (assetsFolderLocation.HasSubDirectory(path))
                                    {
                                        isInDirectory = true;
                                    }
                                }
                                if(!isInDirectory)
                                    continue;
                            }

                            if (LayerNames != null && go == null || Tags != null && go == null)
                            {
                                continue;
                            }

                            if (go != null && Tags != null && !Tags.Contains(go.tag))
                            {
                                continue;
                            }

                            if (go != null && LayerNames != null && !layers.Contains(go.layer))
                            {
                                continue;
                            }

                            if (toggleableAssetLookup.ContainsKey(asset as TElement))
                            {
                                continue;
                            }

                            if (CustomFilterMethod != null)
                            {
                                CustomFilterMethod.Context.NamedValues.Set("asset", asset);

                                if (!CustomFilterMethod.GetValue())
                                {
                                    continue;
                                }
                            }
                            
                            var toggleable = new ToggleableAsset(asset as TElement, AutoPopulate, ShowInlineEditor);

                            toggleableAssets.Add(toggleable);
                            toggleableAssetLookup.Add(asset as TElement, toggleable);
                        }
                    }

                    SetToggleValues();

                    IsPopulated = true;
                    GUIHelper.RequestRepaint();
                    yield return null;
                }
            }

            public void EnsureListPopulation()
            {
                if (Event.current.type == EventType.Layout)
                {
                    populateListRoutine ??= PopulateListRoutine();
                    populateListRoutine.MoveNext();
                }
            }

            public void SetToggleValues(int startIndex = 0)
            {
                if (List.SmartValue == null)
                {
                    return;
                }

                for (var i = startIndex; i < toggleableAssets.Count; i++)
                {
                    if (toggleableAssets[i] == null || toggleableAssets[i].Object == null)
                    {
                        Rescan();
                        break;
                    }

                    toggleableAssets[i].Toggled = false;
                }

                for (var i = List.SmartValue.Count - 1; i >= startIndex; i--)
                {
                    var asset = List.SmartValue[i];
                    if (asset == null)
                    {
                        CollectionResolver.QueueRemoveAt(i);
                    }
                    else
                    {
                        if (toggleableAssetLookup.TryGetValue(asset, out var toggleable))
                        {
                            toggleable.Toggled = true;
                        }
                        else
                        {
                            if (IsPopulated)
                            {
                                CollectionResolver.QueueRemoveAt(i);
                            }
                        }
                    }
                }
            }

            public void Rescan()
            {
                IsPopulated = false;
            }

            private void OnListTitlebarGUI()
            {
                if (IsPopulated)
                {
                    GUILayout.Label(List.SmartValue.Count + " / " + toggleableAssets.Count, SirenixGUIStyles.CenteredGreyMiniLabel);
                }
                else
                {
                    GUILayout.Label("Scanning " + CurrentSearchingIndex + " / " + NumberOfResultsToSearch, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                }
                var disableGUI = !IsPopulated;

                if (disableGUI)
                {
                    GUIHelper.PushGUIEnabled(false);
                }

                GUIHelper.PushGUIEnabled(List.SmartValue.Count != ToggleableAssets.Count);
                if (!AutoPopulate && SirenixEditorGUI.ToolbarButton(EditorIcons.Stretch) && IsPopulated)
                {
                    UpdateList(true);
                }
                GUIHelper.PopGUIEnabled();
                
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh) && IsPopulated)
                {
                    Rescan();
                }

                if (EnhancedAssetUtilities.CanCreateNewAsset<TElement>())
                {
                    if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus) && IsPopulated)
                    {
                        string path = null;
                        if (PrettyPaths != null && PrettyPaths.Length > 0)
                        {
                            path = PrettyPaths.FirstOrDefault();
                        }
                        if (path == null)
                        {
                            var lastAsset = List.SmartValue.Count > 0 ? List.SmartValue[List.SmartValue.Count - 1] : null;
                            if (lastAsset == null)
                            {
                                var lastToggleable = toggleableAssets.LastOrDefault();
                                if (lastToggleable != null)
                                {
                                    lastAsset = lastToggleable.Object;
                                }
                            }
                            if (lastAsset != null)
                            {
                                path = EnhancedAssetUtilities.GetAssetLocation(lastAsset);
                            }
                        }

                        OdinExtensionUtils.DrawSubtypeDropDownOrCall(typeof(TElement), CreateInstance);
                        void CreateInstance(Type type)
                        {
                            Property.Tree.DelayActionUntilRepaint(() =>
                            {
                                OdinExtensionUtils.CreateNewInstanceOfType<TElement>(type, path);
                                Rescan();
                            });
                        }
                    }
                }

                if (disableGUI)
                {
                    GUIHelper.PopGUIEnabled();
                }
            }

            public void UpdateList()
            {
                UpdateList(false);
            }

            public void UpdateList(bool includeAll)
            {
                if (List.SmartValue == null)
                {
                    return;
                }

                toggledAssets.Clear();
                for (var i = 0; i < toggleableAssets.Count; i++)
                {
                    if (includeAll || AutoPopulate || toggleableAssets[i].Toggled)
                    {
                        toggledAssets.Add(toggleableAssets[i].Object);
                    }
                }

                for (var i = List.SmartValue.Count - 1; i >= 0; i--)
                {
                    if (List.SmartValue[i] == null)
                    {
                        CollectionResolver.QueueRemoveAt(i);
                        Rescan();
                    }
                    else if (toggledAssets.Contains(List.SmartValue[i]) == false)
                    {
                        if (IsPopulated)
                        {
                            CollectionResolver.QueueRemoveAt(i);
                        }
                    }
                    else
                    {
                        toggledAssets.Remove(List.SmartValue[i]);
                    }
                }

                foreach (var asset in toggledAssets.GFIterator())
                {
                    CollectionResolver.QueueAdd(Enumerable.Repeat(asset, List.ValueCount).ToArray());
                }

                toggledAssets.Clear();
            }
        }

        [Serializable]
        private class ToggleableAsset
        {
            [HideInInspector]
            public bool AutoToggle;
            
            [HideInInspector]
            [UsedImplicitly]
            public bool ShowInlineEditor;
            
            public bool Toggled;
            
            [EnhancedInlineEditor(Condition = "$ShowInlineEditor", LabelVisibility = LabelVisibilityMode.Hidden, HideFrame = true, ContentIndent = 1)]
            public TElement Object;

            public ToggleableAsset(TElement obj, bool autoToggle, bool showInlineEditor = false)
            {
                AutoToggle = autoToggle;
                Object = obj;
                ShowInlineEditor = showInlineEditor;
            }
        }

        private sealed class AssetInstanceDrawer : OdinValueDrawer<ToggleableAsset>
        {
            private InspectorProperty _objectProperty;

            protected override void Initialize()
            {
                _objectProperty = Property.Children.Get(nameof(ToggleableAsset.Object));
            }

            protected override void DrawPropertyLayout(GUIContent label)
            {
                var entry = ValueEntry;
                if (entry.SmartValue.AutoToggle)
                {
                    _objectProperty.Draw(null);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(16));
                        {
                            EditorGUI.BeginChangeCheck();
                            entry.SmartValue.Toggled = EditorGUILayout.Toggle(entry.SmartValue.Toggled);
                            if (EditorGUI.EndChangeCheck())
                            {
                                entry.ApplyChanges(); 
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        GUIHelper.PushGUIEnabled(entry.SmartValue.Toggled);
                        _objectProperty.Draw(null);
                        GUIHelper.PopGUIEnabled();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
