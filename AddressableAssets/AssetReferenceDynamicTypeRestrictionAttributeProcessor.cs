#if ADDRESSABLES && UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace EDIVE.AddressableAssets
{
    public class AssetReferenceDynamicTypeRestrictionAttributeProcessor<T> : OdinAttributeProcessor<T>
        where T : AssetReference
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            var typeConstraintAttribute = attributes.GetAttribute<AssetReferenceDynamicTypeRestrictionAttribute>();
            if (typeConstraintAttribute == null)
                return;

            var resolver = ValueResolver.Get<Type>(property, typeConstraintAttribute.TypeGetter);
            attributes.Add(new DynamicAssetReferenceTypeRestrictionAttribute(resolver));
        }
    }
    
    // Dynamically added, use AssetReferenceDynamicTypeRestrictionAttribute instead
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    internal class DynamicAssetReferenceTypeRestrictionAttribute : AssetReferenceUIRestriction
    {
        private ValueResolver<Type> TypeResolver { get; set; }
        
        public DynamicAssetReferenceTypeRestrictionAttribute(ValueResolver<Type> typeResolver)
        {
            TypeResolver = typeResolver;
        }
        
        public override bool ValidateAsset(Object obj)
        {
            var type = !TypeResolver.HasError ? TypeResolver.GetValue() : null;
            if (obj == null)
                return true;
            if (obj is GameObject gameObj) {
                return type == null || gameObj.GetComponent(type) != null;
            }
            return type == null || type.IsInstanceOfType(obj);
        }
    }
}
#endif
