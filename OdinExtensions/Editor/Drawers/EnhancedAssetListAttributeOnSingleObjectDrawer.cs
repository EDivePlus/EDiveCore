using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 0, 3001)]
    public class EnhancedAssetListAttributeOnSingleObjectDrawer<TElement> : OdinAttributeDrawer<EnhancedAssetListAttribute, TElement> where TElement : Object
    {
        private static readonly NamedValue[] customFilterMethodArgs = new NamedValue[]
        {
            new NamedValue("asset", typeof(TElement))
        };

        private ValueResolver<bool> customFilterMethod;

        private List<Object> availableAssets = new List<Object>();
        private string[] tags;
        private string[] layerNames;
        private DirectoryInfo assetsFolderLocation;
        private string prettyPath;
        private bool isPopulated = false;
        private double maxSearchDurationPrFrameInMS = 1;
        private int numberOfResultsToSearch = 0;
        private int currentSearchingIndex = 0;
        private IEnumerator populateListRoutine;
        private static GUIStyle padding;
        
        private ValueResolver<string> _pathResolver;
        private ValueResolver<string> _tagsResolver;
        private ValueResolver<string> _layerNamesResolver;
        
        private string _currentPath;

        private static GUIStyle Padding => padding ??= new GUIStyle() {padding = new RectOffset(5, 5, 3, 3)};

        protected override void Initialize()
        {
            var entry = ValueEntry;
            var attribute = Attribute;
            
            _pathResolver = ValueResolver.GetForString(Property, Attribute.Path);
            _tagsResolver = ValueResolver.GetForString(Property, Attribute.Tags);
            _layerNamesResolver = ValueResolver.GetForString(Property, Attribute.LayerNames);
            
            tags = attribute.Tags?.Trim().Split(',').Select(i => i.Trim()).ToArray();
            layerNames = attribute.LayerNames?.Trim().Split(',').Select(i => i.Trim()).ToArray();

            if (attribute.Path != null && !_pathResolver.HasError)
            {
                _currentPath = _pathResolver.GetValue();
                UpdatePath(_currentPath);
            }


            if (attribute.CustomFilterMethod != null)
            {
                customFilterMethod = ValueResolver.Get<bool>(Property, attribute.CustomFilterMethod, customFilterMethodArgs);
            }

            if (Event.current != null)
            {
                // We can get away with lag on load.
                maxSearchDurationPrFrameInMS = 20;
                EnsureListPopulation();
            }

            maxSearchDurationPrFrameInMS = 1;
        }
        
        private void UpdatePath(string newPath)
        {
            newPath = newPath.TrimStart('/', ' ').TrimEnd('/', ' ');
            if (newPath.StartsWith("Assets/"))
            {
                newPath = newPath.Substring(7);
            }
            assetsFolderLocation = new DirectoryInfo(Application.dataPath + "/" + "Assets/" + newPath.Trim('/', ' ') + "/");
            prettyPath = "/" + newPath.Trim('/', ' ').TrimStart('/');
        }

        /// <summary>
        /// Not yet documented.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var entry = ValueEntry;
            var attribute = Attribute;

            var currentValue = (Object)entry.WeakSmartValue;
            
            ValueResolver.DrawErrors(_pathResolver, _layerNamesResolver, _tagsResolver);
            
            if (Attribute.Path != null)
            {
                if (!_pathResolver.HasError)
                {
                    var oldPath = _currentPath;
                    _currentPath = _pathResolver.GetValue();
                    if (oldPath != _currentPath)
                    {
                        UpdatePath(_currentPath);
                        isPopulated = false;
                    }  
                }
            }

            if (customFilterMethod != null && customFilterMethod.HasError)
            {
                customFilterMethod.DrawError();
            }
            else
            {
                EnsureListPopulation();
            }

            SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyPadding);
            {
                SirenixEditorGUI.BeginHorizontalToolbar();
                if (label != null)
                {
                    GUILayout.Label(label);
                }

                GUILayout.FlexibleSpace();
                if (prettyPath != null)
                {
                    GUILayout.Label(prettyPath, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    SirenixEditorGUI.VerticalLineSeparator();
                }

                if (isPopulated)
                {
                    GUILayout.Label(availableAssets.Count + " items", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    GUIHelper.PushGUIEnabled(GUI.enabled && (availableAssets.Count > 0 && (customFilterMethod == null || !customFilterMethod.HasError)));
                }
                else
                {
                    GUILayout.Label("Scanning " + currentSearchingIndex + " / " + numberOfResultsToSearch, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    GUIHelper.PushGUIEnabled(false);
                }

                SirenixEditorGUI.VerticalLineSeparator();

                var drawConflict = entry.Property.ParentValues.Count > 1;
                if (drawConflict == false)
                {
                    var index = availableAssets.IndexOf(currentValue) + 1;
                    if (index > 0)
                    {
                        GUILayout.Label(index.ToString(), SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    }
                    else
                    {
                        drawConflict = true;
                    }
                }

                if (drawConflict)
                {
                    GUILayout.Label("-", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                }

                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleLeft) && isPopulated)
                {
                    var index = availableAssets.IndexOf(currentValue) - 1;
                    index = index < 0 ? availableAssets.Count - 1 : index;
                    entry.WeakSmartValue = availableAssets[index];
                }

                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleDown) && isPopulated)
                {
                    var m = new GenericMenu();
                    var selected = currentValue;
                    var itemsPrPage = 40;
                    var showPages = availableAssets.Count > 50;
                    var page = "";
                    var selectedPage = (availableAssets.IndexOf(entry.WeakSmartValue as Object) / itemsPrPage);
                    for (var i = 0; i < availableAssets.Count; i++)
                    {
                        var obj = availableAssets[i];
                        if (obj != null)
                        {
                            var path = AssetDatabase.GetAssetPath(obj);
                            var name = string.IsNullOrEmpty(path) ? obj.name : path.Substring(7).Replace("/", "\\");
                            var localEntry = entry;

                            if (showPages)
                            {
                                var p = (i / itemsPrPage);
                                page = (p * itemsPrPage) + " - " + Mathf.Min(((p + 1) * itemsPrPage), availableAssets.Count - 1);
                                if (selectedPage == p)
                                {
                                    page += " (contains selected)";
                                }
                                page += "/";
                            }

                            m.AddItem(new GUIContent(page + name), obj == selected, () =>
                           {
                               localEntry.Property.Tree.DelayActionUntilRepaint(() => localEntry.WeakSmartValue = obj);
                           });
                        }
                    }
                    m.ShowAsContext();
                }

                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleRight) && isPopulated)
                {
                    var index = availableAssets.IndexOf(currentValue) + 1;
                    entry.WeakSmartValue = availableAssets[index % availableAssets.Count];
                }

                GUIHelper.PopGUIEnabled();

                SirenixEditorGUI.EndHorizontalToolbar();
                SirenixEditorGUI.BeginVerticalList();
                SirenixEditorGUI.BeginListItem(false, padding);
                CallNextDrawer(null);
                SirenixEditorGUI.EndListItem();
                SirenixEditorGUI.EndVerticalList();
            }
            SirenixEditorGUI.EndIndentedVertical();
        }

        private IEnumerator PopulateListRoutine()
        {
            while (true)
            {
                if (isPopulated)
                {
                    yield return null;
                    continue;
                }

                var seenObjects = new HashSet<Object>();
                var layers = layerNames != null ? layerNames.Select(l => LayerMask.NameToLayer(l)).ToArray() : null;

                availableAssets.Clear();

                IEnumerable<AssetUtilities.AssetSearchResult> allAssets;

                if (prettyPath == null)
                {
                    allAssets = AssetUtilities.GetAllAssetsOfTypeWithProgress(Property.ValueEntry.BaseValueType);
                }
                else
                {
                    allAssets = AssetUtilities.GetAllAssetsOfTypeWithProgress(Property.ValueEntry.BaseValueType, "Assets/" + prettyPath.TrimStart('/'));
                }

                var sw = new Stopwatch();
                sw.Start();

                foreach (var p in allAssets)
                {
                    if (sw.Elapsed.TotalMilliseconds > maxSearchDurationPrFrameInMS)
                    {
                        numberOfResultsToSearch = p.NumberOfResults;
                        currentSearchingIndex = p.CurrentIndex;

                        GUIHelper.RequestRepaint();
                        yield return null;
                        sw.Reset();
                        sw.Start();
                    }

                    var asset = p.Asset;

                    if (asset != null && seenObjects.Add(asset))
                    {
                        var go = asset as Component != null ? (asset as Component).gameObject : asset as GameObject == null ? null : asset as GameObject;

                        var assetName = go == null ? asset.name : go.name;

                        if (Attribute.AssetNamePrefix != null && assetName.StartsWith(Attribute.AssetNamePrefix, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            continue;
                        }

                        if (assetsFolderLocation != null)
                        {
                            var path = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath + "/" + AssetDatabase.GetAssetPath(asset)));
                            if (assetsFolderLocation.HasSubDirectory(path) == false)
                            {
                                continue;
                            }
                        }

                        if (layerNames != null && go == null || tags != null && go == null)
                        {
                            continue;
                        }

                        if (go != null && tags != null && !tags.Contains(go.tag))
                        {
                            continue;
                        }

                        if (go != null && layerNames != null && !layers.Contains(go.layer))
                        {
                            continue;
                        }

                        if (customFilterMethod != null)
                        {
                            customFilterMethod.Context.NamedValues.Set("asset", asset);

                            if (!customFilterMethod.GetValue())
                            {
                                continue;
                            }
                        }
                        
                        availableAssets.Add(asset);
                    }
                }

                isPopulated = true;
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
    }
}
