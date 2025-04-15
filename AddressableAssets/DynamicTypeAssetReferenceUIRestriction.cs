#if ADDRESSABLES && UNITY_EDITOR
using System;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.AddressableAssets
{
    // Dynamically added, use AssetReferenceTypeConstraintAttribute instead
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    internal class DynamicTypeAssetReferenceUIRestriction : AssetReferenceUIRestriction
    {
        private ValueResolver<Type> TypeResolver { get; set; }
        private Type Type => !TypeResolver.HasError ? TypeResolver.GetValue() : null;
        
        public DynamicTypeAssetReferenceUIRestriction(ValueResolver<Type> typeResolver)
        {
            TypeResolver = typeResolver;
        }
        
        public override bool ValidateAsset(Object obj) => Type == null || Type.IsInstanceOfType(obj);
    }
}
#endif
