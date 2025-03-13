using System;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using NamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class ShowCreateNewDrawerForScriptableObject<T> : ShowCreateNewDrawer<T> where T : ScriptableObject
    {
        protected override bool TryCreateInstance(Type type, string defaultPath, string defaultName, out T result)
        {
            var path = defaultPath;
            if (string.IsNullOrEmpty(path)) 
                path = EditorAssetUtils.GetSelectedAssetsPath();

            result = OdinExtensionUtils.CreateNewInstanceOfType<T>(type, path, defaultName);
            return result != null;
        }

        protected override Type GetBaseType() => base.GetBaseType() ?? ValueEntry.BaseValueType;
    }
    
#if ADDRESSABLES
    [DrawerPriority(0, 200, 0)]
    public class ShowCreateNewDrawerForAssetReferenceScriptableObject: ShowCreateNewDrawer<AssetReferenceT<ScriptableObject>>
    {
        protected override bool TryCreateInstance(Type type, string defaultPath, string defaultName, out AssetReferenceT<ScriptableObject> result)
        {
            var path = defaultPath;
            if (string.IsNullOrEmpty(path)) 
                path = EditorAssetUtils.GetSelectedAssetsPath();

            var instance = OdinExtensionUtils.CreateNewInstanceOfType(type, path, defaultName);
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance, out var guid, out long _))
            {
                result = new AssetReferenceT<ScriptableObject>(guid);
                return true;
            }
            
            result = null;
            return false;
        }
        
        protected override Type GetBaseType() => base.GetBaseType() ?? ValueEntry.BaseValueType;
    }
#endif
    
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public abstract class ShowCreateNewDrawer<T> : OdinAttributeDrawer<ShowCreateNewAttribute, T>
    {
        private ValueResolver<Type> _overrideTypeResolver;
        private ValueResolver<bool> _showIfResolver;
        private ValueResolver<string> _overrideDefaultNameResolver;
        private ValueResolver<string> _defaultPathResolver;
        private ActionResolver _onCreatedNewResolver;

        protected override void Initialize()
        {
            _overrideTypeResolver = ValueResolver.Get(Property, Attribute.GetOverrideTypeFunc, Attribute.OverrideType);

            if (!string.IsNullOrEmpty(Attribute.ShowIf))
                _showIfResolver = ValueResolver.Get<bool>(Property, Attribute.ShowIf);

            if (!string.IsNullOrEmpty(Attribute.OverrideDefaultName))
                _overrideDefaultNameResolver = ValueResolver.GetForString(Property, Attribute.OverrideDefaultName);

            if (!string.IsNullOrEmpty(Attribute.DefaultPath))
                _defaultPathResolver = ValueResolver.GetForString(Property, Attribute.DefaultPath);

            if (!string.IsNullOrEmpty(Attribute.OnCreatedNew))
                _onCreatedNewResolver = ActionResolver.Get(Property, Attribute.OnCreatedNew, new NamedValue[] {new("value", typeof(T))});
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_overrideTypeResolver, _showIfResolver, _overrideDefaultNameResolver, _defaultPathResolver);
            ActionResolver.DrawErrors(_onCreatedNewResolver);
            
            EditorGUILayout.BeginHorizontal();
            CallNextDrawer(label);
            if (_showIfResolver == null || _showIfResolver.HasError || _showIfResolver.GetValue())
            {
                var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.FileMedicalSolid, "Create new"))
                {
                    var baseType = GetBaseType();
                    OdinExtensionUtils.DrawSubtypeDropDownOrCall(baseType, CreateInstance);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        protected string GetDefaultName(Type type)
        {
            if (_overrideDefaultNameResolver != null && !_overrideDefaultNameResolver.HasError)
                return _overrideDefaultNameResolver.GetValue();
            if (type != null)
                return $"New {type.Name}";
            
            return $"New {typeof(T).Name}";
        }

        protected virtual Type GetBaseType() => _overrideTypeResolver != null && !_overrideTypeResolver.HasError ? _overrideTypeResolver.GetValue() : null;
        protected string GetDefaultPath() => _defaultPathResolver != null && !_defaultPathResolver.HasError ? PathUtility.GetAbsolutePath(_defaultPathResolver.GetValue()) : Application.dataPath;
        private void CreateInstance(Type type)
        {
            if (TryCreateInstance(type, GetDefaultPath(), GetDefaultName(type), out var instance))
            {
                ValueEntry.SmartValue = instance;
                ValueEntry.ApplyChanges();

                if (_onCreatedNewResolver != null && !_onCreatedNewResolver.HasError)
                {
                    _onCreatedNewResolver.Context.NamedValues.Set("value", instance);
                    _onCreatedNewResolver.DoAction();
                }

            }
        }

        protected abstract bool TryCreateInstance(Type type, string defaultPath, string defaultName, out T result);
    }
}
