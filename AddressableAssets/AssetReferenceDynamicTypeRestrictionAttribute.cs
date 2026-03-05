#if ADDRESSABLES
using System;

namespace EDIVE.AddressableAssets
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AssetReferenceDynamicTypeRestrictionAttribute : Attribute
    {
        public string TypeGetter { get; }
        public AssetReferenceDynamicTypeRestrictionAttribute(string typeGetter)
        {
            TypeGetter = typeGetter;
        }
    }
}
#endif
