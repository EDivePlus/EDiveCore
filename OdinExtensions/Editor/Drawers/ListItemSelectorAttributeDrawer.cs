using System;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0.01, 0, 0)]
    public class ListItemSelectorAttributeDrawer : OdinAttributeDrawer<ListItemSelectorAttribute>
    {
        private const string INDEX_ARGUMENT_ID = "index";
        private const string SET_SELECTED_INDEX_ARGUMENT_ID = "setSelectedIndex";
        private const string SET_SELECTED_ELEMENT_ARGUMENT_ID = "setSelectedElement";
        private static readonly Color SELECTED_COLOR = new Color(0.301f, 0.563f, 1f, 0.497f);
        
        private bool _isListElement;
        private ActionResolver _selectActionResolver;
        private ActionResolver _setSelectedCallbackSetterResolver;
        
        private InspectorProperty _selectedProperty;
        private PropertyContext<InspectorProperty> _globalSelectedProperty;
        
        private LocalPersistentContext<int> _lastSelectedIndexContext;

        protected override void Initialize()
        {
            _isListElement = Property.Parent != null && Property.Parent.ChildResolver is IOrderedCollectionResolver;
            var isList = !_isListElement;
            var listProperty = isList ? Property : Property.Parent;
            var baseMemberProperty = listProperty?.FindParent(x => x.Info.PropertyType == PropertyType.Value, true);
            _globalSelectedProperty = baseMemberProperty?.Context.GetGlobal("selectedIndex" + baseMemberProperty.GetHashCode(), (InspectorProperty) null);
            _lastSelectedIndexContext = PersistentContext.GetLocal(TwoWaySerializationBinder.Default.BindToName(listProperty.Tree.TargetType).GetHashCode(), listProperty.Path, listProperty.Index, "SelectedIndex", -1);

            if (isList)
            {
                switch (Attribute.OnReloadAction)
                {
                    case SelectorReloadActionType.None: 
                        break;
                    
                    case SelectorReloadActionType.Clear:
                        SelectIndex(-1);
                        break;
                    
                    case SelectorReloadActionType.SelectFirst: 
                        if (listProperty?.Children.Count > 0)
                        {
                            _selectedProperty = Property.Children[0];
                            _globalSelectedProperty.Value = _selectedProperty;
                            SelectIndex(0);
                        } 
                        break;

                    case SelectorReloadActionType.SelectPrevious:
                        if (_lastSelectedIndexContext.Value >= 0 && _lastSelectedIndexContext.Value < Property.Children.Count)
                        {
                            _selectedProperty = Property.Children[_lastSelectedIndexContext.Value];
                            _globalSelectedProperty.Value = _selectedProperty;
                            SelectIndex(_lastSelectedIndexContext.Value);
                        }
                        
                        break;

                    case SelectorReloadActionType.SelectPreviousOrFirst:
                        if (_lastSelectedIndexContext.Value >= 0 && _lastSelectedIndexContext.Value < Property.Children.Count)
                        {
                            _selectedProperty = Property.Children[_lastSelectedIndexContext.Value];
                            _globalSelectedProperty.Value = _selectedProperty;
                            SelectIndex(_lastSelectedIndexContext.Value);
                        }
                        else if (listProperty?.Children.Count > 0)
                        {
                            _selectedProperty = Property.Children[0];
                            _globalSelectedProperty.Value = _selectedProperty;
                            SelectIndex(0);
                        } 
                        break;
                        
                    default: throw new ArgumentOutOfRangeException();
                }

                if (Attribute.Select != null)
                {
                    _selectActionResolver = ActionResolver.Get(Property, Attribute.Select,
                        new NamedValue(INDEX_ARGUMENT_ID, typeof(int)));
                }

                if (Attribute.SetSelectedCallbackSetter != null)
                {
                    _setSelectedCallbackSetterResolver = ActionResolver.Get(Property, Attribute.SetSelectedCallbackSetter,
                        new NamedValue(SET_SELECTED_INDEX_ARGUMENT_ID, typeof(Action<int>)),
                        new NamedValue(SET_SELECTED_ELEMENT_ARGUMENT_ID, typeof(Action<object>)));

                    if (_setSelectedCallbackSetterResolver != null && !_setSelectedCallbackSetterResolver.HasError)
                    {
                        Property.Tree.DelayActionUntilRepaint(() =>
                        {
                            Action<int> selectActionIndex = SelectIndex;
                            Action<object> selectActionElement = SelectElement;
                            _setSelectedCallbackSetterResolver.Context.NamedValues.Set(SET_SELECTED_INDEX_ARGUMENT_ID, selectActionIndex);
                            _setSelectedCallbackSetterResolver.Context.NamedValues.Set(SET_SELECTED_ELEMENT_ARGUMENT_ID, selectActionElement);
                            _setSelectedCallbackSetterResolver.DoAction();
                        }); 
                    }
                }
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_isListElement)
            {
                DrawListElement(label);
            }
            else
            {
                ActionResolver.DrawErrors(
                    _selectActionResolver, 
                    _setSelectedCallbackSetterResolver);
                DrawList(label);
            }
        }

        private void DrawListElement(GUIContent label)
        {
            var eventType = Event.current.type;
            if (eventType == EventType.Layout)
            {
                CallNextDrawer(label);
            }
            else
            {
                var rect = GUIHelper.GetCurrentLayoutRect();
                var isSelected = _globalSelectedProperty.Value == Property;

                switch (eventType)
                {
                    case EventType.Repaint when isSelected: EditorGUI.DrawRect(rect, SELECTED_COLOR);
                        break;
                    case EventType.MouseDown when rect.Contains(Event.current.mousePosition): _globalSelectedProperty.Value = Property;
                        break;
                }
                CallNextDrawer(label);
            }
        }
        
        private void DrawList(GUIContent label)
        {
            CallNextDrawer(label);

            if (Event.current.type == EventType.Layout) return;
            var property = _globalSelectedProperty.Value;

            if (property != null && property != _selectedProperty)
            {
                _selectedProperty = property;
                SelectIndex(_selectedProperty.Index);
            }
            else if (_selectedProperty != null && _selectedProperty.Index < Property.Children.Count && _selectedProperty != Property.Children[_selectedProperty.Index])
            {
                SelectIndex(-1);
                _selectedProperty = null;
                _globalSelectedProperty.Value = null;
            }
        }

        private void SelectElement(object element)
        {
            var property = Property.Children
                .Select((p, i) => new {InspectorProperty = p, index = i})
                .FirstOrDefault(p => p.InspectorProperty.ValueEntry.WeakSmartValue == element);

            if (_selectActionResolver != null && !_selectActionResolver.HasError)
            {
                var index = property?.index ?? -1;
                SelectIndex(index);
                Property.Tree.DelayActionUntilRepaint(() =>
                {
                    if (property != null)
                    {
                        _selectedProperty = property.InspectorProperty;
                        _globalSelectedProperty.Value = property.InspectorProperty;
                    }
                    else
                    {
                        _selectedProperty = null;
                        _globalSelectedProperty.Value = null;
                    }

                    _selectActionResolver.Context.NamedValues.Set(INDEX_ARGUMENT_ID, index);
                    _selectActionResolver.DoAction();
                    _lastSelectedIndexContext.Value = index;
                });
            }
        }

        private void SelectIndex(int index)
        {
            GUIHelper.RequestRepaint();
            Property.Tree.DelayActionUntilRepaint(() =>
            {
                if (_selectActionResolver == null || _selectActionResolver.HasError) return;
                _selectActionResolver.Context.NamedValues.Set(INDEX_ARGUMENT_ID, index);
                _selectActionResolver.DoAction();
                _lastSelectedIndexContext.Value = index;
            });
        }
    }
}
