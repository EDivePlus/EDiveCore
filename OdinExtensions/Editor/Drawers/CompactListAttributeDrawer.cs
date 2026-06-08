// Author: František Holubec
// Created: 08.06.2026

using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class CompactListAttributeDrawer<T> : OdinAttributeDrawer<CompactListAttribute, T>
    {
        private const float SIDE_TOOLBAR_WIDTH = 22;

        private static readonly GUILayoutOption[] ListItemOptions = GUILayoutOptions.MinHeight(20).ExpandWidth();

        private ICollectionResolver _resolver;
        private IOrderedCollectionResolver _orderedResolver;
        private ObjectPicker _picker;

        private GUIStyle _listItemStyle;

        private int _insertAt;
        private int _removeAt = -1;
        private object[] _removeValues;

        private Vector2 _draggingMousePosition;
        private DropZoneHandle _dropZone;
        private bool _isAboutToDropUnityObjects;
        private bool _isDroppingUnityObjects;

        protected override bool CanDrawAttributeValueProperty(InspectorProperty property)
        {
            return property.ChildResolver is ICollectionResolver;
        }

        protected override void Initialize()
        {
            _resolver = Property.ChildResolver as ICollectionResolver;
            _orderedResolver = Property.ChildResolver as IOrderedCollectionResolver;
            _listItemStyle = new GUIStyle(GUIStyle.none) {padding = new RectOffset(25, 20, 1, 1)};
            Property.State.Expanded = true;
        }

        private bool IsReadOnly => !ValueEntry.IsEditable || _resolver.IsReadOnly || Attribute.IsReadOnly;
        private bool Draggable => !IsReadOnly && Attribute.Draggable && _orderedResolver != null;
        private bool ShowRemoveButton => !IsReadOnly && !Attribute.HideRemoveButton;
        private bool ShowAddButton => !IsReadOnly && !Attribute.HideAddButton;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var count = Property.Children.Count;
            if (Attribute.MaxItems > 0 && count > Attribute.MaxItems)
            {
                CallNextDrawer(label);
                return;
            }

            _picker = ObjectPicker.GetObjectPicker(this, _resolver.ElementType);

            _listItemStyle.padding.left = Draggable ? 25 : 7;
            _listItemStyle.padding.right = ShowRemoveButton ? 20 : 4;

            SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyMargin);
            var areaRect = EditorGUILayout.BeginHorizontal();
            {
                BeginDropZone();
                {
                    DrawItems(count);
                }
                EndDropZone();
                if (ShowAddButton)
                    GUILayout.Space(SIDE_TOOLBAR_WIDTH);
            }
            EditorGUILayout.EndHorizontal();
            SirenixEditorGUI.EndIndentedVertical();

            if (ShowAddButton)
                DrawSideToolbar(areaRect);

            HandlePendingRemoval();
            HandleObjectPickerEvents();
        }

        private void DrawSideToolbar(Rect areaRect)
        {
            var stripRect = new Rect(areaRect.xMax - SIDE_TOOLBAR_WIDTH, areaRect.y - 1, SIDE_TOOLBAR_WIDTH, areaRect.height + 1);
            if (Event.current.type == EventType.Repaint)
            {
                SirenixGUIStyles.ToolbarBackground.Draw(stripRect, GUIContent.none, false, false, false, false);
                SirenixEditorGUI.DrawBorders(stripRect, 0, 1, 1, 1);
            }
            
            var addRect = new Rect(stripRect.x, stripRect.y, SIDE_TOOLBAR_WIDTH - 1, Mathf.Min(stripRect.height, SIDE_TOOLBAR_WIDTH));
            if (GUI.Button(addRect, GUIContent.none, SirenixGUIStyles.IconButton))
                DoAdd(addRect);

            if (Event.current.type == EventType.Repaint)
            {
                var iconColor = addRect.Contains(Event.current.mousePosition)
                    ? SirenixGUIStyles.IconButton.hover.textColor
                    : SirenixGUIStyles.IconButton.normal.textColor;
                SdfIcons.DrawIcon(addRect.AlignCenter(SIDE_TOOLBAR_WIDTH - 8), SdfIconType.Plus, iconColor);
            }
        }

        private void DoAdd(Rect addButtonRect)
        {
            if (CollectionDrawerStaticInfo.NextCustomAddFunction != null)
            {
                CollectionDrawerStaticInfo.NextCustomAddFunction.Invoke();
                CollectionDrawerStaticInfo.NextCustomAddFunction = null;
                return;
            }

            _picker.ShowObjectPicker(
                null,
                Property.GetAttribute<AssetsOnlyAttribute>() == null && !typeof(ScriptableObject).IsAssignableFrom(_resolver.ElementType),
                addButtonRect,
                !ValueEntry.SerializationBackend.SupportsPolymorphism);
        }

        private void HandleObjectPickerEvents()
        {
            if (!_picker.IsReadyToClaim || Event.current.type != EventType.Repaint)
                return;

            var value = _picker.ClaimObject();
            var values = new object[Property.Tree.WeakTargets.Count];
            values[0] = value;
            for (var j = 1; j < values.Length; j++)
                values[j] = SerializationUtility.CreateCopy(value);
            _resolver.QueueAdd(values);
        }

        private void DrawItems(int count)
        {
            if (Event.current.type is EventType.DragUpdated or EventType.DragPerform)
                _draggingMousePosition = Event.current.mousePosition;

            _insertAt = -1;
            const int from = 0;
            var to = count;

            var evenColor = SirenixGUIStyles.ListItemColorEven;
            var oddColor = SirenixGUIStyles.ListItemColorOdd;

            var drawEmptySpace = (_dropZone != null && _dropZone.IsBeingHovered) || _isDroppingUnityObjects;
            var emptyHeight = drawEmptySpace ? (_isDroppingUnityObjects ? 16 : DragAndDropManager.CurrentDraggingHandle.Rect.height) : 0;

            var rect = SirenixEditorGUI.BeginVerticalList();
            for (int i = 0, j = from, k = from; j < to; i++, j++)
            {
                var dragHandle = BeginDragHandle(j);
                {
                    if (drawEmptySpace)
                    {
                        var topHalf = dragHandle.Rect;
                        topHalf.height /= 2;
                        if (topHalf.Contains(_draggingMousePosition) || (topHalf.y > _draggingMousePosition.y && i == 0))
                        {
                            GUILayout.Space(emptyHeight);
                            drawEmptySpace = false;
                            _insertAt = k;
                        }
                    }

                    if (!dragHandle.IsDragging)
                    {
                        k++;
                        DrawItem(Property.Children[j], dragHandle, evenColor, oddColor);
                    }
                    else
                    {
                        CollectionDrawerStaticInfo.DelayedGUIDrawer.Begin(dragHandle.Rect.width, dragHandle.Rect.height);
                        DragAndDropManager.AllowDrop = false;
                        DrawItem(Property.Children[j], dragHandle, evenColor, oddColor);
                        DragAndDropManager.AllowDrop = true;
                        CollectionDrawerStaticInfo.DelayedGUIDrawer.End();
                    }

                    if (drawEmptySpace)
                    {
                        var bottomHalf = dragHandle.Rect;
                        bottomHalf.height /= 2;
                        bottomHalf.y += bottomHalf.height;
                        if (bottomHalf.Contains(_draggingMousePosition) || (bottomHalf.yMax < _draggingMousePosition.y && j + 1 == to))
                        {
                            GUILayoutUtility.GetRect(0, emptyHeight);
                            drawEmptySpace = false;
                            _insertAt = Mathf.Min(k, to);
                        }
                    }
                }
                EndDragHandle();
            }

            if (drawEmptySpace)
            {
                GUILayoutUtility.GetRect(0, emptyHeight);
                _insertAt = _draggingMousePosition.y > rect.center.y ? to : from;
            }
            else if (count == 0)
            {
                SirenixEditorGUI.BeginListItem(false, _listItemStyle, ListItemOptions);
                GUILayout.Label("List is empty", SirenixGUIStyles.CenteredGreyMiniLabel);
                SirenixEditorGUI.EndListItem();
            }

            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawItem(InspectorProperty itemProperty, DragHandle dragHandle, Color evenColor, Color oddColor)
        {
            var index = itemProperty.Index;
            var color = index % 2 == 0 ? evenColor : oddColor;

            var rect = SirenixEditorGUI.BeginListItem(false, _listItemStyle, color, color, color, color, ListItemOptions);
            {
                var handleIconRect = new Rect(rect.x + 4, rect.y + 2 + ((int) rect.height - 23) / 2f, 20, 20);
                var removeRect = new Rect(handleIconRect.x + rect.width - 22, handleIconRect.y + 1, 14, 14);

                if (Event.current.type == EventType.Repaint)
                {
                    dragHandle.DragHandleRect = new Rect(rect.x + 4, rect.y, 20, rect.height);
                    if (Draggable)
                    {
                        var tmp = GUI.color;
                        GUI.color *= new Color(1, 1, 1, 0.4f);
                        GUI.DrawTexture(handleIconRect, EditorIcons.List.Inactive);
                        GUI.color = tmp;
                    }
                }

                GUIHelper.PushHierarchyMode(false);
                var elementLabel = Attribute.ShowIndexLabels ? new GUIContent(index.ToString()) : null;
                itemProperty.Draw(elementLabel);
                GUIHelper.PopHierarchyMode();

                if (ShowRemoveButton && SirenixEditorGUI.SDFIconButton(removeRect, (GUIContent) null, SdfIconType.X, style: SirenixGUIStyles.IconButton))
                    QueueRemove(itemProperty, index);
            }
            SirenixEditorGUI.EndListItem();
        }

        private void QueueRemove(InspectorProperty itemProperty, int index)
        {
            if (_orderedResolver != null)
            {
                if (index >= 0)
                    _removeAt = index;
            }
            else
            {
                var values = new object[itemProperty.ValueEntry.ValueCount];
                for (var i = 0; i < values.Length; i++)
                    values[i] = itemProperty.ValueEntry.WeakValues[i];
                _removeValues = values;
            }
        }

        private void HandlePendingRemoval()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (_orderedResolver != null && _removeAt >= 0)
            {
                _orderedResolver.QueueRemoveAt(_removeAt);
                _removeAt = -1;
                GUIHelper.RequestRepaint();
            }
            else if (_removeValues != null)
            {
                _resolver.QueueRemove(_removeValues);
                _removeValues = null;
                GUIHelper.RequestRepaint();
            }
        }

        private DragHandle BeginDragHandle(int j)
        {
            var child = Property.Children[j];
            var dragHandle = DragAndDropManager.BeginDragHandle(child, child.ValueEntry.WeakSmartValue, IsReadOnly ? DragAndDropMethods.Reference : DragAndDropMethods.Move);
            dragHandle.Enabled = Draggable;

            if (dragHandle.OnDragStarted)
            {
                CollectionDrawerStaticInfo.CurrentDroppingPropertyInfo = null;
                CollectionDrawerStaticInfo.CurrentDraggingPropertyInfo = child;
                dragHandle.OnDragFinnished = dropEvent =>
                {
                    if (dropEvent == DropEvents.Moved)
                        _orderedResolver.QueueRemoveAt(j);
                    CollectionDrawerStaticInfo.CurrentDraggingPropertyInfo = null;
                };
            }

            return dragHandle;
        }

        private void EndDragHandle()
        {
            var handle = DragAndDropManager.EndDragHandle();
            if (handle.IsDragging)
            {
                Property.Tree.DelayAction(() =>
                {
                    if (DragAndDropManager.CurrentDraggingHandle != null)
                        CollectionDrawerStaticInfo.DelayedGUIDrawer.Draw(_draggingMousePosition - DragAndDropManager.CurrentDraggingHandle.MouseDownPostionOffset);
                });
            }
        }

        private void BeginDropZone()
        {
            if (_orderedResolver == null)
                return;

            _dropZone = DragAndDropManager.BeginDropZone(Property, _resolver.ElementType, true);
            _dropZone.Enabled = !IsReadOnly;
        }

        private void EndDropZone()
        {
            if (_orderedResolver == null)
                return;

            if (_dropZone.IsReadyToClaim)
            {
                if (_insertAt == -1)
                    _insertAt = Property.Children.Count;

                CollectionDrawerStaticInfo.CurrentDraggingPropertyInfo = null;
                CollectionDrawerStaticInfo.CurrentDroppingPropertyInfo = Property;
                var dropped = _dropZone.ClaimObject();

                var values = new object[Property.Tree.WeakTargets.Count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = dropped;

                _orderedResolver.QueueInsertAt(Mathf.Clamp(_insertAt, 0, Property.Children.Count), values);
            }
            else if (!IsReadOnly)
            {
                var droppedObjects = HandleUnityObjectsDrop();
                if (droppedObjects != null)
                {
                    if (_insertAt == -1)
                        _insertAt = Property.Children.Count;

                    foreach (var obj in droppedObjects)
                    {
                        var values = new object[Property.Tree.WeakTargets.Count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = obj;
                        _orderedResolver.QueueInsertAt(Mathf.Clamp(_insertAt, 0, Property.Children.Count), values);
                    }
                }
            }

            DragAndDropManager.EndDropZone();
        }

        private Object[] HandleUnityObjectsDrop()
        {
            var elementType = _resolver.ElementType;
            var eventType = Event.current.type;
            if (eventType == EventType.Layout)
                _isAboutToDropUnityObjects = false;

            if (eventType is EventType.DragUpdated or EventType.DragPerform && _dropZone.Rect.Contains(Event.current.mousePosition))
            {
                Object[] objReferences = null;
                if (DragAndDrop.objectReferences.Any(n => n != null && elementType.IsAssignableFrom(n.GetType())))
                    objReferences = DragAndDrop.objectReferences.Where(x => x != null && elementType.IsAssignableFrom(x.GetType())).Reverse().ToArray();
                else if (elementType.InheritsFrom(typeof(Component)))
                    objReferences = DragAndDrop.objectReferences.OfType<GameObject>().Select(x => x.GetComponent(elementType)).Where(x => x != null).Reverse().Cast<Object>().ToArray();

                var acceptsDrag = objReferences != null && objReferences.Length > 0;
                if (acceptsDrag)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                    _isAboutToDropUnityObjects = true;
                    _isDroppingUnityObjects = true;
                    if (eventType == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        return objReferences;
                    }
                }
            }

            if (eventType == EventType.Repaint)
                _isDroppingUnityObjects = _isAboutToDropUnityObjects;

            return null;
        }
    }
}
