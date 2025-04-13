#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEngine.AddressableAssets;

namespace EDIVE.AddressableAssets
{
    public class AssetReferenceTypeConstraintAttributeProcessor<T> : OdinAttributeProcessor<T>
        where T : AssetReference
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            var typeConstraintAttribute = attributes.GetAttribute<AssetReferenceTypeConstraintAttribute>();
            if (typeConstraintAttribute == null)
                return;

            var resolver = ValueResolver.Get<Type>(property, typeConstraintAttribute.TypeGetter);
            attributes.Add(new DynamicTypeAssetReferenceUIRestriction(resolver));
        }
    }
}
#endif
