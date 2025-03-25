using System;
using System.Collections.Generic;
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
using UnityEngine;
using ActionNamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

#pragma warning disable

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class EnhancedTableListAttributeDrawer : OdinAttributeDrawer<EnhancedTableListAttribute>, IDefinesGenericMenuItems
    {

        private const string INDEX_ARGUMENT_ID = "index";
        private const string ELEMENT_ARGUMENT_ID = "element";
        
        private IOrderedCollectionResolver _resolver;
        private LocalPersistentContext<bool> _isPagingExpanded;
        private LocalPersistentContext<Vector2> _scrollPos;
        private LocalPersistentContext<int> _currPage;
        private GUITableRowLayoutGroup _table;
        private HashSet<string> _seenColumnNames;
        private List<Column> _columns;
        private ObjectPicker _picker;
        private int _colOffset;
        private GUIContent _indexLabel;
        private bool _isReadOnly;
        private int _indexLabelWidth;
        private Rect _columnHeaderRect;
        private GUIPagingHelper _paging;
        private bool _drawAsList;
        private bool _isFirstFrame = true;
        private Dictionary<string, ValueResolver<string>> _columnNameResolvers = new Dictionary<string, ValueResolver<string>>();

        private ActionResolver _onTitleBarGUIResolver;
        private ActionResolver _customAddVoidFunctionResolver;
        private ValueResolver _customAddFunctionResolver;
        private ActionResolver _customRemoveFunctionResolver;

        private MultiCollectionFilter<IOrderedCollectionResolver> _filter;
        
        /// <summary>
        /// Determines whether this instance [can draw attribute property] the specified property.
        /// </summary>
        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            return property.ChildResolver is IOrderedCollectionResolver;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            _drawAsList = false;
            _isReadOnly = Attribute.IsReadOnly || !Property.ValueEntry.IsEditable;
            _indexLabelWidth = (int)SirenixGUIStyles.Label.CalcSize(new GUIContent("100")).x + 15;
            _indexLabel = new GUIContent();
            _colOffset = 0;
            _seenColumnNames = new HashSet<string>();
            _table = new GUITableRowLayoutGroup();
            _table.MinScrollViewHeight = Attribute.MinScrollViewHeight;
            _table.MaxScrollViewHeight = Attribute.MaxScrollViewHeight;
            _resolver = Property.ChildResolver as IOrderedCollectionResolver;
            _scrollPos = this.GetPersistentValue("scrollPos", Vector2.zero);
            _currPage = this.GetPersistentValue("currPage", 0);
            _isPagingExpanded = this.GetPersistentValue("expanded", false);
            _columns = new List<Column>(10);
            _paging = new GUIPagingHelper();
            _paging.NumberOfItemsPerPage = Attribute.NumberOfItemsPerPage > 0 ? Attribute.NumberOfItemsPerPage : GeneralDrawerConfig.Instance.NumberOfItemsPrPage;
            _paging.IsExpanded = _isPagingExpanded.Value;
            _paging.IsEnabled = GeneralDrawerConfig.Instance.ShowPagingInTables || Attribute.ShowPaging;
            _paging.CurrentPage = _currPage.Value;
            Property.ValueEntry.OnChildValueChanged += OnChildValueChanged;
            _filter = new MultiCollectionFilter<IOrderedCollectionResolver>(Property, Property.ChildResolver as IOrderedCollectionResolver);

            if (Attribute.DefaultExpandedStateHasValue)
                Property.State.Expanded = Attribute.DefaultExpandedState;

            if (Attribute.CustomAddFunction != null)
            {
                _customAddFunctionResolver = ValueResolver.Get(_resolver.ElementType, Property, Attribute.CustomAddFunction);
                if (_customAddFunctionResolver.HasError)
                {
                    _customAddVoidFunctionResolver = ActionResolver.Get(Property, Attribute.CustomAddFunction);
                    if (!_customAddVoidFunctionResolver.HasError)
                    {
                        // Wipe out the former error, since we found a proper void/action overload
                        _customAddFunctionResolver = null;
                    }
                }
            }
            
            if (Attribute.CustomRemoveFunction != null)
            {
                _customRemoveFunctionResolver = ActionResolver.Get(Property, Attribute.CustomRemoveFunction,
                    new ActionNamedValue(ELEMENT_ARGUMENT_ID, _resolver.ElementType),
                    new ActionNamedValue(INDEX_ARGUMENT_ID, typeof(int)));
            }

            if (Attribute.OnTitleBarGUI != null)
            {
                _onTitleBarGUIResolver = ActionResolver.Get(Property, Attribute.OnTitleBarGUI);
            }
       
            var p = Attribute.CellPadding;
            if (p > 0)
            {
                _table.CellStyle = new GUIStyle() { padding = new RectOffset(p, p, p, p) };
            }

            GUIHelper.RequestRepaint();

            if (Attribute.ShowIndexLabels)
            {
                _colOffset++;
                _columns.Add(new Column(_indexLabelWidth, true, false, null, ColumnType.Index, 0));
            }

            if (!_isReadOnly)
            {
                _columns.Add(new Column(22, true, false, null, ColumnType.DeleteButton, 0));
            }
        }

        private GUIStyle _propertyPaddingFixed = new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(0, 0, 0, 3),
            margin = new RectOffset(3, 3, 0, 0)
        };

        /// <summary>
        /// Draws the property layout.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_drawAsList)
            {
                if (GUILayout.Button("Draw as table"))
                    _drawAsList = false;
                CallNextDrawer(label);
                return;
            }

            ActionResolver.DrawErrors(
                _onTitleBarGUIResolver,
                _customAddVoidFunctionResolver,
                _customRemoveFunctionResolver);
            
            ValueResolver.DrawErrors(_customAddFunctionResolver);

            if (_drawAsList)
            {
                CallNextDrawer(label);
                return;
            }
            
            _picker = ObjectPicker.GetObjectPicker(this, _resolver.ElementType);

            _paging.Update(_filter.GetCount());

            _currPage.Value = _paging.CurrentPage;
            _isPagingExpanded.Value = _paging.IsExpanded;

            var rect = SirenixEditorGUI.BeginIndentedVertical(_propertyPaddingFixed);
            {
                if (!Attribute.HideToolbar)
                {
                    DrawToolbar(label);
                }

                var drawFoldout = Attribute.ShowFoldout;

                if (_filter.GetCount() == 0)
                    drawFoldout = false;

                if (!drawFoldout)
                {
                    Property.State.Expanded = true;
                    DrawColumnHeaders();
                    DrawTable();
                }
                else
                {
                    if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded) && Property.Children.Count > 0)
                    {
                        DrawColumnHeaders();
                        DrawTable();
                    }
                    SirenixEditorGUI.EndFadeGroup();
                }
            }
            SirenixEditorGUI.EndIndentedVertical();

            if (Event.current.type == EventType.Repaint)
            {
                rect.yMin -= 1;
                rect.height -= 3;
                SirenixEditorGUI.DrawBorders(rect, 1, 1, Attribute.HideToolbar ? 0 : 1, 1);
            }

            DropZone(rect);
            HandleObjectPickerEvents();

            if (Event.current.type == EventType.Repaint)
            {
                _isFirstFrame = false;
            }
        }

        private void OnChildValueChanged(int index)
        {
            var valueEntry = Property.Children[index].ValueEntry;
            if (valueEntry == null)
            {
                return;
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(valueEntry.TypeOfValue))
            {
                return;
            }

            for (var i = 0; i < valueEntry.ValueCount; i++)
            {
                var uObj = valueEntry.WeakValues[i] as UnityEngine.Object;
                if (uObj)
                {
                    EditorUtility.SetDirty(uObj);
                }
            }
        }

        private void DropZone(Rect rect)
        {
            if (_isReadOnly) return;

            var eventType = Event.current.type;
            if ((eventType == EventType.DragUpdated || eventType == EventType.DragPerform) && rect.Contains(Event.current.mousePosition))
            {
                UnityEngine.Object[] objReferences = null;

                if (DragAndDrop.objectReferences.Any(n => n != null && _resolver.ElementType.IsInstanceOfType(n)))
                {
                    objReferences = DragAndDrop.objectReferences.Where(x => x != null && _resolver.ElementType.IsInstanceOfType(x)).Reverse().ToArray();
                }
                else if (_resolver.ElementType.InheritsFrom(typeof(Component)))
                {
                    objReferences = DragAndDrop.objectReferences.OfType<GameObject>().Select(x => x.GetComponent(_resolver.ElementType)).Where(x => x != null).Reverse().ToArray();
                }
                else if (_resolver.ElementType.InheritsFrom(typeof(Sprite)) && DragAndDrop.objectReferences.Any(n => n is Texture2D && AssetDatabase.Contains(n)))
                {
                    objReferences = DragAndDrop.objectReferences.OfType<Texture2D>().Select(x =>
                    {
                        var path = AssetDatabase.GetAssetPath(x);
                        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }).Where(x => x != null).Reverse().ToArray();
                }

                var acceptsDrag = objReferences != null && objReferences.Length > 0;

                if (acceptsDrag)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    if (eventType == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in objReferences)
                        {
                            var values = new object[Property.ParentValues.Count];
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = obj;
                            }
                            _resolver.QueueAdd(values);
                        }
                    }
                }
            }
        }

        private void AddColumns(int rowIndexFrom, int rowIndexTo)
        {
            if (Event.current.type != EventType.Layout)
            {
                return;
            }

            for (var y = rowIndexFrom; y < rowIndexTo; y++)
            {
                var rowProperty = Property.Children[y];
                foreach (var colProperty in rowProperty.Children)
                {
                    var inlinePropertyAttr = colProperty.GetAttribute<InlinePropertyAttribute>();
                    if (inlinePropertyAttr != null)
                    {
                        foreach (var colChildProperty in colProperty.Children)
                        {
                            TryAddColumn(colChildProperty, colProperty.Info.Order + colChildProperty.Info.Order, colProperty);
                        }
                    }
                    else
                    {
                        TryAddColumn(colProperty, colProperty.Info.Order );
                    }

                    void TryAddColumn(InspectorProperty childProperty, float order, InspectorProperty root = null)
                    {
                        if (!childProperty.State.Visible)
                            return;
                        var seenName = root == null ? childProperty.Name : $"{root.Name}.{childProperty.Name}";
                        if (!_seenColumnNames.Add(seenName))
                            return;

                        var hide = GetColumnAttribute<HideInTablesAttribute>(childProperty);
                        if (hide != null)
                        {
                            return;
                        }

                        var preserve = false;
                        var resizable = true;
                        var preferWide = true;
                        var minWidth = Attribute.DefaultMinColumnWidth;
                        var width = Attribute.DefaultMinColumnWidth;
                        var niceName = childProperty.NiceName;

                        var colAttr = GetColumnAttribute<TableColumnWidthAttribute>(childProperty);
                        if (colAttr != null)
                        {
                            preserve = !colAttr.Resizable;
                            resizable = colAttr.Resizable;
                            minWidth = colAttr.Width;
                            width = colAttr.Width;
                            preferWide = false;
                        }

                        var nameOverridden = false;
                        var enhancedColAttr = GetColumnAttribute<EnhancedTableColumnAttribute>(childProperty);
                        if (enhancedColAttr != null)
                        {
                            if (enhancedColAttr.DisplayName != null)
                            {
                                if (!_columnNameResolvers.TryGetValue(enhancedColAttr.DisplayName, out var resolver))
                                {
                                    resolver = ValueResolver.GetForString(childProperty, enhancedColAttr.DisplayName);
                                    _columnNameResolvers[enhancedColAttr.DisplayName] = resolver;
                                }
                                
                                niceName = resolver.GetValue();
                                nameOverridden = true;
                            }
                            if (enhancedColAttr.HasWidth) width = enhancedColAttr.Width;
                            minWidth = enhancedColAttr.HasMinWidth ? enhancedColAttr.MinWidth : width;
                            resizable = enhancedColAttr.Resizable;
                            preserve = enhancedColAttr.HasPreserveWidth ? enhancedColAttr.PreserveWidth : !enhancedColAttr.Resizable;
                            preferWide = false;
                        }
                        
                        var groupAttr = GetColumnAttribute<PropertyGroupAttribute>(childProperty);
                        if (!nameOverridden && groupAttr != null)
                        {
                            if (groupAttr.GroupName != null)
                            {
                                if (!_columnNameResolvers.TryGetValue(groupAttr.GroupName, out var resolver))
                                {
                                    resolver = ValueResolver.GetForString(childProperty, groupAttr.GroupName);
                                    _columnNameResolvers[groupAttr.GroupName] = resolver;
                                }
                                
                                niceName = resolver.GetValue();
                            }
                        }

                        if (!TryGetColumn(childProperty.Name, out var resultColumn))
                        {
                            var newCol = new Column(minWidth, width, preserve, resizable, childProperty.Name, ColumnType.Property, order)
                            {
                                NiceName = niceName
                            };
                            newCol.NiceNameLabelWidth = (int) SirenixGUIStyles.Label.CalcSize(new GUIContent(newCol.NiceName)).x;
                            newCol.PreferWide = preferWide;

                            _columns.Add(newCol);

                            // Using order by because it is STABLE, classic sort is not!
                            _columns = _columns.OrderBy(c => c).ToList();

                            resultColumn = newCol;
                        }

                        if (root != null)
                        {
                            resultColumn.RootPropertyName = root.Name;
                        }

                        GUIHelper.RequestRepaint();
                    }
                }
            }
        }

        private bool TryGetColumn(string name, out Column resultColumn)
        {
            foreach (var column in _columns)
            {
                if (column.Name != name) continue;
                resultColumn = column;
                return true;
            }
            resultColumn = null;
            return false;
        }

        private void DrawToolbar(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            label ??= GUIHelper.TempContent("");

            if (!Attribute.ShowFoldout || _filter.GetCount() == 0)
            {
                EditorGUILayout.LabelField(label);
            }
            else
            {
                Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, label);
            }

            GUILayout.FlexibleSpace();

            if (_filter.IsUsed)
            {
                _filter.Draw();
            }

            _paging.DrawToolbarPagingButtons(Property.State.Expanded, true);

            if (!_isReadOnly)
            {
                if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ListUlSolid))
                {
                    _drawAsList = !_drawAsList;
                }
            }

            if (!_isReadOnly && !Attribute.HideAddButton)
            {
                var addButtonRect = GUILayoutUtility.GetLastRect();
                if (SirenixEditorGUI.ToolbarButton(SdfIconType.Plus))
                {
                    var hasCustomAdd = _customAddFunctionResolver != null && !_customAddFunctionResolver.HasError;
                    var hasCustomAddVoid = _customAddVoidFunctionResolver != null && !_customAddVoidFunctionResolver.HasError;
                    if (CollectionDrawerStaticInfo.NextCustomAddFunction != null)
                    {
                        CollectionDrawerStaticInfo.NextCustomAddFunction?.Invoke();
                        CollectionDrawerStaticInfo.NextCustomAddFunction = null;
                    }
                    else if (Attribute.CustomAddFunction != null && (hasCustomAdd || hasCustomAddVoid))
                    {
                        if (hasCustomAdd)
                        {
                            var value = _customAddFunctionResolver.GetWeakValue();
                            var values = new object[] {value};
                            _resolver.QueueAdd(values);
                        }
                        else
                        {
                            _customAddVoidFunctionResolver.DoAction();
                        }
                    }
                    else
                    {
                        _picker.ShowObjectPicker(
                            null,
                            Property.GetAttribute<AssetsOnlyAttribute>() == null && !typeof(ScriptableObject).IsAssignableFrom(_resolver.ElementType),
                            addButtonRect,
                            !Property.ValueEntry.SerializationBackend.SupportsPolymorphism);
                    }
                }
            }

            if (_onTitleBarGUIResolver != null && !_onTitleBarGUIResolver.HasError)
            {
                _onTitleBarGUIResolver.DoAction();
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void DrawColumnHeaders()
        {
            if (Property.Children.Count == 0)
            {
                return;
            }

            _columnHeaderRect = GUILayoutUtility.GetRect(0, 21);

            _columnHeaderRect.height += 1;
            _columnHeaderRect.y -= 1;

            if (Event.current.type == EventType.Repaint)
            {
                SirenixEditorGUI.DrawBorders(_columnHeaderRect, 1);
                EditorGUI.DrawRect(_columnHeaderRect, SirenixGUIStyles.ColumnTitleBg);
            }

            var offset = _columnHeaderRect.width - _table.ContentRect.width;
            _columnHeaderRect.width -= offset;
            GUITableUtilities.ResizeColumns(_columnHeaderRect, _columns);

            if (Event.current.type == EventType.Repaint)
            {
                GUITableUtilities.DrawColumnHeaderSeperators(_columnHeaderRect, _columns, SirenixGUIStyles.BorderColor);

                var rect = _columnHeaderRect;
                foreach (var col in _columns)
                {
                    if (rect.x > _columnHeaderRect.xMax)
                        break;

                    rect.width = col.ColWidth;
                    rect.xMax = Mathf.Min(_columnHeaderRect.xMax, rect.xMax);

                    if (col.NiceName != null)
                    {
                        GUI.Label(rect, col.NiceName, SirenixGUIStyles.LabelCentered);
                    }

                    rect.x += col.ColWidth;
                }
            }
        }

        private void DrawTable()
        {
            _table.DrawScrollView = Attribute.DrawScrollView && (_paging.IsExpanded || !_paging.IsEnabled);
            _table.ScrollPos = _scrollPos.Value;
            _table.BeginTable(_paging.EndIndex - _paging.StartIndex);
            {
                AddColumns(_table.RowIndexFrom, _table.RowIndexTo);
                DrawListItemBackGrounds();

                var currX = 0f;
                foreach (var col in _columns)
                {
                    var colWidth = (int)col.ColWidth;
                    if (_isFirstFrame && col.PreferWide)
                    {
                        // First frame is often rendered with minWidth becase we don't know the full width yet.
                        // resulting in very tall rows. This tweak will give a better first guess at how tall a row is.
                        colWidth = 200;
                    }

                    _table.BeginColumn((int)currX, colWidth);
                    GUIHelper.PushLabelWidth(colWidth * 0.3f);
                    currX += col.ColWidth;
                    for (var j = _table.RowIndexFrom; j < _table.RowIndexTo; j++)
                    {
                        _table.BeginCell(j);
                        DrawCell(col, j);
                        _table.EndCell(j);
                    }
                    GUIHelper.PopLabelWidth();
                    _table.EndColumn();
                }

                DrawRightClickContextMenuAreas();
            }
            _table.EndTable();
            _scrollPos.Value = _table.ScrollPos;
            DrawColumnSeperators();

            if (_columns.Count > 0 && _columns[0].ColumnType == ColumnType.Index)
            {
                // The indexLabelWidth changes: (1 - 10 - 100 - 1000)
                _columns[0].ColWidth = _indexLabelWidth;
                _columns[0].MinWidth = _indexLabelWidth;
            }
        }

        private void DrawColumnSeperators()
        {
            if (Event.current.type == EventType.Repaint)
            {
                var bCol = SirenixGUIStyles.BorderColor;
                bCol.a *= 0.4f;
                var r = _table.OuterRect;
                GUITableUtilities.DrawColumnHeaderSeperators(r, _columns, bCol);
            }
        }

        private void DrawListItemBackGrounds()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            for (var i = _table.RowIndexFrom; i < _table.RowIndexTo; i++)
            {
                var rect = _table.GetRowRect(i);
                var col = i % 2 == 0 ? SirenixGUIStyles.ListItemColorEven : SirenixGUIStyles.ListItemColorOdd;
                EditorGUI.DrawRect(rect, col);
            }
        }

        private void DrawRightClickContextMenuAreas()
        {
            for (var i = _table.RowIndexFrom; i < _table.RowIndexTo; i++)
            {
                var rect = _table.GetRowRect(i);
                Property.Children[i].Update();
                PropertyContextMenuDrawer.AddRightClickArea(Property.Children[i], rect);
            }
        }

        private void DrawCell(Column col, int rowIndex)
        {
            rowIndex += _paging.StartIndex;

            if (col.ColumnType == ColumnType.Index)
            {
                var rect = GUILayoutUtility.GetRect(0, 16);
                rect.xMin += 5;
                rect.width -= 2;
                if (Event.current.type == EventType.Repaint)
                {
                    _indexLabel.text = rowIndex.ToString();
                    GUI.Label(rect, _indexLabel, SirenixGUIStyles.Label);
                    var labelWidth = (int)SirenixGUIStyles.Label.CalcSize(_indexLabel).x;
                    _indexLabelWidth = Mathf.Max(_indexLabelWidth, labelWidth + 15);
                }
            }
            else if (col.ColumnType == ColumnType.DeleteButton)
            {
                var rect = GUILayoutUtility.GetRect(20, 20).AlignCenter(13, 13);

                if (SirenixEditorGUI.SDFIconButton(rect, SdfIconType.X, IconAlignment.LeftOfText, SirenixGUIStyles.IconButton))
                {
                    if (_customRemoveFunctionResolver != null && !_customRemoveFunctionResolver.HasError)
                    {
                        Property.RecordForUndo("Custom List Remove");
                        _customRemoveFunctionResolver.Context.NamedValues.Set(ELEMENT_ARGUMENT_ID, Property.Children[rowIndex].ValueEntry.WeakSmartValue);
                        _customRemoveFunctionResolver.Context.NamedValues.Set(INDEX_ARGUMENT_ID, rowIndex);
                        _customRemoveFunctionResolver.DoAction();
                        Property.MarkSerializationRootDirty();
                    }
                    else
                    {
                        _resolver.QueueRemoveAt(rowIndex); 
                    }
                }
            }
            else if (col.ColumnType == ColumnType.Property)
            {
                var cell = _filter[rowIndex].Children[col.Name];
                
                cell?.Draw(null);
                if (col.RootPropertyName != null)
                {
                    var rootCell = Property.Children[rowIndex].Children[col.RootPropertyName];
                    if (rootCell.GetAttribute<InlinePropertyAttribute>() != null)
                    {
                        var childCell = rootCell.Children[col.Name];
                        childCell?.Draw(null);
                    }
                }
            }
            else
            {
                throw new NotImplementedException(col.ColumnType.ToString());
            }
        }

        private void HandleObjectPickerEvents()
        {
            if (_picker.IsReadyToClaim && Event.current.type == EventType.Repaint)
            {
                var value = _picker.ClaimObject();
                var values = new object[Property.Tree.WeakTargets.Count];
                values[0] = value;
                for (var j = 1; j < values.Length; j++)
                {
                    values[j] = SerializationUtility.CreateCopy(value);
                }
                _resolver.QueueAdd(values);
            }
        }

        private IEnumerable<InspectorProperty> EnumerateGroupMembers(InspectorProperty groupProperty)
        {
            foreach (var child in groupProperty.Children)
            {
                var info = child.Info;
                if (info.PropertyType != PropertyType.Group)
                {
                    yield return child;
                }
                else
                {
                    foreach (var item in EnumerateGroupMembers(child))
                    {
                        yield return item;
                    }
                }
            }
        }

        private T GetColumnAttribute<T>(InspectorProperty col)
            where T : Attribute
        {
            T colAttr;
            if (col.Info.PropertyType == PropertyType.Group)
            {
                colAttr = EnumerateGroupMembers(col)
                    .Select(c => c.GetAttribute<T>())
                    .FirstOrDefault(c => c != null);
            }
            else
            {
                colAttr = col.GetAttribute<T>();
            }

            return colAttr;
        }

        private enum ColumnType
        {
            Index,
            Property,
            DeleteButton,
        }

        private class Column : IResizableColumn, IComparable<Column>
        {
            public string Name;
            public float ColWidth;
            public float MinWidth;
            public bool Preserve;
            public bool Resizable;
            public string NiceName;
            public int NiceNameLabelWidth;
            public float Order;
            public ColumnType ColumnType;
            public bool PreferWide;
            public string RootPropertyName;

            public Column(int minWidth, bool preserveWidth, bool resizable, string name, ColumnType colType, float order) :
                this(minWidth, minWidth, preserveWidth, resizable, name, colType, order)
            {
            }

            public Column(int minWidth, int width, bool preserveWidth, bool resizable, string name, ColumnType colType, float order)
            {
                MinWidth = minWidth;
                ColWidth = width;
                Preserve = preserveWidth;
                Name = name;
                ColumnType = colType;
                Resizable = resizable;
                Order = order;
            }

            float IResizableColumn.ColWidth { get => ColWidth; set => ColWidth = value; }
            float IResizableColumn.MinWidth => MinWidth;
            bool IResizableColumn.PreserveWidth => Preserve;
            bool IResizableColumn.Resizable => Resizable;

            public int CompareTo(Column other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;

                var typeComparison = ColumnType.CompareTo(other.ColumnType);
                if (typeComparison != 0) return typeComparison;

                return Order.CompareTo(other.Order);
            }
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            genericMenu.AddItem(new GUIContent("Draw as List"), _drawAsList, OnDrawAsListToggled);
        }

        private void OnDrawAsListToggled()
        {
            _drawAsList = !_drawAsList;
        }
    }

    [ResolverPriority(2)]
    internal class ScriptableObjectTableListResolver<T> : BaseMemberPropertyResolver<T>
    where T : ScriptableObject
    {
        private List<OdinPropertyProcessor> _processors;
        
        public override bool CanResolveForPropertyFilter(InspectorProperty property)
        {
            return property.Parent?.GetAttribute<TableListAttribute>() != null && property.Parent.ChildResolver is IOrderedCollectionResolver;
        }

        protected override InspectorPropertyInfo[] GetPropertyInfos()
        {
            _processors ??= OdinPropertyProcessorLocator.GetMemberProcessors(Property);

            var includeSpeciallySerializedMembers = InspectorPropertyInfoUtility.TypeDefinesShowOdinSerializedPropertiesInInspectorAttribute_Cached(typeof(T));
            var infos = InspectorPropertyInfoUtility.CreateMemberProperties(Property, typeof(T), includeSpeciallySerializedMembers);

            foreach (var processor in _processors)
            {
                ProcessedMemberPropertyResolverExtensions.ProcessingOwnerType = typeof(T);
                processor.ProcessMemberProperties(infos);
            }

            return InspectorPropertyInfoUtility.BuildPropertyGroupsAndFinalize(Property, typeof(T), infos, includeSpeciallySerializedMembers);
        }
    }
}