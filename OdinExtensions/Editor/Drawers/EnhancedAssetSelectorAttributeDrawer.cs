using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 0, 2002)]
    public sealed class EnhancedAssetSelectorAttributeDrawer<T> : OdinAttributeDrawer<EnhancedAssetSelectorAttribute, T>
    {
        private GUIContent _label;
        private bool _isList;
        private bool _isListElement;
        private Func<IEnumerable<ValueDropdownItem>> _getValues;
        private Func<IEnumerable<object>> _getSelection;
        private Type _elementOrBaseType;
        private bool _isString;
        private IEnumerable<object> _result;
        private bool _enableMultiSelect;
        
        protected override void Initialize()
        {
            _isList = Property.ChildResolver is IOrderedCollectionResolver;
            _isListElement = Property.Parent != null && Property.Parent.ChildResolver is IOrderedCollectionResolver;
            _getSelection = () => Property.ValueEntry.WeakValues.Cast<object>();
            _elementOrBaseType = _isList ?
                (Property.ChildResolver as IOrderedCollectionResolver)?.ElementType :
                Property.ValueEntry.BaseValueType;
            _isString = _elementOrBaseType == typeof(string);

            _getValues = () =>
            {
                var filter = Attribute.Filter ?? "";

                if (string.IsNullOrEmpty(filter) && !typeof(Component).IsAssignableFrom(_elementOrBaseType) && !_elementOrBaseType.IsInterface)
                {
                    filter = "t:" + _elementOrBaseType.Name;
                }

                var assetGuids = AssetDatabase.FindAssets(filter, Attribute.SearchInFolders ?? Array.Empty<string>());

                return assetGuids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Distinct()
                    .SelectMany(x =>
                    {
                        IEnumerable<UnityEngine.Object> objs;
                        if (x.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".scene", StringComparison.InvariantCultureIgnoreCase))
                        {
                            objs = Enumerable.Repeat(AssetDatabase.LoadAssetAtPath(x, typeof(UnityEngine.Object)), 1);
                        }
                        else
                        {
                            objs = AssetDatabase.LoadAllAssetsAtPath(x);
                        }

                        return objs.Where(obj => obj != null && _elementOrBaseType.IsAssignableFrom(obj.GetType()))
                            .Select(obj => new { o = obj, p = x });
                    })
                    .Select(x => new ValueDropdownItem()
                    {
                        Text = x.p + (AssetDatabase.IsMainAsset(x.o) ? "" : "/" + x.o.name),
                        Value = _isString ? x.p : x.o
                    });
            };
        }

        private static IEnumerable<ValueDropdownItem> ToValueDropdowns(IEnumerable<object> query)
        {
            return query.Select(x =>
            {
                return x switch
                {
                    ValueDropdownItem item => item,
                    IValueDropdownItem ix => new ValueDropdownItem(ix.GetText(), ix.GetValue()),
                    _ => new ValueDropdownItem(null, x)
                };
            });
        }

        /// <summary>
        /// Draws the property with GUILayout support. This method is called by DrawPropertyImplementation if the GUICallType is set to GUILayout, which is the default.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            _label = label;

            if (Property.ValueEntry == null)
            {
                CallNextDrawer(label);
                return;
            }

            if (_isList)
            {
                if (Attribute.DisableListAddButtonBehaviour)
                {
                    CallNextDrawer(label);
                }
                else
                {
                    CollectionDrawerStaticInfo.NextCustomAddFunction = OpenSelector;
                    CallNextDrawer(label);
                    if (_result != null)
                    {
                        AddResult(_result);
                        _result = null;
                    }
                }
            }
            else
            {
                if (Attribute.DrawDropdownForListElements || !_isListElement)
                {
                    DrawDropdown();
                }
                else
                {
                    CallNextDrawer(label);
                }
            }
        }

        private void AddResult(IEnumerable<object> query)
        {
            if (_isList)
            {
                var changer = Property.ChildResolver as IOrderedCollectionResolver;

                if (_enableMultiSelect)
                {
                    changer?.QueueClear();
                }

                foreach (var item in query)
                {
                    var arr = new object[Property.ParentValues.Count];

                    for (var i = 0; i < arr.Length; i++)
                    {
                        arr[i] = item;
                    }

                    changer?.QueueAdd(arr);
                }
            }
            else
            {
                var first = query.FirstOrDefault();
                for (var i = 0; i < Property.ValueEntry.WeakValues.Count; i++)
                {
                    Property.ValueEntry.WeakValues[i] = first;
                }
            }
        }

        private void DrawDropdown()
        {
            IEnumerable<object> newResult = null;

            SirenixEditorGUI.BeginVerticalPropertyLayout(_label);
            EditorGUILayout.BeginHorizontal(GUIStyle.none);

            var caretRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,
                GUILayoutOptions.Width(18).ExpandWidth(false));
            newResult = GenericSelector<object>.DrawSelectorDropdown(caretRect, GUIContent.none, ShowSelector);
            if (Event.current.type == EventType.Repaint)
                FontAwesomeEditorIcons.SquareCaretDownRegular.Draw(caretRect);

            EditorGUILayout.BeginVertical();
            CallNextDrawer(null);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            SirenixEditorGUI.EndVerticalPropertyLayout();

            if (newResult != null && newResult.Any())
            {
                AddResult(newResult);
            }
        }

        private void OpenSelector()
        {
            var rect = new Rect(Event.current.mousePosition, Vector2.zero);
            var selector = ShowSelector(rect);
            selector.SelectionConfirmed += x => _result = x;
        }

        private OdinSelector<object> ShowSelector(Rect rect)
        {
            var selector = CreateSelector();

            rect.x = (int)rect.x;
            rect.y = (int)rect.y;
            rect.width = (int)rect.width;
            rect.height = (int)rect.height;

            if (!_isList)
            {
                rect.xMax = GUIHelper.GetCurrentLayoutRect().xMax;
            }

            selector.ShowInPopup(rect, new Vector2(Attribute.DropdownWidth, Attribute.DropdownHeight));
            return selector;
        }

        private GenericSelector<object> CreateSelector()
        {
            Attribute.IsUniqueList = Attribute.IsUniqueList || Attribute.ExcludeExistingValuesInList;
            var query = _getValues() ?? Enumerable.Empty<ValueDropdownItem>();

            var isEmpty = !query.Any();

            if (!isEmpty)
            {
                if (_isList && Attribute.ExcludeExistingValuesInList || (_isListElement && Attribute.IsUniqueList))
                {
                    var list = query.ToList();
                    var listProperty = Property.FindParent(x => (x.ChildResolver as IOrderedCollectionResolver) != null, true);
                    var comparer = new IValueDropdownEqualityComparer(false);

                    listProperty.ValueEntry.WeakValues.Cast<IEnumerable>()
                        .SelectMany(x => x.Cast<object>())
                        .ForEach(x =>
                        {
                            list.RemoveAll(c => comparer.Equals(c, x));
                        });

                    query = list;
                }
            }

            var selector = new GenericSelector<object>(Attribute.DropdownTitle, false, query.Select(x => new GenericSelectorItem<object>(x.Text, x.Value)));

            _enableMultiSelect = _isList && Attribute.IsUniqueList && !Attribute.ExcludeExistingValuesInList;

            if (Attribute.FlattenTreeView)
            {
                selector.FlattenedTree = true;
            }

            if (_isList && !Attribute.ExcludeExistingValuesInList && Attribute.IsUniqueList)
            {
                selector.CheckboxToggle = true;
            }
            else if (!_enableMultiSelect)
            {
                selector.EnableSingleClickToSelect();
            }

            if (_isList && _enableMultiSelect)
            {
                selector.SelectionTree.Selection.SupportsMultiSelect = true;
                selector.DrawConfirmSelectionButton = true;
            }

            selector.SelectionTree.Config.DrawSearchToolbar = true;

            var selection = Enumerable.Empty<object>();

            if (!_isList)
            {
                selection = _getSelection();
            }
            else if (_enableMultiSelect)
            {
                selection = _getSelection().SelectMany(x => (x as IEnumerable).Cast<object>());
            }

            selector.SetSelection(selection);
            selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);

            if (Attribute.ExpandAllMenuItems)
            {
                selector.SelectionTree.EnumerateTree(x => x.Toggled = true);
            }

            return selector;
        }
    }
}
