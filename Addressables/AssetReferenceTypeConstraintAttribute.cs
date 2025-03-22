using System;

namespace EDIVE.Addressables
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AssetReferenceTypeConstraintAttribute : Attribute
    {
        public string TypeGetter { get; }
        public AssetReferenceTypeConstraintAttribute(string typeGetter)
        {
            TypeGetter = typeGetter;
        }
    }
}
