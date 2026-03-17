// Author: František Holubec
// Created: 09.03.2026

#if UNITY_EDITOR && XR_INTERACTION_TOOLKIT
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Utils
{
    [CustomEditor(typeof(XRBaseInteractable), true)]
    [CanEditMultipleObjects]
    public class XRBaseInteractableOdinEditor : AutoNativeWrapperOdinEditor
    {
        private Type _baseType;
        protected override Type BaseType => _baseType ??= GetBaseType(target.GetType());

        private static Type GetBaseType(Type type)
        {
            var targetAssembly = typeof(XRBaseInteractable).Assembly;

            while (type != null && type.Assembly != targetAssembly)
                type = type.BaseType;

            return type ?? typeof(XRBaseInteractable);
        }
    }
    
    public static class XRBaseInteractableEditorUtils
    {
        private static IDictionary _cacheDict;
        private static Type _monoEditorType;
        private static FieldInfo _inspectorTypeField;
        private static FieldInfo _editorsField;
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += Apply;
        }
   
        private static void Apply()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var attrType = asm.GetType("UnityEditor.CustomEditorAttributes");
            
            _monoEditorType = asm.GetType("UnityEditor.CustomEditorAttributes+MonoEditorType");
            var instance = attrType.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            
            var cache = attrType.GetField("m_Cache", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);
            _cacheDict = cache?.GetType().GetField("m_CustomEditorCache", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(cache) as IDictionary;
            _inspectorTypeField = _monoEditorType.GetField("inspectorType");

            var storageType = _cacheDict?.GetType().GetGenericArguments()[1];
            _editorsField = storageType?.GetField("customEditors");
            
            var childTypes = TypeCacheUtils.GetAssignableTypes<XRBaseInteractable>();
            foreach (var childType in childTypes)
            {
                OverrideEditor(childType, typeof(XRBaseInteractableOdinEditor));
            }
        }

        private static void OverrideEditor(Type inspectedType, Type editorType)
        {
            if (!_cacheDict.Contains(inspectedType))
            {
                var storageType = _editorsField.DeclaringType;
                if (storageType != null)
                {
                    var newStorage = Activator.CreateInstance(storageType);
                    _cacheDict[inspectedType] = newStorage;
                }
            }

            var storage = _cacheDict[inspectedType];
            var editors = (IList)_editorsField.GetValue(storage);
            
            var multiField = storage.GetType().GetField("customEditorsMultiEdition");
            var multiEditors = (IList)multiField.GetValue(storage);

            var entry = Activator.CreateInstance(
                _monoEditorType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { editorType, null, true, false },
                null
            );

            if (!Exists(editors))
                editors.Insert(0, entry);

            if (multiEditors != null && !Exists(multiEditors))
                multiEditors.Insert(0, entry);
            return;

            bool Exists(IList list)
            {
                return list != null && list.Cast<object>().Any(e => (Type) _inspectorTypeField.GetValue(e) == editorType);
            }
        }
    }
}
#endif
