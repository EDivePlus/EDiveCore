using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ValueNamedValue = Sirenix.OdinInspector.Editor.ValueResolvers.NamedValue;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 0, 2002)]
    public sealed class EnhancedValueDropdownAttributeDrawer : OdinAttributeDrawer<EnhancedValueDropdownAttribute>
    {
        private string _error;
        private GUIContent _label;
        private bool _isList;
        private bool _isListElement;
        private Func<IEnumerable<ValueDropdownItem>> _getValues;
        private Func<IEnumerable<object>> _getSelection;
        private IEnumerable<object> _result;
        private bool _enableMultiSelect;
        private Dictionary<object, string> _nameLookup;
        private ValueResolver<Texture> _iconGetterResolver;
        private ValueResolver<string> _valueLabelGetterResolver;
        private ActionResolver _onListEndGUIResolver;
        private ValueResolver<bool> _showIfResolver;

        private ValueResolver<object> _rawGetter;
        private LocalPersistentContext<bool> _isToggled;

        private const string VALUE_ID = "value";

        protected override void Initialize()
        {
            if (!string.IsNullOrEmpty(Attribute.ShowIf))
            {
                _showIfResolver = ValueResolver.Get<bool>(Property, Attribute.ShowIf);
            }
            
            _rawGetter = ValueResolver.Get<object>(Property, Attribute.ValuesGetter);
            if (Attribute.OnListEndGUI != null)
            {
                _onListEndGUIResolver = ActionResolver.Get(Property, Attribute.OnListEndGUI);
            }
            _isToggled = this.GetPersistentValue("Toggled", SirenixEditorGUI.ExpandFoldoutByDefault);

            _error = _rawGetter.ErrorMessage;
            _isList = Property.ChildResolver is ICollectionResolver;
            _isListElement = Property.Parent != null && Property.Parent.ChildResolver is ICollectionResolver;

            if (Attribute.IconGetter != null)
            {
                var type = Property.ChildResolver is ICollectionResolver collectionResolver ? collectionResolver.ElementType : Property.ValueEntry.BaseValueType;
                _iconGetterResolver = ValueResolver.Get<Texture>(Property, Attribute.IconGetter, new ValueNamedValue(VALUE_ID, type));
            }

            if (Attribute.ValueLabelGetter != null)
            {
                var type = Property.ChildResolver is ICollectionResolver collectionResolver ? collectionResolver.ElementType : Property.ValueEntry.BaseValueType;
                _valueLabelGetterResolver = ValueResolver.Get<string>(Property, Attribute.ValueLabelGetter, new ValueNamedValue(VALUE_ID, type));
            }

            _getSelection = () => Property.ValueEntry.WeakValues.Cast<object>();
            _getValues = () =>
            {
                var value = _rawGetter.GetValue();

                return value == null ? null : (value as IEnumerable)
                    .Cast<object>()
                    .Where(x => x != null)
                    .Select(x =>
                    {
                        if (x is ValueDropdownItem)
                        {
                            return (ValueDropdownItem)x;
                        }

                        if (x is IValueDropdownItem)
                        {
                            var ix = x as IValueDropdownItem;
                            return new ValueDropdownItem(ix.GetText(), ix.GetValue());
                        }

                        return new ValueDropdownItem(null, x);
                    });
            };

            if (!Attribute.DontReloadOnInit)
                ReloadDropdownCollections();
        }

        private void ReloadDropdownCollections()
        {
            if (_error != null)
            {
                return;
            }

            object first = null;
            var value = _rawGetter.GetValue();
            if (value != null)
            {
                first = (value as IEnumerable).Cast<object>().FirstOrDefault();
            }

            var isNamedValueDropdownItems = first is IValueDropdownItem;

            if (isNamedValueDropdownItems)
            {
                var vals = _getValues();
                _nameLookup = new Dictionary<object, string>(new IValueDropdownEqualityComparer(false));
                foreach (var item in vals)
                {
                    _nameLookup[item] = item.Text;
                }
            }
            else
            {
                _nameLookup = null;
            }
        }

        private static IEnumerable<ValueDropdownItem> ToValueDropdowns(IEnumerable<object> query)
        {
            return query.Select(x =>
            {
                if (x is ValueDropdownItem)
                {
                    return (ValueDropdownItem)x;
                }

                if (x is IValueDropdownItem)
                {
                    var ix = x as IValueDropdownItem;
                    return new ValueDropdownItem(ix.GetText(), ix.GetValue());
                }

                return new ValueDropdownItem(null, x);
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
          
            ActionResolver.DrawErrors(_onListEndGUIResolver);
            ValueResolver.DrawErrors(_showIfResolver, _iconGetterResolver, _valueLabelGetterResolver);
            if (_showIfResolver != null && !_showIfResolver.HasError && !_showIfResolver.GetValue())
            {
                CallNextDrawer(label);
                return;
            }

            if (_error != null)
            {
                SirenixEditorGUI.ErrorMessageBox(_error);
                CallNextDrawer(label);
            }
            else if (_isList)
            {
                if (Attribute.DisableListAddButtonBehaviour)
                {
                    CallNextDrawer(label);
                }
                else
                {
                    var oldSelector = CollectionDrawerStaticInfo.NextCustomAddFunction;
                    CollectionDrawerStaticInfo.NextCustomAddFunction = OpenSelector;
                    CallNextDrawer(label);
                    if (_result != null)
                    {
                        AddResult(_result);
                        _result = null;
                    }
                    CollectionDrawerStaticInfo.NextCustomAddFunction = oldSelector;
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
                var changer = Property.ChildResolver as ICollectionResolver;

                if (_enableMultiSelect)
                {
                    changer.QueueClear();
                }

                foreach (var item in query)
                {
                    object[] arr = new object[Property.ParentValues.Count];

                    for (int i = 0; i < arr.Length; i++)
                    {
                        var newValue = Attribute.CopyValues ? SerializationUtility.CreateCopy(item) : item;
                        if (Attribute.OverrideExistingValues || !Equals(arr[i], newValue))
                        {
                            arr[i] = newValue;
                        }
                    }

                    changer.QueueAdd(arr);
                }
            }
            else
            {
                var first = query.FirstOrDefault();
                for (int i = 0; i < Property.ValueEntry.WeakValues.Count; i++)
                {
                    var newValue = Attribute.CopyValues ? SerializationUtility.CreateCopy(first) : first;
                    if (Attribute.OverrideExistingValues || !Equals(Property.ValueEntry.WeakValues[i], newValue))
                    {
                        Property.ValueEntry.WeakValues[i] = newValue;
                    }
                }
            }
        }

        private void DrawDropdown()
        {
            IEnumerable<object> newResult = null;
            if (Attribute.AppendNextDrawer && !_isList)
            {
                GUILayout.BeginHorizontal();
                {
                    var width = 18f;
                    if (_label != null)
                    {
                        width += GUIHelper.BetterLabelWidth;
                    }

                    var t = GUIHelper.TempContent("");
                    if (Property.Info.TypeOfValue == typeof(Type))
                        t.image = GUIHelper.GetAssetThumbnail(null, Property.ValueEntry.WeakSmartValue as Type, false);

                    newResult = EnhancedDropdownSelector<object>.DrawSelectorDropdown(_label, t, ShowSelector, !Attribute.OnlyChangeValueOnConfirm, GUIStyle.none, GUILayoutOptions.Width(width));
                    if (Event.current.type == EventType.Repaint)
                    {
                        var btnRect = GUILayoutUtility.GetLastRect().AlignRight(18);
                        FontAwesomeEditorIcons.SquareCaretDownRegular.Draw(btnRect);
                    }

                    GUILayout.BeginVertical();
                    bool disable = Attribute.DisableGUIInAppendedDrawer;
                    if (disable) GUIHelper.PushGUIEnabled(false);
                    CallNextDrawer(null);
                    if (disable) GUIHelper.PopGUIEnabled();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                var name = Attribute.DrawThumbnailIcon ? $" {GetCurrentValueName()}" : GetCurrentValueName();
                var icon = Attribute.DrawThumbnailIcon ? GetThumbnailIcon(Property.ValueEntry.WeakSmartValue) : null;
                var valueName = GUIHelper.TempContent(name, icon);

                if (Property.Info.TypeOfValue == typeof(Type))
                    valueName.image = GUIHelper.GetAssetThumbnail(null, Property.ValueEntry.WeakSmartValue as Type, false);

                var showChildren = Property.Children.Count > 0 && Attribute.ChildrenDisplayType != DropdownChildrenDisplayType.HideChildren;
                if (showChildren)
                {
                    if (Attribute.ChildrenDisplayType is DropdownChildrenDisplayType.ShowChildrenInFoldout)
                    {
                        _isToggled.Value = SirenixEditorGUI.Foldout(_isToggled.Value, _label, out var valRect);
                        newResult = EnhancedDropdownSelector<object>.DrawSelectorDropdown(valRect, valueName, ShowSelector);

                        if (SirenixEditorGUI.BeginFadeGroup(this, _isToggled.Value))
                        {
                            EditorGUI.indentLevel += Attribute.ChildrenIndent;
                            foreach (var child in Property.Children)
                            {
                                child.Draw(child.Label);
                            }
                            EditorGUI.indentLevel -= Attribute.ChildrenIndent;
                        }
                        SirenixEditorGUI.EndFadeGroup();
                    }
                    else
                    {
                        newResult = EnhancedDropdownSelector<object>.DrawSelectorDropdown(_label, valueName, ShowSelector);
                        EditorGUI.indentLevel += Attribute.ChildrenIndent;
                        foreach (var child in Property.Children)
                        {
                            child.Draw(child.Label);
                        }
                        EditorGUI.indentLevel -= Attribute.ChildrenIndent;
                    }
                }
                else
                {
                    newResult = EnhancedDropdownSelector<object>.DrawSelectorDropdown(_label, valueName, ShowSelector);
                }
            }

            if (newResult != null && newResult.Any())
            {
                AddResult(newResult);
            }
        }
        
        public Texture GetThumbnailIcon(object obj)
        {
            if (_iconGetterResolver != null && !_iconGetterResolver.HasError)
            {
                _iconGetterResolver.Context.NamedValues.Set(VALUE_ID, obj);
                return _iconGetterResolver?.GetValue();
            }
            if (obj is UnityEngine.Object unityObject)
            {
                return GUIHelper.GetAssetThumbnail(unityObject, unityObject.GetType(), false);
            }
            if (obj is Type type)
            {
                return GUIHelper.GetAssetThumbnail(null, type, false);
            }
            if (obj is string str)
            {
                if (File.Exists(str))
                    return InternalEditorUtility.GetIconForFile(str);
                if (Directory.Exists(str))
                    return EditorIcons.UnityFolderIcon;
            }
            return null;
        }

        private void OpenSelector()
        {
            ReloadDropdownCollections();
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

            if (Attribute.AppendNextDrawer && !_isList)
            {
                rect.xMax = GUIHelper.GetCurrentLayoutRect().xMax;
            }

            selector.ShowInPopup(rect, new Vector2(Attribute.DropdownWidth, Attribute.DropdownHeight));
            return selector;
        }

        private GenericSelector<object> CreateSelector()
        {
            bool isUniqueList = Attribute.IsUniqueList; // /*(this.Property.ChildResolver is IOrderedCollectionResolver) == false ||*/ (this.Attribute.IsUniqueList || this.Attribute.ExcludeExistingValuesInList);
            var query = _getValues() ?? Enumerable.Empty<ValueDropdownItem>();

            var isEmpty = query.Any() == false;

            if (!isEmpty)
            {
                if (_isList && Attribute.ExcludeExistingValuesInList || (_isListElement && isUniqueList))
                {
                    var list = query.ToList();
                    var listProperty = Property.FindParent(x => (x.ChildResolver as ICollectionResolver) != null, true);
                    var comparer = new IValueDropdownEqualityComparer(false);

                    listProperty.ValueEntry.WeakValues.Cast<IEnumerable>()
                        .SelectMany(x => x.Cast<object>())
                        .ForEach(x =>
                        {
                            list.RemoveAll(c => comparer.Equals(c, x));
                        });

                    query = list;
                }

                // Update item names in the look up table in case the collection has changed.
                if (_nameLookup != null)
                {
                    foreach (var item in query)
                    {
                        if (item.Value != null)
                        {
                            _nameLookup[item.Value] = item.Text;
                        }
                    }
                }
            }

            var enableSearch = Attribute.NumberOfItemsBeforeEnablingSearch == 0 || (query != null && query.Take(Attribute.NumberOfItemsBeforeEnablingSearch).Count() == Attribute.NumberOfItemsBeforeEnablingSearch);

            var selector = new EnhancedDropdownSelector<object>(Attribute.DropdownTitle, false, query.Select(x => new GenericSelectorItem<object>(x.Text, x.Value)));
            if (_onListEndGUIResolver != null && !_onListEndGUIResolver.HasError)
            {
                _onListEndGUIResolver.DoAction();
            }

            _enableMultiSelect = _isList && isUniqueList && !Attribute.ExcludeExistingValuesInList;

            if (Attribute.FlattenTreeView)
            {
                selector.FlattenedTree = true;
            }

            if (_isList && !Attribute.ExcludeExistingValuesInList && isUniqueList)
            {
                selector.CheckboxToggle = true;
            }
            else if (Attribute.DoubleClickToConfirm == false && !_enableMultiSelect)
            {
                selector.EnableSingleClickToSelect();
            }

            if (_isList && _enableMultiSelect)
            {
                selector.SelectionTree.Selection.SupportsMultiSelect = true;
                selector.DrawConfirmSelectionButton = true;

            }

            selector.SelectionTree.Config.DrawSearchToolbar = enableSearch;

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

            if (_iconGetterResolver != null && !_iconGetterResolver.HasError)
            {
                foreach (var menuItem in selector.SelectionTree.EnumerateTree())
                {
                    _iconGetterResolver.Context.NamedValues.Set(VALUE_ID, menuItem.Value);
                    menuItem.Icon = _iconGetterResolver?.GetValue();
                }
            }
            else
            {
                selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);
            }

            if (Attribute.ExpandAllMenuItems)
            {
                selector.SelectionTree.EnumerateTree(x => x.Toggled = true);
            }

            if (Attribute.SortDropdownItems)
            {
                selector.SelectionTree.SortMenuItemsByName();
            }

            return selector;
        }

        private string GetCurrentValueName()
        {
            if (!EditorGUI.showMixedValue)
            {
                var weakValue = Property.ValueEntry.WeakSmartValue;

                string name = null;
                if (_nameLookup != null && weakValue != null)
                {
                    _nameLookup.TryGetValue(weakValue, out name);
                }

                if (_valueLabelGetterResolver != null && !_valueLabelGetterResolver.HasError)
                {
                    _valueLabelGetterResolver.Context.NamedValues.Set(VALUE_ID, weakValue);
                    name = _valueLabelGetterResolver?.GetValue();
                }

                return new GenericSelectorItem<object>(name, weakValue).GetNiceName();
            }

            return SirenixEditorGUI.MixedValueDashChar;
        }

        public class EnhancedDropdownSelector<T> : GenericSelector<T>
        {
            public Action OnTreeEndGUI;

            public EnhancedDropdownSelector(string title, bool supportsMultiSelect, IEnumerable<GenericSelectorItem<T>> collection) 
                : base(title, supportsMultiSelect, collection) { }

            public EnhancedDropdownSelector(string title, IEnumerable<T> collection, bool supportsMultiSelect, Func<T, string> getMenuItemName = null) 
                : base(title, collection, supportsMultiSelect, getMenuItemName) { }

            public EnhancedDropdownSelector(string title, bool supportsMultiSelect, Func<T, string> getMenuItemName, params T[] collection) 
                : base(title, supportsMultiSelect, getMenuItemName, collection) { }

            public EnhancedDropdownSelector(string title, bool supportsMultiSelect, params T[] collection) 
                : base(title, supportsMultiSelect, collection) { }

            public EnhancedDropdownSelector(string title, params T[] collection) 
                : base(title, collection) { }

            public EnhancedDropdownSelector(params T[] collection) 
                : base(collection) { }

            public EnhancedDropdownSelector(string title, bool supportsMultiSelect, Func<T, string> getMenuItemName, IEnumerable<T> collection) 
                : base(title, supportsMultiSelect, getMenuItemName, collection) { }

            public EnhancedDropdownSelector(string title, bool supportsMultiSelect, IEnumerable<T> collection) 
                : base(title, supportsMultiSelect, collection) { }

            public EnhancedDropdownSelector(string title, IEnumerable<T> collection) 
                : base(title, collection) { }

            public EnhancedDropdownSelector(IEnumerable<T> collection) 
                : base(collection) { }

            protected override void DrawSelectionTree()
            {
                base.DrawSelectionTree();
                OnTreeEndGUI?.Invoke();
            }
        }
    }

    internal class IValueDropdownEqualityComparer : IEqualityComparer<object>
    {
        private bool isTypeLookup;

        public IValueDropdownEqualityComparer(bool isTypeLookup) { this.isTypeLookup = isTypeLookup; }

        public new bool Equals(object x, object y)
        {
            if (x is ValueDropdownItem)
            {
                x = ((ValueDropdownItem) x).Value;
            }

            if (y is ValueDropdownItem)
            {
                y = ((ValueDropdownItem) y).Value;
            }

            if (EqualityComparer<object>.Default.Equals(x, y))
            {
                return true;
            }

            if ((x == null) != (y == null))
            {
                return false;
            }

            if (this.isTypeLookup)
            {
                var tx = x as Type ?? x.GetType();
                var ty = y as Type ?? y.GetType();

                if (tx == ty)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return -1;
            }

            if (obj is ValueDropdownItem)
            {
                obj = ((ValueDropdownItem) obj).Value;
            }

            if (obj == null)
            {
                return -1;
            }

            if (this.isTypeLookup)
            {
                var t = obj as Type ?? obj.GetType();
                return t.GetHashCode();
            }
            else
            {
                return obj.GetHashCode();
            }
        }
    }
}
